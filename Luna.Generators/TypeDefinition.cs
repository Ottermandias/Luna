using Microsoft.CodeAnalysis;

namespace Luna.Generators;

/// <summary> A wrapper for a type to store its fully qualified and display name. </summary>
internal readonly record struct TypeDefinition
{
    /// <summary> Create a wrapper from a System type. </summary>
    public TypeDefinition(Type type)
    {
        Name           = type.Name;
        FullyQualified = type.FullName ?? Name;
    }

    /// <summary> Create a wrapper from a given fully qualified name. </summary>
    public TypeDefinition(string fullName)
    {
        FullyQualified = fullName;
        var dot = fullName.LastIndexOf('.');
        Name = dot < 0 ? fullName : fullName.Substring(dot + 1);
    }

    /// <summary> Create a wrapper from a Roslyn named type symbol. </summary>
    public TypeDefinition(INamedTypeSymbol type)
    {
        FullyQualified = type.ToString();
        var dot = FullyQualified.LastIndexOf('.');
        Name = dot < 0 ? FullyQualified : FullyQualified.Substring(dot + 1);
    }

    /// <summary> The fully qualified name, including namespaces and parent types. </summary>
    public readonly string FullyQualified;

    /// <summary> The display name of the type only. </summary>
    public readonly string Name;
}
