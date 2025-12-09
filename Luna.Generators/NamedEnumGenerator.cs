using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Luna.Generators;

internal readonly record struct NamedEnumData
{
    public readonly TypeDefinition                               Name;
    public readonly string                                       MethodName;
    public readonly string                                       Unknown;
    public readonly bool                                         Utf16;
    public readonly bool                                         Utf8;
    public readonly string                                       Namespace;
    public readonly string                                       Class;
    public readonly ValueCollection<(string Value, string Name)> Values;

    public NamedEnumData(string name, string methodName, string unknownName, bool utf16, bool utf8, string @namespace, string @class,
        params IReadOnlyCollection<(string Value, string Name)> values)
    {
        Name       = new TypeDefinition(name);
        MethodName = methodName;
        Unknown    = unknownName;
        Utf16      = utf16;
        Utf8       = utf8;
        Namespace  = @namespace;
        Class      = @class;
        Values     = new ValueCollection<(string Value, string Name)>(values);
    }
}

[Generator]
public sealed class NamedEnumGenerator : IIncrementalGenerator
{
    private static readonly string NamedEnumAttribute = IndentedStringBuilder.CreatePreamble().AppendLine("#pragma warning disable CS9113")
        .AppendLine()
        .OpenNamespace("Luna.Generators")
        .AppendLine(
            "/// <summary> Add a method returning names in UTF8 and UTF16 to the enum. Use with <see cref=\"Luna.Generators.NameAttribute\"/>. </summary>")
        .AppendLine(
            "/// <param name=\"Method\"> The name of the UTF16 method provided. The UTF8 version has 'U8' appended to this name, if also provided. </param>")
        .AppendLine("/// <param name=\"Utf8\"> Whether to provide a UTF8 version of the method. </param>")
        .AppendLine("/// <param name=\"Utf16\"> Whether to provide a UTF16 version of the method. </param>")
        .AppendLine("/// <param name=\"Unknown\"> The name to provide for omitted or undefined values of the enum. </param>")
        .AppendLine(
            "/// <param name=\"Namespace\"> The namespace to put the extension class into. If this is null, the namespace of the enum will be used. </param>")
        .AppendLine(
            "/// <param name=\"Class\"> The name of the static class containing the extension methods. If this is null, <c>[EnumName]Extensions</c> will be used. </param>")
        .AppendLine("[AttributeUsage(AttributeTargets.Enum)]")
        .GeneratedAttribute()
        .AppendLine(
            "internal class NamedEnumAttribute(string Method = \"ToName\", bool Utf8 = true, bool Utf16 = true, string Unknown = \"Unknown\", string? Namespace = null, string? Class = null) : Attribute;")
        .AppendLine()
        .AppendLine(
            "/// <summary> The name to provide when <see cref=\"Luna.Generators.NamedEnumAttribute\"/> is used for this enum. </summary>")
        .AppendLine("/// <param name=\"Name\"> The name to provide. If this is null, the name of the value itself is used. </param>")
        .AppendLine("/// <param name=\"Omit\"> Whether to omit this value from the enum and treat it as undefined. </param>")
        .AppendLine("/// <remarks> This is only intended for enum values. If this is omitted, the name of the value itself is used. </remarks>")
        .AppendLine("[AttributeUsage(AttributeTargets.Field)]")
        .GeneratedAttribute()
        .AppendLine("internal class NameAttribute(string? Name = null, bool Omit = false) : Attribute;")
        .CloseAllBlocks().ToString();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        Utility.AddTagAttributes(ref context, NamedEnumAttribute, nameof(NamedEnumAttribute));
        Utility.Generate(ref context, nameof(NamedEnumAttribute), static (ctx, _) => GetEnumToGenerate(ctx.SemanticModel, ctx.TargetNode),
            static (spc, source) => Execute(source, spc));
    }

    private static void Execute(NamedEnumData? enumToGenerate, SourceProductionContext context)
    {
        if (enumToGenerate is not { } value || value is { Utf16: false, Utf8: false })
            return;

        var result = GenerateExtensionClass(value);
        context.AddSource($"NamedEnum.{value.Name.Name}.g.cs", SourceText.From(result, Encoding.UTF8));
    }


    private static NamedEnumData? GetEnumToGenerate(SemanticModel semanticModel, SyntaxNode enumDeclarationSyntax)
    {
        if (semanticModel.GetDeclaredSymbol(enumDeclarationSyntax) is not INamedTypeSymbol enumSymbol)
            return null;

        var enumName    = enumSymbol.ToString();
        var enumMembers = enumSymbol.GetMembers();
        var members     = new List<(string, string)>(enumMembers.Length);
        var methodName  = "ToName";
        var unknownName = "Unknown";
        var @namespace  = Utility.GetFullNamespace(enumSymbol);
        var @class      = $"{enumName}Extensions";
        var utf8        = true;
        var utf16       = true;

        if (Utility.FindAttribute(semanticModel.Compilation, enumSymbol, $"Luna.Generators.{nameof(NamedEnumAttribute)}") is { } attribute)
        {
            var arguments = attribute.ConstructorArguments;
            if (arguments[0].Value is string m)
                methodName = m;
            if (arguments[1].Value is bool b1)
                utf8 = b1;
            if (arguments[2].Value is bool b2)
                utf16 = b2;
            if (arguments[3].Value is string u)
                unknownName = u;
            if (arguments[4].Value is string n)
                @namespace = n;
            if (arguments[5].Value is string c)
                @class = c;
        }

        if (!utf8 && !utf16)
            return null;

        var namedAttributeSymbol = semanticModel.Compilation.GetTypeByMetadataName("Luna.Generators.NameAttribute")!;

        foreach (var member in enumMembers)
        {
            if (member is not IFieldSymbol symbol)
                continue;

            var add  = true;
            var name = member.Name;
            if (Utility.FindAttribute(symbol, namedAttributeSymbol) is { } fieldAttribute)
            {
                var arguments = fieldAttribute.ConstructorArguments;
                if (arguments[1].Value is true)
                    add = false;
                if (arguments[0].Value is string n)
                    name = n;
            }

            if (add)
                members.Add((member.Name, name));
        }

        return new NamedEnumData(enumName, methodName, unknownName, utf16, utf8, @namespace, @class, members);
    }

    private static string GenerateExtensionClass(in NamedEnumData namedEnum)
    {
        var sb = IndentedStringBuilder.CreatePreamble();
        sb.OpenNamespace(namedEnum.Namespace)
            .OpenExtensionClass(namedEnum.Class);
        if (namedEnum.Utf16)
        {
            sb.AppendLine("/// <summary> Efficiently get a human-readable display name for this value. </summary>");
            if (namedEnum.Utf8)
                sb.Append("/// <remarks> For a UTF8 representation of the name, use <see cref=\"")
                    .Append($"{namedEnum.Class}.{namedEnum.MethodName}").AppendLine("U8\"/>. </remarks>");
            sb.GeneratedAttribute()
                .Append("public static string ").Append(namedEnum.MethodName).Append("(this ").AppendObject(namedEnum.Name.FullyQualified)
                .Indent()
                .AppendLine(" value)")
                .AppendLine("=> value switch")
                .OpenBlock();
            foreach (var (value, name) in namedEnum.Values)
            {
                sb.AppendObject(namedEnum.Name.FullyQualified).Append('.').Append(value).Append(" => \"").Append(name)
                    .AppendLine("\",");
            }

            sb.Append("_ => \"").Append(namedEnum.Unknown).Append("\",").AppendLine()
                .CloseBlock().Append(';').AppendLine().Unindent();
        }

        if (namedEnum.Utf8)
        {
            if (namedEnum.Utf16)
                sb.AppendLine();
            sb.AppendLine("/// <summary> Efficiently get a human-readable display name for this value. </summary>");
            if (namedEnum.Utf16)
                sb.Append("/// <remarks> For a UTF16 representation of the name, use <see cref=\"")
                    .Append($"{namedEnum.Class}.{namedEnum.MethodName}").AppendLine("\"/>. </remarks>");
            sb.GeneratedAttribute()
                .Append("public static ReadOnlySpan<byte> ").Append(namedEnum.MethodName).Append("U8(this ")
                .AppendObject(namedEnum.Name.FullyQualified)
                .Indent().AppendLine(" value)")
                .AppendLine("=> value switch")
                .OpenBlock();
            foreach (var (value, name) in namedEnum.Values)
            {
                sb.AppendObject(namedEnum.Name.FullyQualified).Append('.').Append(value).Append(" => \"").Append(name)
                    .AppendLine("\"u8,");
            }

            sb.Append("_ => \"").Append(namedEnum.Unknown).Append("\"u8,").AppendLine()
                .CloseBlock().Append(';').AppendLine().Unindent();
        }

        sb.CloseAllBlocks();
        return sb.ToString();
    }
}
