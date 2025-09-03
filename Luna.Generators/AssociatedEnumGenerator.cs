using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Luna.Generators;

internal readonly record struct AssociatedEnumData
{
    public readonly TypeDefinition                               Name;
    public readonly TypeDefinition                               EnumType;
    public readonly string                                       ForwardMethod;
    public readonly string                                       BackwardMethod;
    public readonly string                                       ForwardDefault;
    public readonly string                                       BackwardDefault;
    public readonly string                                       Namespace;
    public readonly string                                       Class;
    public readonly ValueCollection<(string Value, string Name)> Values;

    public AssociatedEnumData(string name, string forwardMethod, string backwardMethod, INamedTypeSymbol enumType, string forwardDefault,
        string backwardDefault, string @namespace, string @class, params IReadOnlyCollection<(string Value, string Name)> values)
    {
        Name            = new TypeDefinition(name);
        ForwardMethod   = forwardMethod;
        BackwardMethod  = backwardMethod;
        ForwardDefault  = forwardDefault;
        BackwardDefault = backwardDefault;
        EnumType        = new TypeDefinition(enumType);
        Namespace       = @namespace;
        Class           = @class;
        Values          = new ValueCollection<(string Value, string Name)>(values);
    }
}

[Generator]
public sealed class AssociatedEnumGenerator : IIncrementalGenerator
{
    private static readonly string AssociatedEnumAttribute = IndentedStringBuilder.CreatePreamble().AppendLine("#pragma warning disable CS9113")
        .AppendLine()
        .OpenNamespace("Luna.Generators")
        .AppendLine(
            "/// <summary> Add a method returning an associated enum value for this enum. Use with <see cref=\"Luna.Generators.AssociateAttribute\"/>. </summary>")
        .AppendLine("/// <param name=\"Other\"> The type of the associated enum. </param>")
        .AppendLine(
            "/// <param name=\"ForwardMethod\"> The name of the method going from this enum to the associated one. Method is omitted if empty. Name is constructed from other type if null. </param>")
        .AppendLine(
            "/// <param name=\"BackwardMethod\"> The name of the method going from the associated enum back to this one. Method is omitted if empty. Name is constructed from this type if null. </param>")
        .AppendLine(
            "/// <param name=\"ForwardDefaultValue\"> The name of the default value used for unknown or omitted values in the forward method. If this is null, <c>default</c> is used. </param>")
        .AppendLine(
            "/// <param name=\"BackwardDefaultValue\"> The name of the default value used for unknown or omitted values in the backward method. If this is null, <c>default</c> is used. </param>")
        .AppendLine(
            "/// <param name=\"Namespace\"> The namespace to put the extension class into. If this is null, the namespace of the enum will be used. </param>")
        .AppendLine(
            "/// <param name=\"Class\"> The name of the static class containing the extension methods. If this is null, <c>[EnumName]Extensions</c> will be used. </param>")
        .AppendLine("[AttributeUsage(AttributeTargets.Enum, AllowMultiple = true)]")
        .GeneratedAttribute()
        .AppendLine(
            "internal class AssociatedEnumAttribute(Type Other, string? ForwardMethod = null, string? BackwardMethod = \"\", string? ForwardDefaultValue = null, string? BackwardDefaultValue = null, string? Namespace = null, string? Class = null) : Attribute;")
        .AppendLine()
        .AppendLine(
            "/// <summary> The name to provide when <see cref=\"Luna.Generators.NamedEnumAttribute\"/> is used for this enum. </summary>")
        .AppendLine("/// <param name=\"Name\"> The name to provide. If this is null, the name of the value itself is used. </param>")
        .AppendLine("/// <param name=\"Omit\"> Whether to omit this value from the enum and treat it as undefined. </param>")
        .AppendLine("/// <remarks> This is only intended for enum values. If this is omitted, the name of the value itself is used. </remarks>")
        .AppendLine("[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]")
        .GeneratedAttribute()
        .AppendLine("internal class AssociateAttribute(string? Name = null, bool Omit = false) : Attribute;")
        .CloseAllBlocks().ToString();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        Utility.AddTagEnums(ref context, AssociatedEnumAttribute, nameof(AssociatedEnumAttribute));
        Utility.Generate(ref context, nameof(AssociatedEnumAttribute), static (ctx, _) => GetEnumToGenerate(ctx.SemanticModel, ctx.TargetNode),
            static (spc, source) => Execute(source, spc));
    }

    private static void Execute(AssociatedEnumData? enumToGenerate, SourceProductionContext context)
    {
        if (enumToGenerate is not { } value || value is { ForwardMethod.Length: 0, BackwardMethod.Length: 0 })
            return;

        var result = GenerateExtensionClass(value);
        context.AddSource($"AssociatedEnum.{value.Name.Name}_{value.EnumType.Name}.g.cs", SourceText.From(result, Encoding.UTF8));
    }

    private static AssociatedEnumData? GetEnumToGenerate(SemanticModel semanticModel, SyntaxNode enumDeclarationSyntax)
    {
        if (semanticModel.GetDeclaredSymbol(enumDeclarationSyntax) is not INamedTypeSymbol enumSymbol)
            return null;

        var               enumName        = enumSymbol.ToString();
        var               enumMembers     = enumSymbol.GetMembers();
        var               members         = new List<(string, string)>(enumMembers.Length);
        string?           forwardName     = null;
        string?           backwardName    = null;
        INamedTypeSymbol? type            = null;
        var               forwardDefault  = string.Empty;
        var               backwardDefault = string.Empty;
        var               @namespace      = enumSymbol.ContainingNamespace.Name;
        var               @class          = $"{enumName}Extensions";

        if (Utility.FindAttribute(semanticModel.Compilation, enumSymbol, $"Luna.Generators.{nameof(AssociatedEnumAttribute)}") is { } attribute)
        {
            var arguments = attribute.ConstructorArguments;
            if (arguments[0].Value is INamedTypeSymbol t)
                type = t;
            if (arguments[1].Value is string f)
                forwardName = f;
            if (arguments[2].Value is string b)
                backwardName = b;
            if (arguments[3].Value is string d1)
                forwardDefault = d1;
            if (arguments[4].Value is string d2)
                backwardDefault = d2;
            if (arguments[5].Value is string n)
                @namespace = n;
            if (arguments[6].Value is string c)
                @class = c;
        }

        if (type is null)
            return null;

        forwardName  ??= $"To{type.Name}";
        backwardName ??= $"To{enumSymbol.Name}";
        if (forwardName.Length is 0 && backwardName.Length is 0)
            return null;

        var associatedAttributeSymbol = semanticModel.Compilation.GetTypeByMetadataName("Luna.Generators.AssociateAttribute")!;

        // Get all the fields from the enum, and add their name to the list
        foreach (var member in enumMembers)
        {
            if (member is not IFieldSymbol symbol)
                continue;

            var add  = true;
            var name = member.Name;
            if (Utility.FindAttribute(symbol, associatedAttributeSymbol) is { } fieldAttribute)
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

        return new AssociatedEnumData(enumName, forwardName, backwardName, type, forwardDefault, backwardDefault, @namespace, @class,
            members);
    }

    private static string GenerateExtensionClass(in AssociatedEnumData associatedEnum)
    {
        var sb = IndentedStringBuilder.CreatePreamble();
        sb.OpenNamespace(associatedEnum.Namespace)
            .OpenExtensionClass(associatedEnum.Class);
        if (associatedEnum.ForwardMethod.Length > 0)
        {
            sb.Append("/// <summary> Get the associated <see cref=\"").AppendObject(associatedEnum.EnumType.FullyQualified)
                .Append("\"/> value. </summary>").AppendLine();
            sb.GeneratedAttribute()
                .Append("public static ").AppendObject(associatedEnum.EnumType.FullyQualified).Append(' ').Append(associatedEnum.ForwardMethod)
                .Append("(this ")
                .AppendObject(associatedEnum.Name.FullyQualified).Indent().AppendLine(" value)")
                .AppendLine("=> value switch")
                .OpenBlock();
            foreach (var (value, name) in associatedEnum.Values)
            {
                sb.AppendObject(associatedEnum.Name.FullyQualified).Append('.').Append(value).Append(" => ")
                    .AppendObject(associatedEnum.EnumType.FullyQualified).Append('.').Append(name).Append(',').AppendLine();
            }

            if (associatedEnum.ForwardDefault.Length is 0)
                sb.AppendLine("_ => default,");
            else
                sb.Append("_ => ").AppendObject(associatedEnum.EnumType.FullyQualified).Append('.').Append(associatedEnum.ForwardDefault)
                    .Append(',').AppendLine();
            sb.CloseBlock().Append(';').AppendLine().Unindent();
        }

        if (associatedEnum.BackwardMethod.Length > 0)
        {
            if (associatedEnum.ForwardMethod.Length > 0)
                sb.AppendLine();
            sb.Append("/// <summary> Get the associated <see cref=\"").AppendObject(associatedEnum.Name.FullyQualified)
                .AppendLine("\"/> value. </summary>");
            sb.GeneratedAttribute().Append("public static ").AppendObject(associatedEnum.Name.FullyQualified).Append(' ').Append(associatedEnum.BackwardMethod)
                .Append("(this ")
                .AppendObject(associatedEnum.EnumType.FullyQualified).Indent().AppendLine(" value)")
                .AppendLine("=> value switch")
                .OpenBlock();
            foreach (var (value, name) in associatedEnum.Values)
            {
                sb.AppendObject(associatedEnum.EnumType.FullyQualified).Append('.').Append(name).Append(" => ")
                    .AppendObject(associatedEnum.Name.FullyQualified).Append('.').Append(value).Append(',').AppendLine();
            }

            if (associatedEnum.BackwardDefault.Length is 0)
                sb.AppendLine("_ => default,");
            else
                sb.Append("_ => ").AppendObject(associatedEnum.Name.FullyQualified).Append('.').Append(associatedEnum.BackwardDefault)
                    .Append(',').AppendLine();
            sb.CloseBlock().Append(';').AppendLine().Unindent();
        }

        sb.CloseAllBlocks();
        return sb.ToString();
    }
}
