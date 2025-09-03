using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Luna.Generators;

internal static class Utility
{
    public static AttributeData? FindAttribute(Compilation compilation, ISymbol symbol, string attributeName)
        => FindAttribute(symbol, compilation.GetTypeByMetadataName(attributeName));

    public static AttributeData? FindAttribute(ISymbol symbol, INamedTypeSymbol? needle)
    {
        return needle is null
            ? null
            : symbol.GetAttributes().FirstOrDefault(a => needle.Equals(a.AttributeClass, SymbolEqualityComparer.Default));
    }

    public static IEnumerable<AttributeData> FindAttributes(ISymbol symbol, INamedTypeSymbol? needle)
    {
        return needle is null
            ? []
            : symbol.GetAttributes().Where(a => needle.Equals(a.AttributeClass, SymbolEqualityComparer.Default));
    }

    public static void AddTagEnums(ref IncrementalGeneratorInitializationContext context, string code, string name)
    {
        context.RegisterPostInitializationOutput(ctx
            => ctx.AddSource($"{name}.g.cs", SourceText.From(code, Encoding.UTF8)));
    }

    public static void Generate<T>(ref IncrementalGeneratorInitializationContext context, string name,
        Func<GeneratorAttributeSyntaxContext, CancellationToken, T?> generator, Action<SourceProductionContext, T> executor) where T : struct
    {
        var enumsToGenerate = context.SyntaxProvider
            .ForAttributeWithMetadataName($"Luna.Generators.{name}", static (_, _) => true, generator)
            .Where(e => e.HasValue)
            .Select((e, _) => e!.Value);

        context.RegisterSourceOutput(enumsToGenerate, executor);
    }
}
