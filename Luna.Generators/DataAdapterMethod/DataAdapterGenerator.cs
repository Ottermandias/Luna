using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Luna.Generators;

[Generator]
public sealed class DataAdapterGenerator : IIncrementalGenerator
{
    private const string MethodParameter = "methodId";
    private const string ReturnType      = "TRet";
    private const string ReturnParameter = "ret";

    private static readonly string[] TypeNames      = ["T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9"];
    private static readonly string[] ParameterNames = ["a", "b", "c", "d", "e", "f", "g", "h", "i"];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static context
            => context.AddSource($"{nameof(AdapterMethodAttribute)}.g.cs", AdapterMethodAttribute.CreateAttribute().GetText(Encoding.UTF8)));

        var methods = context.SyntaxProvider
            .ForAttributeWithMetadataName(AdapterMethodAttribute.MetadataName, IsCandidate, GetTypeToGenerate)
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        context.RegisterSourceOutput(methods.Collect(), Generate);
    }

    private static void Generate(SourceProductionContext context, ImmutableArray<IMethodSymbol> allMethods)
    {
        foreach (var typeGroup in allMethods.GroupBy(static m => m.ContainingType, SymbolEqualityComparer.Default)
                     .OrderBy(static g => g.Key!.ToDisplayString(), StringComparer.Ordinal))
        {
            var type = (INamedTypeSymbol)typeGroup.Key!;
            if (!DataAdapterValidation.ValidateContainingType(context, type))
                continue;

            var entries = typeGroup.Select(m => (DataAdapterMethodEntry.TryCreate(context, m, out var e), e))
                .Where(p => p.Item1)
                .Select(p => p.e).ToList();
            if (entries.Count is 0)
                continue;

            DataAdapterValidation.CheckUniqueness(context, entries);
            if (entries.Count is 0)
                continue;

            var source = GenerateType(type, entries);
            context.AddSource($"{type.FullyQualifiedMetadataName()}.g.cs", source.GetText(Encoding.UTF8));
        }
    }

    private static CompilationUnitSyntax GenerateType(INamedTypeSymbol type, List<DataAdapterMethodEntry> entries)
    {
        var dispatchers = entries.GroupBy(static e => (e.Arity, e.IsFunction))
            .OrderBy(static g => g.Key.Arity)
            .ThenBy(static g => g.Key.IsFunction)
            .Select(static g => GenerateDispatcher(g.Key.Arity, g.Key.IsFunction, g.OrderBy(static m => m.Id)))
            .Cast<MemberDeclarationSyntax>()
            .ToArray();

        MemberDeclarationSyntax declaration = CreatePartialType(type, dispatchers);
        for (var containing = type.ContainingType; containing is not null; containing = containing.ContainingType)
            declaration = CreatePartialType(containing, [declaration]);

        if (!type.ContainingNamespace.IsGlobalNamespace)
            declaration = SyntaxFactory.FileScopedNamespaceDeclaration(GetNamespaceSyntax(type.ContainingNamespace))
                .WithMembers(SyntaxFactory.SingletonList(declaration));

        declaration = declaration.WithLeadingTrivia(SyntaxFactory.DefaultFileTrivia());

        return SyntaxFactory.CompilationUnit()
            .WithMembers(SyntaxFactory.SingletonList(declaration))
            .Normalize();
    }

    private static MethodDeclarationSyntax GenerateDispatcher(int arity, bool function, IEnumerable<DataAdapterMethodEntry> entries)
    {
        var parameters = new ParameterSyntax[arity + (function ? 2 : 1)];
        parameters[0] = SyntaxFactory.Parameter(MethodParameter.Identifier())
            .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)));
        for (var i = 0; i < arity; ++i)
            parameters[i + 1] = SyntaxFactory.Parameter(ParameterNames[i].Identifier()).WithType(TypeNames[i].IdentifierName());
        if (function)
            parameters[arity + 1] = SyntaxFactory.Parameter(ReturnParameter.Identifier())
                .WithType(SyntaxFactory.NullableType(ReturnType.IdentifierName()))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.OutKeyword)));

        var invokeAliveCheck = SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression("CheckAlive".IdentifierName()));
        var sections = entries.Select(GenerateSwitchSection).Append(GenerateDefaultSection(arity, function));
        var method = SyntaxFactory
            .MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(function ? SyntaxKind.BoolKeyword : SyntaxKind.VoidKeyword)),
                (function ? "TryInvoke" : "Invoke").Identifier())
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters)))
            .WithBody(SyntaxFactory.Block(invokeAliveCheck, SyntaxFactory.SwitchStatement(MethodParameter.IdentifierName())
                .WithSections(SyntaxFactory.List(sections))));

        if (parameters.Length > 1)
        {
            var types = (function ? TypeNames.Take(arity).Append(ReturnType) : TypeNames.Take(arity)).ToArray();
            method = method
                .WithTypeParameterList(
                    SyntaxFactory.TypeParameterList(
                        SyntaxFactory.SeparatedList(types.Select(static t => SyntaxFactory.TypeParameter(t.Identifier())))))
                .WithConstraintClauses(SyntaxFactory.List(types.Select(AllowsRefStructConstraint)));
        }

        return method;
    }

    private static TypeDeclarationSyntax CreatePartialType(INamedTypeSymbol type, IEnumerable<MemberDeclarationSyntax> members)
    {
        // The earlier validation only permits class types.
        var declaration = SyntaxFactory.ClassDeclaration(type.Name.Identifier())
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
            .WithMembers(SyntaxFactory.List(members));

        if (type.TypeParameters.Length is not 0)
            declaration = declaration.WithTypeParameterList(SyntaxFactory.TypeParameterList(
                SyntaxFactory.SeparatedList(type.TypeParameters.Select(static p => SyntaxFactory.TypeParameter(p.Name.Identifier())))));

        return declaration;
    }

    private static SwitchSectionSyntax GenerateSwitchSection(DataAdapterMethodEntry entry)
    {
        var               invocation = CreateInvocation(entry);
        StatementSyntax[] statements;

        if (entry.IsFunction)
        {
            var checkRet = CreateCheckRet(entry, invocation);
            statements =
            [
                SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    ReturnParameter.IdentifierName(), checkRet)),
                SyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)),
            ];
        }
        else
        {
            statements =
            [
                SyntaxFactory.ExpressionStatement(invocation),
                SyntaxFactory.ReturnStatement(),
            ];
        }

        return SyntaxFactory.SwitchSection()
            .WithLabels(SyntaxFactory.SingletonList<SwitchLabelSyntax>(
                SyntaxFactory.CaseSwitchLabel(IntLiteral(entry.Id))))
            .WithStatements(SyntaxFactory.List(statements));
    }

    private static SwitchSectionSyntax GenerateDefaultSection(
        int arity,
        bool function)
    {
        var exceptionType = GetGlobalName("Dalamud", "Plugin", "Ipc", "AdapterMethodMissingException");
        var exception = SyntaxFactory.ObjectCreationExpression(exceptionType)
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            [
                SyntaxFactory.Argument(MethodParameter.IdentifierName()),
                SyntaxFactory.Argument(IntLiteral(arity)),
                SyntaxFactory.Argument(BoolLiteral(function)),
            ])));

        return SyntaxFactory.SwitchSection()
            .WithLabels(SyntaxFactory.SingletonList<SwitchLabelSyntax>(SyntaxFactory.DefaultSwitchLabel()))
            .WithStatements(SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.ThrowStatement(exception)));
    }

    private static ExpressionSyntax CreateInvocation(in DataAdapterMethodEntry entry)
    {
        if (entry.Method.MethodKind is MethodKind.PropertyGet)
        {
            var property = (IPropertySymbol)entry.Method.AssociatedSymbol!;
            if (!property.IsIndexer)
                return property.Name.IdentifierName();

            var arguments = new ArgumentSyntax[property.Parameters.Length];
            for (var i = 0; i < property.Parameters.Length; ++i)
                arguments[i] = SyntaxFactory.Argument(CreateCheckValue(entry, property.Parameters[i], i));

            return SyntaxFactory.ElementAccessExpression(SyntaxFactory.ThisExpression())
                .WithArgumentList(SyntaxFactory.BracketedArgumentList(SyntaxFactory.SeparatedList(arguments)));
        }
        else
        {
            var arguments = new ArgumentSyntax[entry.Method.Parameters.Length];
            for (var i = 0; i < entry.Method.Parameters.Length; ++i)
                arguments[i] = SyntaxFactory.Argument(CreateCheckValue(entry, entry.Method.Parameters[i], i));

            return SyntaxFactory.InvocationExpression(entry.Method.Name.IdentifierName())
                .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));
        }
    }

    private static InvocationExpressionSyntax CreateCheckValue(in DataAdapterMethodEntry entry, IParameterSymbol parameter, int argumentIndex)
    {
        var method = SyntaxFactory.GenericName("CheckValue".Identifier())
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                SyntaxFactory.SeparatedList<TypeSyntax>([TypeNames[argumentIndex].IdentifierName(), parameter.Type.GetTypeSyntax()])));

        var valueArgument = SyntaxFactory.Argument(ParameterNames[argumentIndex].IdentifierName());

        // Reference types go through CheckValue<TArg, TOut>(..., argument)
        // Value types go through CheckValue<TArg, TOut>(..., ref argument)
        if (!parameter.Type.IsReferenceType)
            valueArgument = valueArgument.WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.RefKeyword));

        return SyntaxFactory.InvocationExpression(method)
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            [
                SyntaxFactory.Argument(MethodParameter.IdentifierName()),
                SyntaxFactory.Argument(IntLiteral(entry.Arity)),
                SyntaxFactory.Argument(BoolLiteral(entry.IsFunction)),
                SyntaxFactory.Argument(IntLiteral(argumentIndex)),
                valueArgument,
            ])));
    }

    private static InvocationExpressionSyntax CreateCheckRet(in DataAdapterMethodEntry entry, ExpressionSyntax value)
    {
        TypeSyntax[] typeArguments = entry.Method.ReturnType.IsReferenceType
            ? [ReturnType.IdentifierName()]
            : [ReturnType.IdentifierName(), entry.Method.ReturnType.GetTypeSyntax()];

        var method = SyntaxFactory.GenericName("CheckRet".Identifier())
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(typeArguments)));
        var methodId   = SyntaxFactory.Argument(MethodParameter.IdentifierName());
        var arity      = SyntaxFactory.Argument(IntLiteral(entry.Arity));
        var valueParam = SyntaxFactory.Argument(value);
        var arguments = entry.Method.ReturnType.IsReferenceType
            ? SyntaxFactory.SeparatedList([methodId, arity, valueParam, SyntaxFactory.Argument(BoolLiteral(entry.DisposeOnFailure))])
            : SyntaxFactory.SeparatedList([methodId, arity, valueParam]);

        return SyntaxFactory.InvocationExpression(method)
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));
    }

    private static NameSyntax GetGlobalName(params string[] parts)
    {
        if (parts.Length is 0)
            throw new ArgumentException("At least one name is required.", nameof(parts));

        NameSyntax name = SyntaxFactory.AliasQualifiedName(SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword)),
            parts[0].IdentifierName());
        for (var i = 1; i < parts.Length; ++i)
            name = SyntaxFactory.QualifiedName(name, parts[i].IdentifierName());
        return name;
    }

    private static TypeParameterConstraintClauseSyntax AllowsRefStructConstraint(string typeParameter)
        => SyntaxFactory.TypeParameterConstraintClause(
                typeParameter.IdentifierName())
            .WithConstraints(
                SyntaxFactory.SingletonSeparatedList<TypeParameterConstraintSyntax>(
                    SyntaxFactory.AllowsConstraintClause(
                        SyntaxFactory.SingletonSeparatedList<AllowsConstraintSyntax>(
                            SyntaxFactory.RefStructConstraint()))));

    private static bool IsCandidate(SyntaxNode node, CancellationToken token)
        => node is MethodDeclarationSyntax or PropertyDeclarationSyntax or AccessorDeclarationSyntax;

    private static IMethodSymbol? GetTypeToGenerate(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        if (context.TargetSymbol is IMethodSymbol m)
            return m;
        if (context.TargetSymbol is IPropertySymbol p)
            return p.GetMethod;

        return null;
    }

    private static LiteralExpressionSyntax BoolLiteral(bool value)
        => SyntaxFactory.LiteralExpression(value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);

    private static LiteralExpressionSyntax IntLiteral(int value)
        => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(value));

    private static NameSyntax GetNamespaceSyntax(INamespaceSymbol ns)
    {
        var        names = GetNamespaceParts(ns);
        NameSyntax name  = names[0].IdentifierName();
        for (var i = 1; i < names.Count; ++i)
            name = SyntaxFactory.QualifiedName(name, names[i].IdentifierName());
        return name;

        static IReadOnlyList<string> GetNamespaceParts(INamespaceSymbol ns)
        {
            var parts = new List<string>();
            for (var current = ns; !current.IsGlobalNamespace; current = current.ContainingNamespace)
                parts.Add(current.Name);
            parts.Reverse();
            return parts;
        }
    }
}
