//HintName: AssociatedEnumAttribute.g.cs
using System;
using System.CodeDom.Compiler;

#nullable enable

#pragma warning disable CS9113

namespace Luna.Generators
{
    /// <summary> Add a method returning an associated enum value for this enum. Use with <see cref="Luna.Generators.AssociateAttribute"/>. </summary>
    /// <param name="Other"> The type of the associated enum. </param>
    /// <param name="ForwardMethod"> The name of the method going from this enum to the associated one. Method is omitted if empty. Name is constructed from other type if null. </param>
    /// <param name="BackwardMethod"> The name of the method going from the associated enum back to this one. Method is omitted if empty. Name is constructed from this type if null. </param>
    /// <param name="ForwardDefaultValue"> The name of the default value used for unknown or omitted values in the forward method. If this is null, <c>default</c> is used. </param>
    /// <param name="BackwardDefaultValue"> The name of the default value used for unknown or omitted values in the backward method. If this is null, <c>default</c> is used. </param>
    /// <param name="Namespace"> The namespace to put the extension class into. If this is null, the namespace of the enum will be used. </param>
    /// <param name="Class"> The name of the static class containing the extension methods. If this is null, <c>[EnumName]Extensions</c> will be used. </param>
    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = true)]
    [GeneratedCode("Luna.Generators", "1.0.0.0")]
    internal class AssociatedEnumAttribute(Type Other, string? ForwardMethod = null, string? BackwardMethod = "", string? ForwardDefaultValue = null, string? BackwardDefaultValue = null, string? Namespace = null, string? Class = null) : Attribute;
    
    /// <summary> The name to provide when <see cref="Luna.Generators.NamedEnumAttribute"/> is used for this enum. </summary>
    /// <param name="Name"> The name to provide. If this is null, the name of the value itself is used. </param>
    /// <param name="Omit"> Whether to omit this value from the enum and treat it as undefined. </param>
    /// <remarks> This is only intended for enum values. If this is omitted, the name of the value itself is used. </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    [GeneratedCode("Luna.Generators", "1.0.0.0")]
    internal class AssociateAttribute(string? Name = null, bool Omit = false) : Attribute;
}

