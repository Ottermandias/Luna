using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Luna.Generators;

[Generator]
public sealed class ArityGenerator : IIncrementalGenerator
{
    private readonly struct ArityMethod(MethodDeclarationSyntax method, IMethodSymbol symbol, int maximumArity, bool includeZeroArity)
    {
        public readonly MethodDeclarationSyntax Method           = method;
        public readonly IMethodSymbol           Symbol           = symbol;
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

    private static void Generate(SourceProductionContext context, ArityMethod value)
    {
        if (!ArityRewriter.ValidateTemplate(context, value.Symbol))
            return;

        var generated = new List<MemberDeclarationSyntax>();
        if (value.IncludeZeroArity)
        {
            var method = (MethodDeclarationSyntax)new ArityRewriter(0).Visit(value.Method);
            generated.Add(RemoveGenerateAritiesAttribute(method));
        }

        for (var arity = 2; arity <= value.MaximumArity; ++arity)
        {
            var method = (MethodDeclarationSyntax)new ArityRewriter(arity).Visit(value.Method);
            method = RemoveGenerateAritiesAttribute(method);
            generated.Add(method);
        }

        var root   = value.Method.SyntaxTree.GetCompilationUnitRoot();
        var source = value.Symbol.ContainingType.WrapInPartialType(generated, root.Usings);
        source = (CompilationUnitSyntax)new FormattingRewriter(value.Method).Visit(source);
        var typeParameters = string.Join("_", value.Symbol.TypeParameters.Select(static p => p.Name));
        context.AddSource($"{value.Symbol.ContainingType.Name}.{value.Symbol.Name}.{typeParameters}.g.cs", source.GetText(Encoding.UTF8));
    }

    private static bool IsCandidate(SyntaxNode node, CancellationToken token)
        => node is MethodDeclarationSyntax;

    private static ArityMethod GetTypeToGenerate(
        GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        var method           = (MethodDeclarationSyntax)context.TargetNode;
        var symbol           = (IMethodSymbol)context.TargetSymbol;
        var maxArity         = (int)context.Attributes[0].ConstructorArguments[0].Value!;
        var includeZeroArity = context.Attributes[0].GetNamedArgument("IncludeZeroArity") is true;
        return new ArityMethod(method, symbol, maxArity, includeZeroArity);
    }

    private static MethodDeclarationSyntax RemoveGenerateAritiesAttribute(MethodDeclarationSyntax method)
    {
        var lists = new List<AttributeListSyntax>();

        foreach (var list in method.AttributeLists)
        {
            var attributes = list.Attributes.Where(static attribute => !IsGenerateAritiesAttribute(attribute)).ToArray();
            if (attributes.Length is not 0)
                lists.Add(list.WithAttributes(SyntaxFactory.SeparatedList(attributes)));
        }

        return method.WithAttributeLists(SyntaxFactory.List(lists));
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
