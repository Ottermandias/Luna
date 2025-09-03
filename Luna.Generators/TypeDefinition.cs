using Microsoft.CodeAnalysis;

namespace Luna.Generators;

internal readonly record struct TypeDefinition
{
    public TypeDefinition(Type type)
    {
        FullyQualified = type.FullName!;
        Name           = type.Name;
    }

    public TypeDefinition(string fullName)
    {
        FullyQualified = fullName;
        var dot = fullName.IndexOf('.');
        Name = dot < 0 ? fullName : fullName.Substring(dot + 1);
    }

    public TypeDefinition(INamedTypeSymbol type)
    {
        FullyQualified = type.ToString();
        var dot = FullyQualified.IndexOf('.');
        Name = dot < 0 ? FullyQualified : FullyQualified.Substring(dot + 1);
    }

    public readonly string FullyQualified;
    public readonly string Name;
}
