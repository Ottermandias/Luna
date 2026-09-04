using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable RS2008

namespace Luna.Generators;

internal sealed class ArityRewriter(int arity) : CSharpSyntaxRewriter
{
    private const string MainType      = "T1";
    private const string MainParameter = "a1";

    /// <summary> Converts type parameter lists like {T1,TRet} to {T1, T2, ..., TRet}. </summary>
    public override SyntaxNode? VisitTypeParameterList(TypeParameterListSyntax node)
    {
        var parameters = new List<TypeParameterSyntax>();

        foreach (var parameter in node.Parameters)
        {
            if (parameter.Identifier.ValueText is MainType)
                for (var i = 1; i <= arity; ++i)
                    parameters.Add(parameter.WithIdentifier(Rename(parameter.Identifier, TypeName(i))));
            else
                parameters.Add((TypeParameterSyntax)Visit(parameter));
        }

        return parameters.Count is 0 ? null : node.WithParameters(SyntaxFactory.SeparatedList(parameters));
    }

    /// <summary> Converts generic parameter lists like (in T1 a) to (in T1 a, in T2 b, ...). </summary>
    public override SyntaxNode? VisitParameterList(ParameterListSyntax node)
    {
        var parameters = new List<ParameterSyntax>();

        foreach (var parameter in node.Parameters)
        {
            if (IsExpandableParameter(parameter))
                for (var i = 1; i <= arity; ++i)
                {
                    var rewritten = (ParameterSyntax)new SlotRewriter(i).Visit(parameter);
                    rewritten = rewritten.WithIdentifier(Rename(rewritten.Identifier, ValueName(i)));
                    parameters.Add(rewritten);
                }
            else
                parameters.Add((ParameterSyntax)Visit(parameter));
        }

        return node.WithParameters(SyntaxFactory.SeparatedList(parameters));
    }

    /// <summary> Converts generic type lists like Action{T1} to Action{T1, T2, ...}. </summary>
    public override SyntaxNode VisitTypeArgumentList(TypeArgumentListSyntax node)
    {
        var arguments = new List<TypeSyntax>();

        foreach (var argument in node.Arguments)
        {
            if (ContainsMainTypeAtThisLevel(argument))
                for (var i = 1; i <= arity; ++i)
                    arguments.Add((TypeSyntax)new SlotRewriter(i).Visit(argument));
            else
                // Recursion for nested generics.
                arguments.Add((TypeSyntax)Visit(argument));
        }

        return node.WithArguments(SyntaxFactory.SeparatedList(arguments));
    }

    /// <summary> Removes empty type argument lists for zero-arity. </summary>
    public override SyntaxNode VisitGenericName(GenericNameSyntax node)
    {
        var result = (GenericNameSyntax)base.VisitGenericName(node)!;
        return result.TypeArgumentList.Arguments.Count is not 0
            ? result
            : SyntaxFactory.IdentifierName(result.Identifier).WithTriviaFrom(result);
    }

    /// <summary> Converts parameter lists, possibly with indirections, like func(a) to func(a, b, ...) or check(a.X) to check(a.X, b.X, ...). </summary>
    public override SyntaxNode VisitArgumentList(ArgumentListSyntax node)
        => node.WithArguments(ExpandArguments(node.Arguments));

    /// <summary> Converts this-access parameter lists, possibly with indirections, like this[a] to this[a, b, ...]. </summary>
    public override SyntaxNode VisitBracketedArgumentList(BracketedArgumentListSyntax node)
        => node.WithArguments(ExpandArguments(node.Arguments));

