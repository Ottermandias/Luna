using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Luna.Generators;

internal readonly record struct StrongTypeData
{
    public readonly TypeDefinition Name;
    public readonly TypeDefinition BaseType;
    public readonly string         Namespace;
    public readonly Accessibility  Accessibility;
    public readonly string         FieldName;
    public readonly bool           OverrideToString;
    public readonly bool           EquatableSelf;
    public readonly bool           EquatableBase;
    public readonly bool           ComparableSelf;
    public readonly bool           ComparableBase;
    public readonly bool           Incrementable;
    public readonly bool           Decrementable;
    public readonly bool           AdditionSelf;
    public readonly bool           AdditionBase;
    public readonly bool           SubtractionSelf;
    public readonly bool           SubtractionBase;
    public readonly bool           ImplicitConversionFromBase;
    public readonly bool           ImplicitConversionToBase;
    public readonly bool           ConversionToBase;
    public readonly bool           NewtonsoftConverter;
    public readonly bool           SystemConverter;
    public readonly bool           HasZero;
    public readonly bool           HasOne;


    public StrongTypeData(INamedTypeSymbol name, INamedTypeSymbol baseType, string @namespace, ulong flags, Accessibility accessibility,
        string fieldName, bool overrideToString)
    {
        Name                       = new TypeDefinition(name);
        BaseType                   = new TypeDefinition(baseType);
        Namespace                  = @namespace;
        EquatableSelf              = flags >> 0 is not 0;
        EquatableBase              = flags >> 1 is not 0;
        ComparableSelf             = flags >> 2 is not 0;
        ComparableBase             = flags >> 3 is not 0;
        Incrementable              = flags >> 4 is not 0;
        Decrementable              = flags >> 5 is not 0;
        AdditionSelf               = flags >> 6 is not 0;
        AdditionBase               = flags >> 7 is not 0;
        SubtractionSelf            = flags >> 8 is not 0;
        SubtractionBase            = flags >> 9 is not 0;
        ImplicitConversionFromBase = flags >> 10 is not 0;
        ImplicitConversionToBase   = flags >> 11 is not 0;
        ConversionToBase           = flags >> 12 is not 0;
        NewtonsoftConverter        = flags >> 13 is not 0;
        SystemConverter            = flags >> 14 is not 0;
        HasZero                    = flags >> 15 is not 0;
        HasOne                     = flags >> 16 is not 0;

        if (ComparableSelf)
            EquatableSelf = true;
        if (ComparableBase)
            EquatableBase = true;
        if (ImplicitConversionToBase)
            ConversionToBase = true;

        Accessibility    = accessibility;
        FieldName        = fieldName;
        OverrideToString = overrideToString;
    }
}

