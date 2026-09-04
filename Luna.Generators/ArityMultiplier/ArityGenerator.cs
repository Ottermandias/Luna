using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Luna.Generators;

[Generator]
public sealed class ArityGenerator : IIncrementalGenerator
{
    private readonly struct ArityTemplate(MemberDeclarationSyntax template, ISymbol symbol, int maximumArity, bool includeZeroArity)
    {
        public readonly MemberDeclarationSyntax Template         = template;
        public readonly ISymbol                 Symbol           = symbol;
        public readonly int                     MaximumArity     = maximumArity;
        public readonly bool                    IncludeZeroArity = includeZeroArity;
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static context
            => context.AddSource($"{nameof(GenerateAritiesAttribute)}.g.cs",
                GenerateAritiesAttribute.CreateAttribute().GetText(Encoding.UTF8)));

        var methods = context.SyntaxProvider
            .ForAttributeWithMetadataName(GenerateAritiesAttribute.MetadataName, IsCandidate, GetTypeToGenerate);

        context.RegisterSourceOutput(methods, Generate);
    }

    private static void Generate(SourceProductionContext context, ArityTemplate value)
    {
        switch (value.Template, value.Symbol)
        {
            case (MethodDeclarationSyntax method, IMethodSymbol symbol):  GenerateMethod(context, value, method, symbol); break;
            case (ClassDeclarationSyntax type, INamedTypeSymbol symbol):  GenerateType(context, value, type, symbol); break;
            case (StructDeclarationSyntax type, INamedTypeSymbol symbol): GenerateType(context, value, type, symbol); break;
        }
    }

    private static void GenerateMethod(SourceProductionContext context, in ArityTemplate value, MethodDeclarationSyntax template,
        IMethodSymbol symbol)
    {
        if (!ArityRewriter.ValidateTemplate(context, symbol))
            return;

        var generated = new List<MemberDeclarationSyntax>();
        if (value.IncludeZeroArity)
        {
            var method = (MethodDeclarationSyntax)new ArityRewriter(0).Visit(template);
            method = RemoveGenerateAritiesAttribute(method);
            method = method.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Generated())));
            generated.Add(method);
        }

        for (var arity = 2; arity <= value.MaximumArity; ++arity)
        {
            var method = (MethodDeclarationSyntax)new ArityRewriter(arity).Visit(template);
            method = RemoveGenerateAritiesAttribute(method);
            method = method.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Generated())));
            generated.Add(method);
        }

        var root   = template.SyntaxTree.GetCompilationUnitRoot();
        var source = symbol.ContainingType.WrapInPartialType(generated, root.Usings);
        source = (CompilationUnitSyntax)new FormattingRewriter(template).Visit(source);
        var typeParameters = string.Join("_", symbol.TypeParameters.Select(static p => p.Name));
        context.AddSource($"{symbol.ContainingType.Name}.{symbol.Name}.{typeParameters}.g.cs", source.GetText(Encoding.UTF8));
    }

    private static void GenerateType(SourceProductionContext context, in ArityTemplate value, TypeDeclarationSyntax template,
        INamedTypeSymbol symbol)
    {
        if (!ArityRewriter.ValidateTemplate(context, symbol))
            return;

        var generated = new List<MemberDeclarationSyntax>();
        if (value.IncludeZeroArity)
        {
            var type = (TypeDeclarationSyntax)new ArityRewriter(0).Visit(template);
            type = RemoveGenerateAritiesAttribute(type);
            type = (TypeDeclarationSyntax)new GeneratedAnnotationRewriter().Visit(type);
            generated.Add(type);
        }

        for (var arity = 2; arity <= value.MaximumArity; ++arity)
        {
            var type = (TypeDeclarationSyntax)new ArityRewriter(arity).Visit(template);
            type = RemoveGenerateAritiesAttribute(type);
            type = (TypeDeclarationSyntax)new GeneratedAnnotationRewriter().Visit(type);
            generated.Add(type);
        }

        var                   root = template.SyntaxTree.GetCompilationUnitRoot();
        CompilationUnitSyntax source;
        if (symbol.ContainingType is { } parentType)
            source = parentType.WrapInPartialType(generated, root.Usings);
        else
            source = template.WrapInContainingNamespaces(generated, root).PrepareGeneratedSource();


        context.AddSource($"{symbol.Name}.Arities.g.cs", source.NormalizeWhitespace().GetText(Encoding.UTF8));
    }

    private static bool IsCandidate(SyntaxNode node, CancellationToken token)
        => node is MethodDeclarationSyntax or ClassDeclarationSyntax or StructDeclarationSyntax;

    private static ArityTemplate GetTypeToGenerate(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        var method           = (MemberDeclarationSyntax)context.TargetNode;
        var symbol           = context.TargetSymbol;
        var maxArity         = (int)context.Attributes[0].ConstructorArguments[0].Value!;
        var includeZeroArity = context.Attributes[0].GetNamedArgument("IncludeZeroArity") is true;
        return new ArityTemplate(method, symbol, maxArity, includeZeroArity);
    }

    private static MethodDeclarationSyntax RemoveGenerateAritiesAttribute(MethodDeclarationSyntax method)
    {
        var leading = method.GetLeadingTrivia();
        return method.WithAttributeLists(RemoveGenerateAritiesAttribute(method.AttributeLists))
            .WithLeadingTrivia(leading);
    }

    private static TypeDeclarationSyntax RemoveGenerateAritiesAttribute(TypeDeclarationSyntax type)
    {
        var leading = type.GetLeadingTrivia();
        return type.WithAttributeLists(RemoveGenerateAritiesAttribute(type.AttributeLists))
            .WithLeadingTrivia(leading);
    }

    private static SyntaxList<AttributeListSyntax> RemoveGenerateAritiesAttribute(SyntaxList<AttributeListSyntax> lists)
    {
        var result = new List<AttributeListSyntax>();

        foreach (var list in lists)
        {
            var attributes = list.Attributes
                .Where(static a => !IsGenerateAritiesAttribute(a))
                .ToArray();

            if (attributes.Length is not 0)
                result.Add(list.WithAttributes(SyntaxFactory.SeparatedList(attributes)));
        }

        return SyntaxFactory.List(result);
    }

    private static bool IsGenerateAritiesAttribute(AttributeSyntax attribute)
    {
        var name = attribute.Name switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            QualifiedNameSyntax qualified   => qualified.Right.Identifier.ValueText,
            AliasQualifiedNameSyntax alias  => alias.Name.Identifier.ValueText,
            _                               => attribute.Name.ToString(),
        };

        return name is "GenerateArities" or "GenerateAritiesAttribute";
    }
}
