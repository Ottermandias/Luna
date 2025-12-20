using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Luna.Generators;

internal readonly record struct TooltipEnumData
{
    public readonly TypeDefinition                               Name;
    public readonly string                                       MethodName;
    public readonly string                                       Unknown;
    public readonly bool                                         Utf16;
    public readonly string                                       Namespace;
    public readonly string                                       Class;
    public readonly ValueCollection<(string Value, string Name)> Values;

    public TooltipEnumData(string name, string methodName, string unknownName, bool utf16, string @namespace, string @class,
        params IReadOnlyCollection<(string Value, string Name)> values)
    {
        Name       = new TypeDefinition(name);
        MethodName = methodName;
        Unknown    = unknownName;
        Utf16      = utf16;
        Namespace  = @namespace;
        Class      = @class;
        Values     = new ValueCollection<(string Value, string Name)>(values);
    }
}

[Generator]
public sealed class TooltipEnumGenerator : IIncrementalGenerator
{
    private static readonly string TooltipEnumAttribute = IndentedStringBuilder.CreatePreamble().AppendLine("#pragma warning disable CS9113")
        .AppendLine()
        .OpenNamespace("Luna.Generators")
        .AppendLine(
            "/// <summary> Add a method returning tooltips in UTF8 or UTF16 to the enum. Use with <see cref=\"Luna.Generators.TooltipAttribute\"/>. </summary>")
        .AppendLine("/// <param name=\"Method\"> The name of the method provided. </param>")
        .AppendLine("/// <param name=\"Utf16\"> Whether to provide a UTF16 version of the method instead of the default UTF8 version. </param>")
        .AppendLine("/// <param name=\"Unknown\"> The text to provide for omitted or undefined values of the enum. </param>")
        .AppendLine(
            "/// <param name=\"Namespace\"> The namespace to put the extension class into. If this is null, the namespace of the enum will be used. </param>")
        .AppendLine(
            "/// <param name=\"Class\"> The name of the static class containing the extension methods. If this is null, <c>[EnumName]Extensions</c> will be used. </param>")
        .EmbeddedAttribute()
        .GeneratedAttribute()
        .AppendLine("[AttributeUsage(AttributeTargets.Enum)]")
        .AppendLine(
            "internal class TooltipEnumAttribute(string Method = \"Tooltip\", bool Utf16 = false, string Unknown = \"\", string? Namespace = null, string? Class = null) : Attribute;")
        .AppendLine()
        .AppendLine(
            "/// <summary> The tooltip to provide when <see cref=\"Luna.Generators.TooltipEnumAttribute\"/> is used for this enum. </summary>")
        .AppendLine("/// <param name=\"Tooltip\"> The tooltip to provide. If this is null, an empty string is used. </param>")
        .AppendLine("/// <param name=\"Omit\"> Whether to omit this value from the enum and treat it as undefined. </param>")
        .AppendLine("/// <remarks> This is only intended for enum values. If this is omitted, the name of the value itself is used. </remarks>")
        .EmbeddedAttribute()
        .GeneratedAttribute()
        .AppendLine("[AttributeUsage(AttributeTargets.Field)]")
        .AppendLine("internal class TooltipAttribute(string? Tooltip = null, bool Omit = false) : Attribute;")
        .CloseAllBlocks().ToString();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        Utility.AddTagAttributes(ref context, TooltipEnumAttribute, nameof(TooltipEnumAttribute));
        Utility.Generate(ref context, nameof(TooltipEnumAttribute), static (ctx, _) => GetEnumToGenerate(ctx.SemanticModel, ctx.TargetNode),
            static (spc, source) => Execute(source, spc));
    }

    private static void Execute(TooltipEnumData? enumToGenerate, SourceProductionContext context)
    {
        if (enumToGenerate is not { } value)
            return;

        var result = GenerateExtensionClass(value);
        context.AddSource($"TooltipEnum.{value.Name.Name}.g.cs", SourceText.From(result, Encoding.UTF8));
    }


    private static TooltipEnumData? GetEnumToGenerate(SemanticModel semanticModel, SyntaxNode enumDeclarationSyntax)
    {
        if (semanticModel.GetDeclaredSymbol(enumDeclarationSyntax) is not INamedTypeSymbol enumSymbol)
            return null;

        var enumName     = enumSymbol.ToString();
        var enumMembers  = enumSymbol.GetMembers();
        var members      = new List<(string, string)>(enumMembers.Length);
        var methodName   = "Tooltip";
        var unknownValue = "";
        var @namespace   = Utility.GetFullNamespace(enumSymbol);
        var @class       = $"{enumName}Extensions";
        var utf16        = false;

        if (Utility.FindAttribute(semanticModel.Compilation, enumSymbol, $"Luna.Generators.{nameof(TooltipEnumAttribute)}") is { } attribute)
        {
            var arguments = attribute.ConstructorArguments;
            if (arguments[0].Value is string m)
                methodName = m;
            if (arguments[1].Value is bool b1)
                utf16 = b1;
            if (arguments[2].Value is string u)
                unknownValue = u;
            if (arguments[3].Value is string n)
                @namespace = n;
            if (arguments[4].Value is string c)
                @class = c;
        }

        var tooltipAttributeSymbol = semanticModel.Compilation.GetTypeByMetadataName("Luna.Generators.TooltipAttribute")!;

        foreach (var member in enumMembers)
        {
            if (member is not IFieldSymbol symbol)
                continue;

            var add     = true;
            var tooltip = $"{member.Name}";
            if (Utility.FindAttribute(symbol, tooltipAttributeSymbol) is { } fieldAttribute)
            {
                var arguments = fieldAttribute.ConstructorArguments;
                if (arguments[1].Value is true)
                    add = false;
                if (arguments[0].Value is string t)
                    tooltip = SymbolDisplay.FormatLiteral(t, false);
            }

            if (add)
                members.Add((member.Name, tooltip));
        }

        return new TooltipEnumData(enumName, methodName, unknownValue, utf16, @namespace, @class, members);
    }

    private static string GenerateExtensionClass(in TooltipEnumData tooltipEnum)
    {
        var sb           = IndentedStringBuilder.CreatePreamble();
        sb.OpenNamespace(tooltipEnum.Namespace)
            .OpenExtensionClass(tooltipEnum.Class);

        if (!tooltipEnum.Utf16)
        {
            foreach (var (value, tooltip) in tooltipEnum.Values)
                sb.Append("private static readonly global::ImSharp.StringU8 ").Append(value).Append("_Tooltip__GenU8 = new(\"").Append(tooltip)
                    .AppendLine("\"u8);");

            sb.Append("private static readonly global::ImSharp.StringU8 MissingEntry_Tooltip__GenU8_ = new(\"").Append(tooltipEnum.Unknown)
                .AppendLine("\"u8);")
                .AppendLine();
        }

        sb.Append("/// <summary> Efficiently get an ").Append(tooltipEnum.Utf16 ? "UTF16" : "UTF8")
            .AppendLine(" tooltip for this value. </summary>");
        sb.GeneratedAttribute()
            .Append("public static ").Append(tooltipEnum.Utf16 ? "string " : "global::ImSharp.StringU8 ").Append(tooltipEnum.MethodName)
            .Append("(this ")
            .AppendObject(tooltipEnum.Name.FullyQualified)
            .Indent()
            .AppendLine(" value)")
            .AppendLine("=> value switch")
            .OpenBlock();
        if (tooltipEnum.Utf16)
        {
            foreach (var (value, tooltip) in tooltipEnum.Values)
            {
                sb.AppendObject(tooltipEnum.Name.FullyQualified).Append('.').Append(value).Append(" => \"").Append(tooltip)
                    .AppendLine("\",");
            }

            sb.Append("_ => \"").Append(tooltipEnum.Unknown).AppendLine("\",");
        }
        else
        {
            foreach (var (value, _) in tooltipEnum.Values)
            {
                sb.AppendObject(tooltipEnum.Name.FullyQualified).Append('.').Append(value).Append(" => ").Append(value)
                    .AppendLine("_Tooltip__GenU8,");
            }
            sb.AppendLine("_ => MissingEntry_Tooltip__GenU8_,");
        }

        sb.CloseBlock().Append(';').AppendLine().Unindent()
            .CloseAllBlocks();
        return sb.ToString();
    }
}