[Generator]
public sealed class StrongTypeGenerator : IIncrementalGenerator
{
    private static readonly string StrongTypeAttribute = IndentedStringBuilder.CreatePreamble().AppendLine("#pragma warning disable CS9113")
        .AppendLine()
        .OpenNamespace("Luna.Generators")
        .AppendLine("/// <summary> Flags that control which functionality this strong type should implement. </summary>")
        .AppendLine("[Flags]")
        .GeneratedAttribute()
        .AppendLine("internal enum StrongTypeFlag : ulong")
        .OpenBlock()
        .AppendLine("/// <summary> Whether the strong type is equatable to itself, including equality operators. </summary>")
        .AppendLine("EquatableSelf              = 1 << 0,")
        .AppendLine("/// <summary> Whether the strong type is equatable to its base type, including equality operators. </summary>")
        .AppendLine("EquatableBase              = 1 << 1,")
        .AppendLine(
            "/// <summary> Whether the strong type is comparable to itself, including comparison operators, also implies <see cref=\"EquatableSelf\"/>. </summary>")
        .AppendLine("ComparableSelf             = 1 << 2,")
        .AppendLine(
            "/// <summary> Whether the strong type is comparable to its base type, including comparison operators, also implies <see cref=\"EquatableBase\"/>. </summary>")
        .AppendLine("ComparableBase             = 1 << 3,")
        .AppendLine("/// <summary> Whether the strong type supports increment operators (++). </summary>")
        .AppendLine("Incrementable              = 1 << 4,")
        .AppendLine("/// <summary> Whether the strong type supports decrement operators (--). </summary>")
        .AppendLine("Decrementable              = 1 << 5,")
        .AppendLine("/// <summary> Whether the strong type supports addition with itself. </summary>")
        .AppendLine("AdditionSelf               = 1 << 6,")
        .AppendLine("/// <summary> Whether the strong type supports addition with its base type (in both directions). </summary>")
        .AppendLine("AdditionBase               = 1 << 7,")
        .AppendLine("/// <summary> Whether the strong type supports subtraction with itself (with itself as a return type). </summary>")
        .AppendLine("SubtractionSelf            = 1 << 8,")
        .AppendLine(
            "/// <summary> Whether the strong type supports subtraction with its base type (with itself as a return type, only one direction). </summary>")
        .AppendLine("SubtractionBase            = 1 << 9,")
        .AppendLine(
            "/// <summary> Whether the strong type can be implicitly converted from its base type. Otherwise it will still support explicit conversion. </summary>")
        .AppendLine("ImplicitConversionFromBase = 1 << 10,")
        .AppendLine(
            "/// <summary> Whether the strong type can be explicitly converted back to its base type. Ignored if <see cref=\"ImplicitConversionToBase\"/> is set. </summary>")
        .AppendLine("ExplicitConversionToBase   = 1 << 11,")
        .AppendLine("/// <summary> Whether the strong type can be implicitly converted back to its base type. </summary>")
        .AppendLine("ImplicitConversionToBase   = 1 << 12,")
        .AppendLine("/// <summary> Whether the strong type contains and applies a Newtonsoft.Json Converter. </summary>")
        .AppendLine("NewtonsoftConverter        = 1 << 13,")
        .AppendLine("/// <summary> Whether the strong type contains and applies a System.Text.Json Converter. </summary>")
        .AppendLine("SystemConverter            = 1 << 14,")
        .AppendLine("/// <summary> Whether the strong type contains a Zero entry. </summary>")
        .AppendLine("HasZero                    = 1 << 15,")
        .AppendLine("/// <summary> Whether the strong type contains a One entry. </summary>")
        .AppendLine("HasOne                     = 1 << 16,")
        .AppendLine()
        .AppendLine("/// <summary> The default functionality for a basic type. </summary>")
        .AppendLine(
            "Default = EquatableSelf | EquatableBase | ComparableBase | ComparableSelf | Incrementable | Decrementable | AdditionBase | SubtractionBase | ImplicitConversionFromBase | ExplicitConversionToBase | HasZero,")
        .CloseBlock().AppendLine()
        .AppendLine()
        .AppendLine("/// <summary> Create a strongly typed ID type struct. </summary>")
        .AppendLine("[AttributeUsage(AttributeTargets.Struct)]")
        .GeneratedAttribute()
        .AppendLine(
            "internal class StrongTypeAttribute<T>(string FieldName = \"Value\", StrongTypeFlag Flags = StrongTypeFlag.Default) : Attribute where T : unmanaged, System.Numerics.INumber<T>;")
        .CloseAllBlocks().ToString();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        Utility.AddTagAttributes(ref context, StrongTypeAttribute, nameof(StrongTypeAttribute));
        Utility.Generate(ref context, nameof(StrongTypeAttribute) + "`1",
            static (ctx, _) => GetTypeToGenerate(ctx.SemanticModel, ctx.TargetNode),
            static (spc, source) => Execute(source, spc));
    }


    private static void Execute(StrongTypeData? typeToGenerate, SourceProductionContext context)
    {
        if (typeToGenerate is not { } value)
            return;

        var result = GenerateExtensionClass(value);
        context.AddSource($"StrongType.{value.Name.Name}.g.cs", SourceText.From(result, Encoding.UTF8));
    }

    private static StrongTypeData? GetTypeToGenerate(SemanticModel semanticModel, SyntaxNode enumDeclarationSyntax)
    {
        if (semanticModel.GetDeclaredSymbol(enumDeclarationSyntax) is not INamedTypeSymbol typeSymbol)
            return null;

        if (Utility.FindGenericAttributes(semanticModel.Compilation, typeSymbol, $"Luna.Generators.{nameof(StrongTypeAttribute)}`1")
                .FirstOrDefault() is { } attribute)
        {
            var arguments        = attribute.ConstructorArguments;
            var overrideToString = !typeSymbol.GetMembers().Any(m => m.Name is nameof(ToString));

            return new StrongTypeData(typeSymbol, (INamedTypeSymbol)attribute.AttributeClass!.TypeArguments.First()!,
                Utility.GetFullNamespace(typeSymbol), (ulong)arguments[1].Value!, typeSymbol.DeclaredAccessibility, (string)arguments[0].Value!,
                overrideToString);
        }

        return null;
    }

    private static string GenerateExtensionClass(in StrongTypeData strongType)
    {
        var sb = IndentedStringBuilder.CreatePreamble();
        sb.OpenNamespace(strongType.Namespace);
        if (strongType.NewtonsoftConverter)
            sb.AppendLine("[Newtonsoft.Json.JsonConverter(typeof(NewtonsoftJsonConverter))]");
        if (strongType.SystemConverter)
            sb.AppendLine("[System.Text.Json.Serialization.JsonConverter(typeof(SystemJsonConverter))]");
        sb.Append(strongType.Accessibility.ToModifier()).Append("readonly partial struct ").Append(strongType.Name.Name).Append('(')
            .AppendObject(strongType.BaseType.FullyQualified).Append(' ').Append(strongType.FieldName).Append(')').AppendLine();

        var interfaces = CollectInterfaces(strongType);
        if (interfaces.Count > 0)
            sb.Append("    : ").Append(interfaces[0]);
        foreach (var i in interfaces.Skip(1))
            sb.Append(',').AppendLine().Append("      ").Append(i);
        if (interfaces.Count > 0)
            sb.AppendLine();

        sb.OpenBlock();
        sb.Append("public readonly ").AppendObject(strongType.BaseType.FullyQualified).Append(' ').Append(strongType.FieldName).Append(" = ")
            .Append(strongType.FieldName).AppendLine(';');
        sb.AppendLine();
        if (strongType.OverrideToString)
            sb.AppendLine("/// <inheritdoc/>")
                .GeneratedAttribute()
                .AppendLine("public override string ToString()")
                .Append("    => ").Append(strongType.FieldName).AppendLine(".ToString();")
                .AppendLine();

        sb.AppendLine("/// <inheritdoc/>")
            .GeneratedAttribute()
            .Append("public static ").Append(strongType.Name.Name).AppendLine(" Parse(string s, IFormatProvider? provider)")
            .Append("    => ").AppendObject(strongType.BaseType.FullyQualified).AppendLine(".Parse(s, provider);")
            .AppendLine()
            .AppendLine("/// <inheritdoc/>")
            .GeneratedAttribute()
            .Append("public static bool TryParse(string? s, IFormatProvider? provider, out ").Append(strongType.Name.Name)
            .AppendLine(" result)")
            .OpenBlock()
            .Append("if (").AppendObject(strongType.BaseType.FullyQualified).AppendLine(".TryParse(s, provider, out var v))")
            .OpenBlock()
            .Append("result = new ").Append(strongType.Name.Name).AppendLine("(v);")
            .AppendLine("return true;")
            .CloseBlock().AppendLine()
            .AppendLine("result = default;")
            .AppendLine("return false;")
            .CloseBlock().AppendLine()
            .AppendLine()
            .AppendLine("/// <inheritdoc/>")
            .GeneratedAttribute()
            .Append("public static ").Append(strongType.Name.Name).AppendLine(" Parse(ReadOnlySpan<char> s, IFormatProvider? provider)")
            .Append("    => ").AppendObject(strongType.BaseType.FullyQualified).AppendLine(".Parse(s, provider);")
            .AppendLine()
            .AppendLine("/// <inheritdoc/>")
            .GeneratedAttribute()
            .Append("public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out ").Append(strongType.Name.Name)
            .AppendLine(" result)")
            .OpenBlock()
            .Append("if (").AppendObject(strongType.BaseType.FullyQualified).AppendLine(".TryParse(s, provider, out var v))")
            .OpenBlock()
            .Append("result = new ").Append(strongType.Name.Name).AppendLine("(v);")
            .AppendLine("return true;")
            .CloseBlock().AppendLine()
            .AppendLine("result = default;")
            .AppendLine("return false;")
            .CloseBlock().AppendLine()
            .AppendLine()
            .AppendLine("/// <inheritdoc/>")
            .GeneratedAttribute()
            .Append("public static ").Append(strongType.Name.Name).AppendLine(" Parse(ReadOnlySpan<byte> s, IFormatProvider? provider)")
            .Append("    => ").AppendObject(strongType.BaseType.FullyQualified).AppendLine(".Parse(s, provider);")
            .AppendLine()
            .AppendLine("/// <inheritdoc/>")
            .GeneratedAttribute()
            .Append("public static bool TryParse(ReadOnlySpan<byte> s, IFormatProvider? provider, out ").Append(strongType.Name.Name)
            .AppendLine(" result)")
            .OpenBlock()
            .Append("if (").AppendObject(strongType.BaseType.FullyQualified).AppendLine(".TryParse(s, provider, out var v))")
            .OpenBlock()
            .Append("result = new ").Append(strongType.Name.Name).AppendLine("(v);")
            .AppendLine("return true;")
            .CloseBlock().AppendLine()
            .AppendLine("result = default;")
            .AppendLine("return false;")
            .CloseBlock().AppendLine()
            .AppendLine();

        sb.AppendLine("/// <inheritdoc/>")
            .GeneratedAttribute()
            .AppendLine("public string ToString(string? format, IFormatProvider?  formatProvider)")
            .Append("    => ").Append(strongType.FieldName).AppendLine(".ToString(format, formatProvider);")
            .AppendLine()
            .AppendLine("/// <inheritdoc/>")
            .GeneratedAttribute()
            .AppendLine(
                "public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? formatProvider)")
            .Append("    => ").Append(strongType.FieldName).AppendLine(".TryFormat(destination, out charsWritten, format, formatProvider);")
            .AppendLine()
            .AppendLine("/// <inheritdoc/>")
            .GeneratedAttribute()
            .AppendLine(
                "public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? formatProvider)")
            .Append("    => ").Append(strongType.FieldName).AppendLine(".TryFormat(destination, out bytesWritten, format, formatProvider);")
            .AppendLine();

        if (strongType.EquatableSelf)
        {
            sb.AppendLine("/// <inheritdoc/>")
                .GeneratedAttribute()
                .Append("public bool Equals(").Append(strongType.Name.Name).AppendLine(" other)")
                .Append("    => ").Append(strongType.FieldName).Append(".Equals(other.").Append(strongType.FieldName).AppendLine(");")
                .AppendLine()
                .AppendLine("/// <inheritdoc/>")
                .GeneratedAttribute()
                .AppendLine("public override bool Equals(object? obj)")
                .Append("    => (obj is ").Append(strongType.Name.Name).Append(" other && other.").Append(strongType.FieldName)
                .Append(".Equals(").Append(strongType.FieldName);
            if (strongType.EquatableBase)
                sb.AppendLine("))").Append("    || (obj is ").AppendObject(strongType.BaseType.FullyQualified)
                    .Append(" baseType && baseType.Equals(").Append(strongType.FieldName).AppendLine("));");
            else
                sb.AppendLine("));");

            sb.AppendLine()
                .AppendLine("/// <inheritdoc/>")
                .GeneratedAttribute()
                .AppendLine("public override int GetHashCode()")
                .Append("    => ").Append(strongType.FieldName).AppendLine(".GetHashCode();")
                .AppendLine()
                .AppendComparisonOperator("==", strongType.Name.Name, strongType.Name.Name, strongType.FieldName, strongType.FieldName, 100)
                .AppendLine()
                .AppendComparisonOperator("!=", strongType.Name.Name, strongType.Name.Name, strongType.FieldName, strongType.FieldName, 100)
                .AppendLine();
        }


        if (strongType.EquatableBase)
            sb.AppendLine("/// <inheritdoc/>")
                .GeneratedAttribute()
                .Append("public bool Equals(").AppendObject(strongType.BaseType.FullyQualified).AppendLine(" other)")
                .Append("    => ").Append(strongType.FieldName).Append(".Equals(other").AppendLine(");")
                .AppendLine()
                .AppendComparisonOperator("==", strongType.Name.Name, strongType.BaseType.FullyQualified, strongType.FieldName, string.Empty,
                    50)
                .AppendLine()
                .AppendComparisonOperator("!=", strongType.Name.Name, strongType.BaseType.FullyQualified, strongType.FieldName, string.Empty,
                    50)
                .AppendLine()
                .AppendComparisonOperator("==", strongType.BaseType.FullyQualified, strongType.Name.Name, string.Empty, strongType.FieldName,
                    50)
                .AppendLine()
                .AppendComparisonOperator("!=", strongType.BaseType.FullyQualified, strongType.Name.Name, string.Empty, strongType.FieldName,
                    50)
                .AppendLine();

        if (strongType.ComparableSelf)
            sb.AppendLine("/// <inheritdoc/>")
                .GeneratedAttribute()
                .Append("public int CompareTo(").Append(strongType.Name.Name).AppendLine(" other)")
                .Append("    => ").Append(strongType.FieldName).Append(".CompareTo(other.").Append(strongType.FieldName).AppendLine(");")
                .AppendLine()
                .AppendComparisonOperator(">", strongType.Name.Name, strongType.Name.Name, strongType.FieldName, strongType.FieldName, 100)
                .AppendLine()
                .AppendComparisonOperator("<", strongType.Name.Name, strongType.Name.Name, strongType.FieldName, strongType.FieldName, 100)
                .AppendLine()
                .AppendComparisonOperator(">=", strongType.Name.Name, strongType.Name.Name, strongType.FieldName, strongType.FieldName, 100)
                .AppendLine()
                .AppendComparisonOperator("<=", strongType.Name.Name, strongType.Name.Name, strongType.FieldName, strongType.FieldName, 100)
                .AppendLine();

        if (strongType.ComparableBase)
            sb.AppendLine("/// <inheritdoc/>")
                .GeneratedAttribute()
                .Append("public int CompareTo(").AppendObject(strongType.BaseType.FullyQualified).AppendLine(" other)")
                .Append("    => ").Append(strongType.FieldName).AppendLine(".CompareTo(other);")
                .AppendLine()
                .AppendComparisonOperator(">", strongType.Name.Name, strongType.BaseType.FullyQualified, strongType.FieldName, string.Empty, 50)
                .AppendLine()
                .AppendComparisonOperator("<", strongType.Name.Name, strongType.BaseType.FullyQualified, strongType.FieldName, string.Empty, 50)
                .AppendLine()
                .AppendComparisonOperator(">=", strongType.Name.Name, strongType.BaseType.FullyQualified, strongType.FieldName, string.Empty,
                    50)
                .AppendLine()
                .AppendComparisonOperator("<=", strongType.Name.Name, strongType.BaseType.FullyQualified, strongType.FieldName, string.Empty,
                    50)
                .AppendLine()
                .AppendComparisonOperator(">", strongType.BaseType.FullyQualified, strongType.Name.Name, string.Empty, strongType.FieldName, 50)
                .AppendLine()
                .AppendComparisonOperator("<", strongType.BaseType.FullyQualified, strongType.Name.Name, string.Empty, strongType.FieldName, 50)
                .AppendLine()
                .AppendComparisonOperator(">=", strongType.BaseType.FullyQualified, strongType.Name.Name, string.Empty, strongType.FieldName,
                    50)
                .AppendLine()
                .AppendComparisonOperator("<=", strongType.BaseType.FullyQualified, strongType.Name.Name, string.Empty, strongType.FieldName,
                    50)
                .AppendLine();

        sb.GeneratedAttribute()
            .Append("public static ").Append(strongType.ImplicitConversionFromBase ? "implicit" : "explicit").Append(" operator ")
            .Append(strongType.Name.Name).Append('(')
            .AppendObject(strongType.BaseType.FullyQualified).AppendLine(" value)")
            .AppendLine("    => new(value);")
            .AppendLine();

        if (strongType.ConversionToBase)
            sb.GeneratedAttribute()
                .Append("public static ").Append(strongType.ImplicitConversionToBase ? "implicit" : "explicit").Append(" operator ")
                .AppendObject(strongType.BaseType.FullyQualified).Append('(')
                .Append(strongType.Name.Name).AppendLine(" value)")
                .Append("    => value.").Append(strongType.FieldName).AppendLine(';')
                .AppendLine();

        if (strongType.Incrementable)
            sb.GeneratedAttribute()
                .Append("public static ").Append(strongType.Name.Name).Append(" operator ++(").Append(strongType.Name.Name)
                .AppendLine(" value)")
                .OpenBlock()
                .Append("var v = value.").Append(strongType.FieldName).AppendLine(';')
                .AppendLine("return new(++v);")
                .CloseBlock().AppendLine()
                .AppendLine();

        if (strongType.Decrementable)
            sb.GeneratedAttribute()
                .Append("public static ").Append(strongType.Name.Name).Append(" operator --(").Append(strongType.Name.Name)
                .AppendLine(" value)")
                .OpenBlock()
                .Append("var v = value.").Append(strongType.FieldName).AppendLine(';')
                .AppendLine("return new(--v);")
                .CloseBlock().AppendLine()
                .AppendLine();

        if (strongType.AdditionSelf)
            sb.AppendArithmeticOperator("+", strongType.Name.Name, strongType.Name.Name, strongType.Name.Name, strongType.FieldName,
                    strongType.FieldName)
                .AppendLine()
                .AppendLine("/// <inheritdoc/>")
                .GeneratedAttribute()
                .Append("public static ").Append(strongType.Name.Name).AppendLine(" AdditiveIdentity")
                .AppendLine("    => default;")
                .AppendLine();

        if (strongType.AdditionBase)
            sb.AppendArithmeticOperator("+", strongType.Name.Name, strongType.Name.Name, strongType.BaseType.FullyQualified,
                    strongType.FieldName, string.Empty)
                .AppendLine()
                .AppendArithmeticOperator("+", strongType.Name.Name, strongType.BaseType.FullyQualified, strongType.Name.Name, string.Empty,
                    strongType.FieldName)
                .AppendLine()
                .AppendLine("/// <inheritdoc/>")
                .GeneratedAttribute()
                .Append("static ").AppendObject(strongType.BaseType.FullyQualified).Append(" global::System.Numerics.IAdditiveIdentity<")
                .Append(strongType.Name.Name).Append(", ").AppendObject(strongType.BaseType.FullyQualified).AppendLine(">.AdditiveIdentity")
                .AppendLine("    => default;")
                .AppendLine();

        if (strongType.HasZero)
            sb.Append("public static readonly ").Append(strongType.Name.Name).AppendLine(" Zero = new(0);")
                .AppendLine();

        if (strongType.HasOne)
            sb.Append("public static readonly ").Append(strongType.Name.Name).AppendLine(" One = new(1);")
                .AppendLine();

        if (strongType.SubtractionSelf)
            sb.AppendArithmeticOperator("-", strongType.Name.Name, strongType.Name.Name, strongType.Name.Name, strongType.FieldName,
                    strongType.FieldName)
                .AppendLine();

        if (strongType.SubtractionBase)
            sb.AppendArithmeticOperator("-", strongType.Name.Name, strongType.Name.Name, strongType.BaseType.FullyQualified,
                    strongType.FieldName, string.Empty)
                .AppendLine()
                .AppendArithmeticOperator("-", strongType.Name.Name, strongType.BaseType.FullyQualified, strongType.Name.Name, string.Empty,
                    strongType.FieldName)
                .AppendLine();

        if (strongType.NewtonsoftConverter)
            sb.GeneratedAttribute()
                .Append("private sealed class NewtonsoftJsonConverter : global::Newtonsoft.Json.JsonConverter<").Append(strongType.Name.Name)
                .AppendLine('>')
                .OpenBlock()
                .Append("public override void WriteJson(global::Newtonsoft.Json.JsonWriter writer, ").Append(strongType.Name.Name)
                .AppendLine(" value, global::Newtonsoft.Json.JsonSerializer serializer)")
                .Append("    => writer.WriteValue(value.").Append(strongType.FieldName).AppendLine(");")
                .AppendLine()
                .Append("public override ").Append(strongType.Name.Name)
                .Append(" ReadJson(global::Newtonsoft.Json.JsonReader reader, Type objectType, ").Append(strongType.Name.Name)
                .AppendLine(" existingValue, bool hasExistingValue, global::Newtonsoft.Json.JsonSerializer serializer)")
                .Append("    => new(serializer.Deserialize<").AppendObject(strongType.BaseType.FullyQualified).AppendLine(">(reader));")
                .CloseBlock().AppendLine()
                .AppendLine();


        if (strongType.SystemConverter)
            sb.GeneratedAttribute()
                .Append("private sealed class SystemJsonConverter : global::System.Text.Json.Serialization.JsonConverter<")
                .Append(strongType.Name.Name).AppendLine('>')
                .OpenBlock()
                .Append("public override ").Append(strongType.Name.Name).AppendLine(
                    " Read(ref global::System.Text.Json.Utf8JsonReader reader, Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)")
                .Append("    => new(").AppendObject(strongType.BaseType.FullyQualified).AppendLine(".Parse(reader.ValueSpan, null));")
                .AppendLine()
                .Append("public override void Write(global::System.Text.Json.Utf8JsonWriter writer, ").Append(strongType.Name.Name)
                .AppendLine(" value, global::System.Text.Json.JsonSerializerOptions options)")
                .Append("    => writer.WriteNumberValue(value.").Append(strongType.FieldName).AppendLine(");")
                .AppendLine();

        sb.CloseAllBlocks();
        return sb.ToString();
    }

    private static List<string> CollectInterfaces(in StrongTypeData strongType)
    {
        var list = new List<string>(32)
        {
            $"IParsable<{strongType.Name.Name}>",
            $"ISpanParsable<{strongType.Name.Name}>",
            $"IUtf8SpanParsable<{strongType.Name.Name}>",
            "IFormattable",
            "ISpanFormattable",
            "IUtf8SpanFormattable",
        };

        if (strongType.EquatableSelf)
        {
            list.Add($"global::System.IEquatable<{strongType.Name.Name}>");
            list.Add($"global::System.Numerics.IEqualityOperators<{strongType.Name.Name}, {strongType.Name.Name}, bool>");
        }

        if (strongType.EquatableBase)
        {
            list.Add($"global::System.IEquatable<{strongType.BaseType.Name}>");
            list.Add($"global::System.Numerics.IEqualityOperators<{strongType.Name.Name}, {strongType.BaseType.Name}, bool>");
        }

        if (strongType.ComparableBase)
        {
            list.Add($"global::System.IComparable<{strongType.Name.Name}>");
            list.Add($"global::System.Numerics.IComparisonOperators<{strongType.Name.Name}, {strongType.Name.Name}, bool>");
        }

        if (strongType.ComparableSelf)
        {
            list.Add($"global::System.IComparable<{strongType.BaseType.Name}>");
            list.Add($"global::System.Numerics.IComparisonOperators<{strongType.Name.Name}, {strongType.BaseType.Name}, bool>");
        }

        if (strongType.Incrementable)
            list.Add($"global::System.Numerics.IIncrementOperators<{strongType.Name.Name}>");

        if (strongType.Decrementable)
            list.Add($"global::System.Numerics.IDecrementOperators<{strongType.Name.Name}>");

        if (strongType.AdditionSelf)
        {
            list.Add($"global::System.Numerics.IAdditionOperators<{strongType.Name.Name}, {strongType.Name.Name}, {strongType.Name.Name}>");
            list.Add($"global::System.Numerics.IAdditiveIdentity<{strongType.Name.Name}, {strongType.Name.Name}>");
        }

        if (strongType.AdditionBase)
        {
            list.Add($"global::System.Numerics.IAdditionOperators<{strongType.Name.Name}, {strongType.BaseType.Name}, {strongType.Name.Name}>");
            list.Add($"global::System.Numerics.IAdditiveIdentity<{strongType.Name.Name}, {strongType.BaseType.Name}>");
        }

        if (strongType.SubtractionSelf)
            list.Add($"global::System.Numerics.ISubtractionOperators<{strongType.Name.Name}, {strongType.Name.Name}, {strongType.Name.Name}>");

        if (strongType.SubtractionBase)
            list.Add(
                $"global::System.Numerics.ISubtractionOperators<{strongType.Name.Name}, {strongType.BaseType.Name}, {strongType.Name.Name}>");

        return list;
    }
}
