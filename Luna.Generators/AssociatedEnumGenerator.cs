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
        .AppendLine("/// <typeparam name=\"T\"> The type of the associated enum. </param>")
        .AppendLine(
            "/// <param name=\"ForwardMethod\"> The name of the method going from this enum to the associated one. Method is omitted if empty. Name is constructed from other type if null. </param>")
        .AppendLine(
            "/// <param name=\"BackwardMethod\"> The name of the method going from the associated enum back to this one. Method is omitted if empty. Name is constructed from this type if null. </param>")
        .AppendLine(
            "/// <param name=\"ForwardDefaultValue\"> The default value used for unknown or omitted values in the forward method. </param>")
        .AppendLine(
            "/// <param name=\"BackwardDefaultValue\"> The default value used for unknown or omitted values in the backward method. If this is <c>null</c> <c>default</c> is used. </param>")
        .AppendLine(
            "/// <param name=\"Namespace\"> The namespace to put the extension class into. If this is null, the namespace of the enum will be used. </param>")
        .AppendLine(
            "/// <param name=\"Class\"> The name of the static class containing the extension methods. If this is null, <c>[EnumName]Extensions</c> will be used. </param>")
        .AppendLine("[AttributeUsage(AttributeTargets.Enum, AllowMultiple = true)]")
        .GeneratedAttribute()
        .AppendLine(
            "internal class AssociatedEnumAttribute<T>(string? ForwardMethod = null, string? BackwardMethod = \"\", T ForwardDefaultValue = default!, object? BackwardDefaultValue = null, string? Namespace = null, string? Class = null) : Attribute where T : Enum;")
        .AppendLine()
        .AppendLine(
            "/// <summary> The name to provide when <see cref=\"Luna.Generators.NamedEnumAttribute\"/> is used for this enum. </summary>")
        .AppendLine("/// <typeparam name=\"T\"> The type of the associated enum. </param>")
        .AppendLine(
            "/// <param name=\"Value\"> The associated value. If this is null, the name of the attributed value itself is used. </param>")
        .AppendLine(
            "/// <param name=\"Associate\"> Whether to associate this value from the enum or omit it and treat it as undefined. </param>")
        .AppendLine(
            "/// <param name=\"DefaultName\"> Whether to take the name from the attributed enum value, ignoring the provided value. </param>")
        .AppendLine("/// <remarks> This is only intended for enum values. If this is omitted, the name of the value itself is used. </remarks>")
        .AppendLine("[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]")
        .GeneratedAttribute()
        .AppendLine("internal class AssociateAttribute<T>(T Value, bool Associate = true, bool DefaultName = false) : Attribute where T : Enum")
        .OpenBlock()
        .AppendLine("[global::System.Runtime.CompilerServices.OverloadResolutionPriority(100)]")
        .AppendLine("public AssociateAttribute(bool Associate = true)")
        .AppendLine("    : this(default!, Associate, true)")
        .AppendLine("{}")
        .CloseAllBlocks().ToString();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        Utility.AddTagAttributes(ref context, AssociatedEnumAttribute, nameof(AssociatedEnumAttribute));
        Utility.Generate<ValueCollection<AssociatedEnumData>>(ref context, nameof(AssociatedEnumAttribute) + "`1",
            static (ctx, _) => GetEnumsToGenerate(ctx.SemanticModel, ctx.TargetNode),
            static (spc, source) => Execute(source, spc));
    }

    private static void Execute(IEnumerable<AssociatedEnumData>? enumsToGenerate, SourceProductionContext context)
    {
        if (enumsToGenerate is null)
            return;

        foreach (var enumToGenerate in enumsToGenerate)
        {
            if (enumToGenerate is { ForwardMethod.Length: 0, BackwardMethod.Length: 0 })
                return;

            var result = GenerateExtensionClass(enumToGenerate);
            context.AddSource($"AssociatedEnum.{enumToGenerate.Name.Name}_{enumToGenerate.EnumType.Name}.g.cs",
                SourceText.From(result, Encoding.UTF8));
        }
    }

    private static ValueCollection<AssociatedEnumData> GetEnumsToGenerate(SemanticModel semanticModel, SyntaxNode enumDeclarationSyntax)
    {
        if (semanticModel.GetDeclaredSymbol(enumDeclarationSyntax) is not INamedTypeSymbol enumSymbol)
            return new ValueCollection<AssociatedEnumData>([]);

        var list = new List<AssociatedEnumData>();
        foreach (var attribute in Utility.FindGenericAttributes(semanticModel.Compilation, enumSymbol,
                     $"Luna.Generators.{nameof(AssociatedEnumAttribute)}`1"))
        {
            var arguments = attribute.ConstructorArguments;
            if (attribute.AttributeClass!.TypeArguments.IsEmpty || attribute.AttributeClass!.TypeArguments[0] is not INamedTypeSymbol type)
                continue;

            var     enumName        = enumSymbol.ToString();
            var     enumMembers     = enumSymbol.GetMembers();
            var     members         = new List<(string, string)>(enumMembers.Length);
            string? forwardName     = null;
            string? backwardName    = null;
            var     forwardDefault  = string.Empty;
            var     backwardDefault = string.Empty;
            var     @namespace      = enumSymbol.ContainingNamespace.Name;
            var     @class          = $"{enumName}Extensions";

            if (arguments[0].Value is string f)
                forwardName = f;
            if (arguments[1].Value is string b)
                backwardName = b;
            if (Utility.GetEnumNameByValue(arguments[2].Type, arguments[2].Value) is { } d1)
                forwardDefault = d1;
            if (Utility.GetEnumNameByValue(arguments[3].Type, arguments[3].Value) is { } d2)
                backwardDefault = d2;
            if (arguments[4].Value is string n)
                @namespace = n;
            if (arguments[5].Value is string c)
                @class = c;

            forwardName  ??= $"To{type.Name}";
            backwardName ??= $"To{enumSymbol.Name}";
            if (forwardName.Length is 0 && backwardName.Length is 0)
                continue;

            var associatedAttributeSymbol =
                semanticModel.Compilation.GetTypeByMetadataName("Luna.Generators.AssociateAttribute`1")!.Construct([type],
                    [NullableAnnotation.NotAnnotated]);
            // Get all the fields from the enum, and add their name to the list
            foreach (var member in enumMembers)
            {
                if (member is not IFieldSymbol symbol)
                    continue;

                var name = member.Name;
                if (Utility.FindAttribute(symbol, associatedAttributeSymbol) is { } fieldAttribute)
                {
                    var fieldArguments = fieldAttribute.ConstructorArguments;
                    switch (fieldArguments.Length)
                    {
                        // Default / Omit constructor.
                        case 1:
                            if (fieldArguments[0].Value is false)
                                continue;

                            break;
                        // Value constructor. 
                        case 3:
                            if (fieldArguments[1].Value is false)
                                continue;

                            if (fieldArguments[2].Value is not true
                             && Utility.GetEnumNameByValue(fieldArguments[0].Type, fieldArguments[0].Value) is { } valueName)
                                name = valueName;
                            break;
                    }
                }

                members.Add((member.Name, name));
            }

            list.Add(new AssociatedEnumData(enumName, forwardName, backwardName, type, forwardDefault, backwardDefault, @namespace, @class,
                members));
        }

        return new ValueCollection<AssociatedEnumData>(list);
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
            sb.GeneratedAttribute().Append("public static ").AppendObject(associatedEnum.Name.FullyQualified).Append(' ')
                .Append(associatedEnum.BackwardMethod)
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
