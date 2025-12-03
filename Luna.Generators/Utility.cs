using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Luna.Generators;

internal static class Utility
{
    /// <summary> Search for the name of a value in an Enum by the constant value. </summary>
    /// <param name="enumType"> The enum type. </param>
    /// <param name="value"> The constant value. </param>
    /// <returns> The name of the value if it could be found. </returns>
    public static string? GetEnumNameByValue(ITypeSymbol? enumType, object? value)
    {
        if (value is not { } v)
            return null;

        return enumType?.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(m => m.HasConstantValue && m.ConstantValue.Equals(value))?.Name;
    }

    /// <summary> Obtain the full containing namespace of a type object. </summary>
    public static string GetFullNamespace(ITypeSymbol? type)
    {
        if (type is null)
            return string.Empty;

        var currentNamespace = type.ContainingNamespace;
        if (currentNamespace.IsGlobalNamespace)
            return string.Empty;

        var ret = currentNamespace.Name;
        currentNamespace = currentNamespace.ContainingNamespace;
        while (!currentNamespace.IsGlobalNamespace)
        {
            ret              = $"{currentNamespace.Name}.{ret}";
            currentNamespace = currentNamespace.ContainingNamespace;
        }

        return ret;
    }

    /// <summary> Find the first attribute matching the given attribute name on a symbol. </summary>
    /// <param name="compilation"> The compilation to fetch the type by its name. </param>
    /// <param name="symbol"> The declaration symbol in which the attribute is searched for. </param>
    /// <param name="attributeName"> The fully qualified name of the attribute's type. </param>
    /// <returns> The attribute data if it could be found. </returns>
    public static AttributeData? FindAttribute(Compilation compilation, ISymbol symbol, string attributeName)
        => FindAttribute(symbol, compilation.GetTypeByMetadataName(attributeName));

    /// <summary> Find all occurrences of a generic attribute regardless of type parameter on a symbol. </summary>
    /// <param name="compilation"> The compilation to fetch the type by its name. </param>
    /// <param name="symbol"> The declaration symbol in which the attributes are searched for. </param>
    /// <param name="attributeName"> The fully qualified generic name of the attribute. </param>
    /// <returns> All matching attributes. </returns>
    public static IEnumerable<AttributeData> FindGenericAttributes(Compilation compilation, ISymbol symbol, string attributeName)
        => FindGenericAttributes(symbol, compilation.GetTypeByMetadataName(attributeName));

    /// <summary> Find the first attribute matching the given named type on a symbol. </summary>
    /// <param name="symbol"> The declaration symbol in which the attribute is searched for. </param>
    /// <param name="needle"> The attribute type to search for. </param>
    /// <returns> The attribute data if it could be found. </returns>
    public static AttributeData? FindAttribute(ISymbol symbol, INamedTypeSymbol? needle)
    {
        return needle is null
            ? null
            : symbol.GetAttributes().FirstOrDefault(a => needle.Equals(a.AttributeClass, SymbolEqualityComparer.Default));
    }

    /// <summary> Find all occurrences of a generic attribute regardless of type parameter on a symbol. </summary>
    /// <param name="symbol"> The declaration symbol in which the attributes are searched for. </param>
    /// <param name="needle"> The attribute type to search for. </param>
    /// <returns> All matching attributes. </returns>
    public static IEnumerable<AttributeData> FindGenericAttributes(ISymbol symbol, INamedTypeSymbol? needle)
    {
        if (needle is null)
            yield break;

        var unboundNeedle = needle.ConstructUnboundGenericType();
        var attributes    = symbol.GetAttributes();
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass?.IsGenericType is true
             && unboundNeedle.Equals(attribute.AttributeClass.ConstructUnboundGenericType(), SymbolEqualityComparer.Default))
                yield return attribute;
        }
    }

    /// <summary> Add initialization files based on given code and names. </summary>
    /// <param name="context"> The context to add the output to. </param>
    /// <param name="code"> The code to write to the file. </param>
    /// <param name="name"> The name used for the file. </param>
    public static void AddTagAttributes(ref IncrementalGeneratorInitializationContext context, string code, string name)
    {
        context.RegisterPostInitializationOutput(ctx
            => ctx.AddSource($"{name}.g.cs", SourceText.From(code, Encoding.UTF8)));
    }

    /// <summary> Generate code for specifically tagged attributes using a generator and executor. </summary>
    /// <typeparam name="T"> The intermediate type that contains descriptive data for the generation. Should be equality-comparable and will be cached. </typeparam>
    /// <param name="context"> The context to generate code for. </param>
    /// <param name="name"> The name of the generated attribute tag, assumed to lie in the Luna.Generators namespace. </param>
    /// <param name="generator"> The generator. </param>
    /// <param name="executor"> The executor. </param>
    public static void Generate<T>(ref IncrementalGeneratorInitializationContext context, string name,
        Func<GeneratorAttributeSyntaxContext, CancellationToken, T?> generator, Action<SourceProductionContext, T> executor) where T : struct
    {
        var enumsToGenerate = context.SyntaxProvider
            .ForAttributeWithMetadataName($"Luna.Generators.{name}", static (_, _) => true, generator)
            .Where(e => e.HasValue)
            .Select((e, _) => e!.Value);

        context.RegisterSourceOutput(enumsToGenerate, executor);
    }

    /// <summary> Convert an accessibility type to its modifier representation with a trailing space. </summary>
    public static string ToModifier(this Accessibility accessibility)
        => accessibility switch
        {
            Accessibility.NotApplicable        => string.Empty,
            Accessibility.Private              => "private ",
            Accessibility.ProtectedAndInternal => "private protected ",
            Accessibility.Protected            => "protected ",
            Accessibility.Internal             => "internal ",
            Accessibility.ProtectedOrInternal  => "protected internal ",
            Accessibility.Public               => "public ",
            _                                  => string.Empty,
        };

    /// <summary> Construct a C# string literal of the given value. </summary>
    public static string ToLiteral(this string input)
        => SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(input)).ToFullString();
}
