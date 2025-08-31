using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Luna.Generators;

public readonly struct EnumToGenerate(
    string name,
    string methodName,
    string unknownName,
    bool utf8,
    bool utf16,
    params IEnumerable<(string Value, string Name)> values)
{
    public readonly string                                      Name       = name;
    public readonly string                                      MethodName = methodName;
    public readonly string                                      Unknown    = unknownName;
    public readonly bool                                        Utf8       = utf8;
    public readonly bool                                        Utf16      = utf16;
    public readonly ImmutableArray<(string Value, string Name)> Values     = [..values];
}

[Generator]
public sealed class NamedEnumGenerator : IIncrementalGenerator
{
    private const string NamedEnumAttribute = """
                                              #nullable enable
                                              
                                              namespace Luna.Generators
                                              {
                                                  [System.AttributeUsage(System.AttributeTargets.Enum)]
                                                  public class NamedEnumAttribute(string method = "ToName", bool u8 = true, bool u16 = true, string unknown = "Unknown") : System.Attribute
                                                  {
                                                      public readonly string Method = method;
                                                      public readonly string UnknownName = unknown;
                                                      public readonly bool Utf8 = u8;
                                                      public readonly bool Utf16 = u16;
                                                  }
                                                  
                                                  /// <remarks> This is only intended for enum values. </remarks>
                                                  [System.AttributeUsage(System.AttributeTargets.Field)]
                                                  public class NamedAttribute(string? name = null, bool omit = false) : System.Attribute
                                                  {
                                                      public readonly string? Name = name;
                                                      public readonly bool Omit = omit;
                                                  }
                                              }
                                              """;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx
            => ctx.AddSource("NamedEnumAttribute.g.cs", SourceText.From(NamedEnumAttribute, Encoding.UTF8)));

        var enumsToGenerate = context.SyntaxProvider
            .ForAttributeWithMetadataName("Luna.Generators.NamedEnumAttribute", static (s, _) => true,
                static (ctx, _) => GetEnumToGenerate(ctx.SemanticModel, ctx.TargetNode))
            .Where(e => e.HasValue);

        context.RegisterSourceOutput(enumsToGenerate, static (spc, source) => Execute(source, spc));
    }

    private static void Execute(EnumToGenerate? enumToGenerate, SourceProductionContext context)
    {
        if (enumToGenerate is not { } value || value is { Utf8: false, Utf16: false })
            return;

        var result = GenerateExtensionClass(value);
        context.AddSource($"NamedEnum.{value.Name}.g.cs", SourceText.From(result, Encoding.UTF8));
    }

    private static EnumToGenerate? GetEnumToGenerate(SemanticModel semanticModel, SyntaxNode enumDeclarationSyntax)
    {
        // Get the semantic representation of the enum syntax
        if (semanticModel.GetDeclaredSymbol(enumDeclarationSyntax) is not INamedTypeSymbol enumSymbol)
            // something went wrong
            return null;

        // Get the full type name of the enum e.g. Colour, 
        // or OuterClass<T>.Colour if it was nested in a generic type (for example)
        var enumName = enumSymbol.ToString();

        // Get all the members in the enum
        var enumMembers = enumSymbol.GetMembers();
        var members     = new List<(string, string)>(enumMembers.Length);

        // Get all the fields from the enum, and add their name to the list
        foreach (var member in enumMembers)
        {
            if (member is IFieldSymbol { ConstantValue: not null })
            {
                //var attributes = member.GetAttributes();
                //// TODO: handle omit, do correctly.
                //if (attributes.First(a => a.AttributeClass?.Name is "Luna.Generators.NamedAttribute") is { } attribute)
                //    members.Add((member.Name, attribute.NamedArguments[0].Value.Value as string ?? member.Name));
                //else
                //    members.Add((member.Name, member.Name));
            }
        }

        var methodName  = "ToName";
        var unknownName = "Unknown";
        var utf8        = true;
        var utf16       = true;
        if (!utf8 && !utf16)
            return null;

        return new EnumToGenerate(enumName, methodName, unknownName, utf8, utf16, members);
    }

    private static string GenerateExtensionClass(EnumToGenerate @enum)
    {
        var sb = new StringBuilder();
        sb.AppendLine("#nullable enable")
            .AppendLine("")
            .AppendLine("namespace Luna.Generators")
            .AppendLine("{")
            .AppendLine("    public static partial class EnumExtensions")
            .AppendLine("    {");
        if (@enum.Utf8)
        {
            sb.Append("        public static string ").Append(@enum.MethodName).Append("(this ").Append(@enum.Name).Append(" value)\n")
                .AppendLine("            => value switch")
                .AppendLine("            {");
            foreach (var (value, name) in @enum.Values)
                sb.Append("                ").Append(@enum.Name).Append('.').Append(value).Append(" => \"").Append(name).Append("\",\n");
            sb.Append("                _ => \"").Append(@enum.Unknown).Append("\",\n")
                .AppendLine("            };");
        }

        if (@enum.Utf16)
        {
            sb.Append("        public static ReadOnlySpan<byte> ").Append(@enum.MethodName).Append("U8(this ").Append(@enum.Name)
                .Append(" value)\n")
                .AppendLine("            => value switch")
                .AppendLine("            {");
            foreach (var (value, name) in @enum.Values)
                sb.Append("                ").Append(@enum.Name).Append('.').Append(value).Append(" => \"").Append(name).Append("\"u8,\n");
            sb.Append("                _ => \"").Append(@enum.Unknown).Append("\"u8,\n")
                .Append("            };");
        }

        sb.AppendLine("    }")
            .AppendLine("}");
        return sb.ToString();
    }
}
