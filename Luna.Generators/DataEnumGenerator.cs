using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Luna.Generators;

internal readonly record struct DataEnumData
{
    public readonly TypeDefinition                               Name;
    public readonly TypeDefinition                               DataType;
    public readonly string                                       Method;
    public readonly string                                       DefaultValue;
    public readonly bool                                         Nullable;
    public readonly string                                       Namespace;
    public readonly string                                       Class;
    public readonly ValueCollection<(string Value, string Name)> Values;

    public DataEnumData(string name, string method, INamedTypeSymbol dataType, string defaultValue, bool nullable, string @namespace,
        string @class,
        params IReadOnlyCollection<(string Value, string Name)> values)
    {
        Name         = new TypeDefinition(name);
        Method       = method;
        DataType     = new TypeDefinition(dataType);
        DefaultValue = defaultValue;
        Nullable     = nullable;
        Namespace    = @namespace;
        Class        = @class;
        Values       = new ValueCollection<(string Value, string Name)>(values);
    }
}

[Generator]
public sealed class DataEnumGenerator : IIncrementalGenerator
{
    private static readonly string DataEnumAttribute = IndentedStringBuilder.CreatePreamble().AppendLine("#pragma warning disable CS9113")
        .AppendLine()
        .OpenNamespace("Luna.Generators")
        .AppendLine(
            "/// <summary> Add a method returning an associated data point for this enum. Use with <see cref=\"Luna.Generators.DataAttribute\"/>. </summary>")
        .AppendLine("/// <param name=\"DataType\"> The type of the associated data points. </param>")
        .AppendLine(
            "/// <param name=\"Method\"> The name of the method going from this enum to the data points. This also has to be added to the <see cref=\"Luna.Generators.DataAttribute\"/> attributes. </param>")
        .AppendLine(
            "/// <param name=\"DefaultValue\"> The text for the default value used for unknown or omitted values in the method. If this is empty, <c>default</c> is used. </param>")
        .AppendLine(
            "/// <param name=\"Nullable\"> Whether the returned value can be null and is marked as nullable or not. </param>")
        .AppendLine(
            "/// <param name=\"Namespace\"> The namespace to put the extension class into. If this is null, the namespace of the enum will be used. </param>")
        .AppendLine(
            "/// <param name=\"Class\"> The name of the static class containing the extension methods. If this is null, <c>[EnumName]Extensions</c> will be used. </param>")
        .AppendLine("[AttributeUsage(AttributeTargets.Enum, AllowMultiple = true)]")
        .GeneratedAttribute()
        .AppendLine(
            "internal class DataEnumAttribute(Type Data, string Method, string DefaultValue = \"\", bool Nullable = true, string? Namespace = null, string? Class = null) : Attribute;")
        .AppendLine()
        .AppendLine(
            "/// <summary> The data to provide when <see cref=\"Luna.Generators.DataEnumAttribute\"/> is used for this enum. </summary>")
        .AppendLine("/// <param name=\"Method\"> The method to attach to. </param>")
        .AppendLine("/// <param name=\"Data\"> The data to provide. </param>")
        .AppendLine(
            "/// <param name=\"Omit\"> Whether to omit this value from the enum and treat it as undefined. Same behavior as not providing an attribute. </param>")
        .AppendLine(
            "/// <remarks> This is intended to provide code replacements as data points, so the text entered is not escaped and will be compiled as is. </remarks>")
        .AppendLine("[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]")
        .GeneratedAttribute()
        .AppendLine("internal class DataAttribute(string Method, string Data, bool Omit = false) : Attribute;")
        .CloseAllBlocks().ToString();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        Utility.AddTagEnums(ref context, DataEnumAttribute, nameof(DataEnumAttribute));
        Utility.Generate(ref context, nameof(DataEnumAttribute), static (ctx, _) => GetEnumToGenerate(ctx.SemanticModel, ctx.TargetNode),
            static (spc, source) => Execute(source, spc));
    }

    private static void Execute(DataEnumData? enumToGenerate, SourceProductionContext context)
    {
        if (enumToGenerate is not { } value || value is { Method.Length: 0, DataType.Name.Length: 0 })
            return;

        var result = GenerateExtensionClass(value);
        context.AddSource($"DataEnum.{value.Name.Name}_{value.Method}.g.cs", SourceText.From(result, Encoding.UTF8));
    }

    private static DataEnumData? GetEnumToGenerate(SemanticModel semanticModel, SyntaxNode enumDeclarationSyntax)
    {
        if (semanticModel.GetDeclaredSymbol(enumDeclarationSyntax) is not INamedTypeSymbol enumSymbol)
            return null;

        var               enumName    = enumSymbol.ToString();
        var               enumMembers = enumSymbol.GetMembers();
        var               members     = new List<(string, string)>(enumMembers.Length);
        string?           method      = null;
        INamedTypeSymbol? type        = null;
        var               @default    = string.Empty;
        var               nullable    = true;
        var               @namespace  = enumSymbol.ContainingNamespace.Name;
        var               @class      = $"{enumName}Extensions";

        if (Utility.FindAttribute(semanticModel.Compilation, enumSymbol, $"Luna.Generators.{nameof(DataEnumAttribute)}") is { } attribute)
        {
            var arguments = attribute.ConstructorArguments;
            if (arguments[0].Value is INamedTypeSymbol t)
                type = t;
            if (arguments[1].Value is string m)
                method = m;
            if (arguments[2].Value is string d)
                @default = d;
            if (arguments[3].Value is bool nul)
                nullable = nul;
            if (arguments[4].Value is string n)
                @namespace = n;
            if (arguments[5].Value is string c)
                @class = c;
        }

        if (type is null || string.IsNullOrEmpty(method))
            return null;

        var dataAttributeSymbol = semanticModel.Compilation.GetTypeByMetadataName("Luna.Generators.DataAttribute")!;

        // Get all the fields from the enum, and add their name to the list
        foreach (var member in enumMembers)
        {
            if (member is not IFieldSymbol symbol)
                continue;

            var add  = false;
            var name = member.Name;
            foreach (var fieldAttribute in Utility.FindAttributes(symbol, dataAttributeSymbol))
            {
                var arguments = fieldAttribute.ConstructorArguments;
                if (arguments[0].Value is string m && m != method)
                    continue;

                if (arguments[2].Value is false)
                    add = true;
                if (arguments[1].Value is string n)
                    name = n;
                break;
            }

            if (add)
                members.Add((member.Name, name));
        }

        return new DataEnumData(enumName, method!, type, @default, nullable, @namespace, @class, members);
    }

    private static string GenerateExtensionClass(in DataEnumData dataEnum)
    {
        var sb = IndentedStringBuilder.CreatePreamble();
        sb.OpenNamespace(dataEnum.Namespace)
            .OpenExtensionClass(dataEnum.Class);
        sb.Append("/// <summary> Get the associated data point of type <see cref=\"").AppendObject(dataEnum.DataType.FullyQualified)
            .Append("\"/>. </summary>").AppendLine();
        sb.GeneratedAttribute()
            .Append("public static ").AppendObject(dataEnum.DataType.FullyQualified);
        if (dataEnum.Nullable)
            sb.Append('?');
        sb.Append(' ').Append(dataEnum.Method)
            .Append("(this ")
            .AppendObject(dataEnum.Name.FullyQualified).Indent().AppendLine(" value)")
            .AppendLine("=> value switch")
            .OpenBlock();
        foreach (var (value, name) in dataEnum.Values)
            sb.AppendObject(dataEnum.Name.FullyQualified).Append('.').Append(value).Append(" => ").Append(name).Append(',').AppendLine();

        if (dataEnum.DefaultValue.Length is 0)
            sb.AppendLine("_ => default,");
        else
            sb.Append("_ => ").Append(dataEnum.DefaultValue).Append(',').AppendLine();
        sb.CloseBlock().Append(';').AppendLine().Unindent();
        sb.CloseAllBlocks();
        return sb.ToString();
    }
}
