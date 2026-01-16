using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

    /// <summary> Find the first attribute matching the given attribute name in a collection. </summary>
    /// <param name="compilation"> The compilation to fetch the type by its name. </param>
    /// <param name="attributes"> The collection in which the attribute is searched for. </param>
    /// <param name="attributeName"> The fully qualified name of the attribute's type. </param>
    /// <returns> The attribute data if it could be found. </returns>
    /// <remarks> Can be used with <see cref="IMethodSymbol.GetReturnTypeAttributes"/>. </remarks>
    public static AttributeData? FindAttribute(Compilation compilation, ImmutableArray<AttributeData> attributes, string attributeName)
        => FindAttribute(attributes, compilation.GetTypeByMetadataName(attributeName));

    /// <summary> Find all occurrences of a generic attribute regardless of type parameter on a symbol. </summary>
    /// <param name="compilation"> The compilation to fetch the type by its name. </param>
    /// <param name="symbol"> The declaration symbol in which the attributes are searched for. </param>
    /// <param name="attributeName"> The fully qualified generic name of the attribute. </param>
    /// <returns> All matching attributes. </returns>
    public static IEnumerable<AttributeData> FindGenericAttributes(Compilation compilation, ISymbol symbol, string attributeName)
        => FindGenericAttributes(symbol, compilation.GetTypeByMetadataName(attributeName));

    /// <summary> Find all occurrences of a generic attribute regardless of type parameter in a collection. </summary>
    /// <param name="compilation"> The compilation to fetch the type by its name. </param>
    /// <param name="attributes"> The collection in which the attributes are searched for. </param>
    /// <param name="attributeName"> The fully qualified generic name of the attribute. </param>
    /// <returns> All matching attributes. </returns>
    /// <remarks> Can be used with <see cref="IMethodSymbol.GetReturnTypeAttributes"/>. </remarks>
    public static IEnumerable<AttributeData> FindGenericAttributes(Compilation compilation, ImmutableArray<AttributeData> attributes,
        string attributeName)
        => FindGenericAttributes(attributes, compilation.GetTypeByMetadataName(attributeName));

    /// <summary> Find the first attribute matching the given named type on a symbol. </summary>
    /// <param name="symbol"> The declaration symbol in which the attribute is searched for. </param>
    /// <param name="needle"> The attribute type to search for. </param>
    /// <returns> The attribute data if it could be found. </returns>
    public static AttributeData? FindAttribute(ISymbol symbol, INamedTypeSymbol? needle)
        => FindAttribute(symbol.GetAttributes(), needle);

    /// <summary> Find the first attribute matching the given named type in a collection. </summary>
    /// <param name="attributes"> The collection in which the attribute is searched for. </param>
    /// <param name="needle"> The attribute type to search for. </param>
    /// <returns> The attribute data if it could be found. </returns>
    /// <remarks> Can be used with <see cref="IMethodSymbol.GetReturnTypeAttributes"/>. </remarks>
    public static AttributeData? FindAttribute(ImmutableArray<AttributeData> attributes, INamedTypeSymbol? needle)
    {
        return needle is null
            ? null
            : attributes.FirstOrDefault(a => needle.Equals(a.AttributeClass, SymbolEqualityComparer.Default));
    }

    /// <summary> Find all occurrences of a generic attribute regardless of type parameter on a symbol. </summary>
    /// <param name="symbol"> The declaration symbol in which the attributes are searched for. </param>
    /// <param name="needle"> The attribute type to search for. </param>
    /// <returns> All matching attributes. </returns>
    public static IEnumerable<AttributeData> FindGenericAttributes(ISymbol symbol, INamedTypeSymbol? needle)
        => FindGenericAttributes(symbol.GetAttributes(), needle);

    /// <summary> Find all occurrences of a generic attribute regardless of type parameter in a collection. </summary>
    /// <param name="attributes"> The collection in which the attributes are searched for. </param>
    /// <param name="needle"> The attribute type to search for. </param>
    /// <returns> All matching attributes. </returns>
    /// <remarks> Can be used with <see cref="IMethodSymbol.GetReturnTypeAttributes"/>. </remarks>
    public static IEnumerable<AttributeData> FindGenericAttributes(ImmutableArray<AttributeData> attributes, INamedTypeSymbol? needle)
    {
        if (needle is null)
            yield break;

        var unboundNeedle = needle.ConstructUnboundGenericType();
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass?.IsGenericType is true
             && unboundNeedle.Equals(attribute.AttributeClass.ConstructUnboundGenericType(), SymbolEqualityComparer.Default))
                yield return attribute;
        }
    }

    /// <summary> Retrieves a named (property value) argument from an attribute. </summary>
    /// <param name="attribute"> The attribute to retrieve information from. </param>
    /// <param name="name"> The property to retrieve. </param>
    /// <returns> The property's value, or null if not found or if <paramref name="attribute"/> was null. </returns>
    /// <remarks>
    /// This consumes the historical attribute property syntax (for example <c>Property = "Value"</c>),
    /// not the named constructor argument syntax (for example <c>parameter: "value"</c>).
    /// </remarks>
    public static object? GetNamedArgument(this AttributeData? attribute, string name)
        => attribute?.NamedArguments.FirstOrDefault(kv => kv.Key.Equals(name, StringComparison.Ordinal)).Value.Value;

    /// <summary> Determines whether the given symbol shadows a symbol from the parent class using the <c>new</c> keyword. </summary>
    /// <param name="symbol"> The symbol to inspect. </param>
    /// <param name="token"> A cancellation token for the parse that may be caused by this function. </param>
    /// <returns> Whether the given symbol is declared with the <c>new</c> keyword. </returns>
    /// <remarks> This function may cause a parse to happen to recover syntax data. </remarks>
    public static bool IsNew(ISymbol symbol, CancellationToken token)
        => symbol.DeclaringSyntaxReferences[0].GetSyntax(token).ChildTokens().Any(static token => token.Kind() is SyntaxKind.NewKeyword);

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
    /// <param name="predicate"> The predicate for attributes. If this is null, an always true predicate will be used. </param>
    /// <param name="executor"> The executor. </param>
    public static void Generate<T>(ref IncrementalGeneratorInitializationContext context, string name,
        Func<GeneratorAttributeSyntaxContext, CancellationToken, T?> generator, Action<SourceProductionContext, T> executor,
        Func<SyntaxNode, CancellationToken, bool>? predicate = null) where T : struct
    {
        var enumsToGenerate = context.SyntaxProvider
            .ForAttributeWithMetadataName($"Luna.Generators.{name}", predicate ?? (static (_, _) => true), generator)
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

    /// <summary> Convert a type kind to the C# keyword representing it. </summary>
    public static string ToKeyword(this TypeKind typeKind)
        => typeKind switch
        {
            TypeKind.Class     => "class",
            TypeKind.Struct    => "struct",
            TypeKind.Interface => "interface",
            _                  => "class",
        };

    /// <summary> Construct a C# string literal of the given value. </summary>
    public static string ToLiteral(this string input)
        => SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(input)).ToFullString();

    /// <summary> Obtain the fully qualified metadata name of a specific symbol. </summary>
    /// <param name="symbol"> The symbol. </param>
    /// <returns> The fully qualified metadata name. </returns>
    public static string FullyQualifiedMetadataName(this ITypeSymbol symbol)
        => new StringBuilder().AppendFullyQualifiedMetadataName(symbol).ToString();

    /// <summary> Append the fully qualified metadata name of a specific symbol to a string. </summary>
    /// <param name="builder"> The string builder. </param>
    /// <param name="symbol"> The symbol. </param>
    /// <returns> The fully qualified metadata name. </returns>
    /// <remarks>> Adapted from MVVM Community Toolkit. </remarks>
    public static StringBuilder AppendFullyQualifiedMetadataName(this StringBuilder builder, ITypeSymbol symbol)
    {
        BuildFrom(symbol, builder);
        return builder;

        static void BuildFrom(ISymbol? symbol, StringBuilder builder)
        {
            switch (symbol)
            {
                case INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: false }:
                    BuildFrom(symbol.ContainingNamespace, builder);
                    builder.Append('.');
                    builder.Append(symbol.MetadataName);
                    break;

                case INamespaceSymbol { IsGlobalNamespace: false }:
                case ITypeSymbol { ContainingSymbol      : INamespaceSymbol { IsGlobalNamespace: true } }:
                    builder.Append(symbol.MetadataName);
                    break;

                case ITypeSymbol { ContainingSymbol: INamespaceSymbol namespaceSymbol }:
                    BuildFrom(namespaceSymbol, builder);
                    builder.Append('.');
                    builder.Append(symbol.MetadataName);
                    break;

                case ITypeSymbol { ContainingSymbol: ITypeSymbol typeSymbol }:
                    BuildFrom(typeSymbol, builder);
                    builder.Append('+');
                    builder.Append(symbol.MetadataName);
                    break;
            }
        }
    }

    /// <summary>
    /// Groups items in a given <see cref="IncrementalValuesProvider{TValue}"/> sequence by a specified key.
    /// </summary>
    /// <typeparam name="TLeft">The type of left items in each tuple.</typeparam>
    /// <typeparam name="TRight">The type of right items in each tuple.</typeparam>
    /// <typeparam name="TKey">The type of resulting key elements.</typeparam>
    /// <typeparam name="TElement">The type of resulting projected elements.</typeparam>
    /// <param name="source">The input <see cref="IncrementalValuesProvider{TValues}"/> instance.</param>
    /// <param name="keySelector">The key selection <see cref="Func{T, TResult}"/>.</param>
    /// <param name="elementSelector">The element selection <see cref="Func{T, TResult}"/>.</param>
    /// <returns>An <see cref="IncrementalValuesProvider{TValues}"/> with the grouped results.</returns>
    /// <remarks>> Adapted from MVVM Community Toolkit. </remarks>
    public static IncrementalValuesProvider<(TKey Key, ValueCollection<TElement> Right)> GroupBy<TLeft, TRight, TKey, TElement>(
        this IncrementalValuesProvider<(TLeft Left, TRight Right)> source,
        Func<(TLeft Left, TRight Right), TKey> keySelector,
        Func<(TLeft Left, TRight Right), TElement> elementSelector)
        where TLeft : IEquatable<TLeft>
        where TRight : IEquatable<TRight>
        where TKey : IEquatable<TKey>
        where TElement : IEquatable<TElement>
    {
        return source.Collect().SelectMany((item, token) =>
        {
            Dictionary<TKey, List<TElement>> map = new();

            foreach ((TLeft, TRight) pair in item)
            {
                var key     = keySelector(pair);
                var element = elementSelector(pair);

                if (!map.TryGetValue(key, out var list))
                {
                    list = [element];
                    map.Add(key, list);
                }
                else
                {
                    list.Add(element);
                }
            }

            token.ThrowIfCancellationRequested();

            var ret = map.Select(kvp => (kvp.Key, new ValueCollection<TElement>(kvp.Value))).ToList();
            return new ValueCollection<(TKey, ValueCollection<TElement>)>(ret);
        });
    }
}