    /// <summary> Converts constraint clauses like `where T1 : allows ref struct`. </summary>
    public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var result = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;
        // Use original clauses to avoid doubling the expansion.
        // Constraint clauses are a SyntaxList instead of a SeparatedSyntaxList.
        return result.WithConstraintClauses(ExpandConstraints(node.ConstraintClauses));
    }

    private SyntaxList<TypeParameterConstraintClauseSyntax> ExpandConstraints(SyntaxList<TypeParameterConstraintClauseSyntax> constraints)
    {
        var result = new List<TypeParameterConstraintClauseSyntax>();
        foreach (var constraint in constraints)
        {
            if (constraint.Name.Identifier.ValueText == MainType)
                for (var i = 1; i <= arity; ++i)
                    result.Add((TypeParameterConstraintClauseSyntax)new SlotRewriter(i).Visit(constraint));
            else
                result.Add((TypeParameterConstraintClauseSyntax)Visit(constraint));
        }

        return SyntaxFactory.List(result);
    }

    private SeparatedSyntaxList<ArgumentSyntax> ExpandArguments(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        var result = new List<ArgumentSyntax>();
        foreach (var argument in arguments)
        {
            if (ContainsMainParameterAtThisLevel(argument.Expression))
                for (var i = 1; i <= arity; ++i)
                    result.Add((ArgumentSyntax)new SlotRewriter(i).Visit(argument));
            else
                result.Add((ArgumentSyntax)Visit(argument));
        }

        return SyntaxFactory.SeparatedList(result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsExpandableParameter(ParameterSyntax parameter)
        => parameter.Identifier.ValueText is MainParameter && parameter.Type is not null && ContainsMainType(parameter.Type);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsMainType(SyntaxNode node)
        => node.DescendantNodesAndSelf()
            .OfType<IdentifierNameSyntax>()
            .Any(static n => n.Identifier.ValueText is MainType);

    /// <summary> Find the expandable type belonging to this generic argument rather than one contained inside another generic argument list. </summary>
    private static bool ContainsMainTypeAtThisLevel(TypeSyntax type)
    {
        foreach (var identifier in type.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
        {
            if (identifier.Identifier.ValueText is not MainType)
                continue;

            if (identifier == type)
                return true;

            var nested = identifier
                .Ancestors()
                .TakeWhile(n => n != type)
                .Any(static n => n is TypeArgumentListSyntax);

            if (!nested)
                return true;
        }

        return false;
    }

    /// <summary> Find the expandable invocation argument to the most nested method rather than one before. </summary>
    private static bool ContainsMainParameterAtThisLevel(ExpressionSyntax expression)
    {
        foreach (var identifier in expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
        {
            if (identifier.Identifier.ValueText != MainParameter)
                continue;

            if (identifier == expression)
                return true;

            var nested = identifier
                .Ancestors()
                .TakeWhile(n => n != expression)
                .Any(static n => n is ArgumentListSyntax or BracketedArgumentListSyntax);

            if (!nested)
                return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string TypeName(int index)
        => MainType.Replace("1", index.ToString());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ValueName(int index)
        => MainParameter.Replace("1", index.ToString());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SyntaxToken Rename(SyntaxToken token, string name)
        => SyntaxFactory.Identifier(token.LeadingTrivia, name, token.TrailingTrivia);

    private sealed class SlotRewriter(int slot) : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
            => node.Identifier.ValueText switch
            {
                MainType      => node.WithIdentifier(Rename(node.Identifier, TypeName(slot))),
                MainParameter => node.WithIdentifier(Rename(node.Identifier, ValueName(slot))),
                _             => base.VisitIdentifierName(node),
            };
    }

    public static bool ValidateTemplate(SourceProductionContext context, IMethodSymbol method)
    {
        if (method.TypeParameters.FirstOrDefault(static p => p.Name is MainType) is not { } mainType)
            return Invalid($"it must have a type parameter named {MainType}");

        if (method.Parameters.FirstOrDefault(p => p.Name is MainParameter) is not { } mainParameter)
            return Invalid($"it must have a parameter named {MainParameter}");

        if (!SymbolEqualityComparer.Default.Equals(mainParameter.Type, mainType))
            return Invalid($"parameter {MainParameter} must have type {MainType}");

        return true;

        bool Invalid(string reason)
        {
            context.ReportDiagnostic(Diagnostic.Create(InvalidTemplate, method.Locations.FirstOrDefault(), method.ToDisplayString(), reason));
            return false;
        }
    }

    public static bool ValidateTemplate(SourceProductionContext context, INamedTypeSymbol type)
    {
        if (type.TypeParameters.Any(static p => p.Name is MainType))
            return true;

        context.ReportDiagnostic(Diagnostic.Create(InvalidTemplate, type.Locations.FirstOrDefault(), type.ToDisplayString(), $"it must have a type parameter named {MainType}"));
        return false;
    }

    private static readonly DiagnosticDescriptor InvalidTemplate = new(
        "LUNAARITY001",
        "Invalid arity template",
        "Method '{0}' can not be used as an arity template: {1}",
        "Luna.Generators",
        DiagnosticSeverity.Error,
        true);
}
