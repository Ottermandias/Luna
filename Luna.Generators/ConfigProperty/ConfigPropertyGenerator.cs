using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Luna.Generators;

[Generator]
public sealed class ConfigPropertyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx
            => ctx.AddSource($"{nameof(ConfigPropertyAttribute)}.g.cs", ConfigPropertyAttribute.CreateAttribute().GetText(Encoding.UTF8)));

        var properties = context.SyntaxProvider
            .ForAttributeWithMetadataName($"Luna.Generators.{nameof(ConfigPropertyAttribute)}", IsCandidate, GetTypeToGenerate)
            .Where(static e => e.Item2.HasValue)
            .Select(static (a, _) => (a.Item1, a.Item2!.Value));

        var grouped = properties.GroupBy(static p => p.Left, static p => p.Right);

        context.RegisterSourceOutput(grouped, RegisterSources);
    }

    private static void RegisterSources(SourceProductionContext context,
        (HierarchyInfo Hierarchy, ValueCollection<ConfigPropertyData> Properties) item)
    {
        var compilationUnit = item.Hierarchy.GetCompilationUnit(item.Properties.SelectMany(p => p.GetSyntax()));
        context.AddSource($"{item.Hierarchy.FilenameHint}.g.cs", compilationUnit.GetText(Encoding.UTF8));
    }

    private static (HierarchyInfo, ConfigPropertyData?) GetTypeToGenerate(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        var hierarchy = HierarchyInfo.From(context.TargetSymbol.ContainingType);
        token.ThrowIfCancellationRequested();
        _ = TryGetInfo(context.TargetSymbol, context.SemanticModel, token, out var property);
        token.ThrowIfCancellationRequested();
        return (hierarchy, property);
    }

    private static bool TryGetInfo(ISymbol memberSymbol, SemanticModel semanticModel,
        CancellationToken token, out ConfigPropertyData? propertyInfo)
    {
        var propertyType = ((IFieldSymbol)memberSymbol).Type;
        var typeNameWithNullability =
            propertyType.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions
                    .IncludeNullableReferenceTypeModifier));
        var fieldName = memberSymbol.Name;
        propertyInfo = null;
        if (Utility.FindAttribute(semanticModel.Compilation, memberSymbol, "Luna.Generators.ConfigPropertyAttribute") is not { } attribute)
            // This can not happen.
            return false;

        token.ThrowIfCancellationRequested();
        var propertyName = GetPropertyName(memberSymbol, attribute);
        var saveName     = attribute.GetNamedArgument("SaveMethodName") as string ?? "Save";
        var eventName    = attribute.GetNamedArgument("EventName") as string;
        var skipSave     = attribute.GetNamedArgument("SkipSave") as bool? ?? false;
        token.ThrowIfCancellationRequested();
        if (skipSave)
            saveName = null;

        propertyInfo = new ConfigPropertyData(fieldName, propertyName, eventName, saveName, typeNameWithNullability);
        return true;
    }

    /// <summary> Remove leading underscores and upper-case the first letter, unless a name is specified via attribute. </summary>
    private static string GetPropertyName(ISymbol memberSymbol, AttributeData attribute)
    {
        if (attribute.GetNamedArgument("PropertyName") is string name)
            return name;

        var propertyName = memberSymbol.Name.AsMemory();
        if (propertyName.Span[0] is '_')
            propertyName = propertyName.Slice(1);
        var sb = new StringBuilder(propertyName.Length)
            .Append(char.ToUpperInvariant(propertyName.Span[0]))
            .Append(propertyName.Slice(1));
        return sb.ToString();
    }

    /// <summary> Only attributes on fields are valid, and they logically need to have at least one attribute. The containing type needs to be partial. </summary>
    private static bool IsCandidate(SyntaxNode node, CancellationToken token)
    {
        if (node is not VariableDeclaratorSyntax var1)
            return false;

        if (var1.Parent is not VariableDeclarationSyntax var2)
            return false;

        if (var2.Parent is not FieldDeclarationSyntax { AttributeLists.Count: > 0 } field)
            return false;

        return field.Parent is TypeDeclarationSyntax type && type.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
    }
}
