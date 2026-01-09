//HintName: AssociatedEnumAttribute.g.cs
using System;
using System.CodeDom.Compiler;

#nullable enable

#pragma warning disable CS9113

namespace Luna.Generators
{
    /// <summary> Add a method returning an associated enum value for this enum. Use with <see cref="Luna.Generators.AssociateAttribute"/>. </summary>
    /// <typeparam name="T"> The type of the associated enum. </param>
    /// <param name="ForwardMethod"> The name of the method going from this enum to the associated one. Method is omitted if empty. Name is constructed from other type if null. </param>
    /// <param name="BackwardMethod"> The name of the method going from the associated enum back to this one. Method is omitted if empty. Name is constructed from this type if null. </param>
    /// <param name="ForwardDefaultValue"> The default value used for unknown or omitted values in the forward method. </param>
    /// <param name="BackwardDefaultValue"> The default value used for unknown or omitted values in the backward method. If this is <c>null</c> <c>default</c> is used. </param>
    /// <param name="Namespace"> The namespace to put the extension class into. If this is null, the namespace of the enum will be used. </param>
    /// <param name="Class"> The name of the static class containing the extension methods. If this is null, <c>[EnumName]Extensions</c> will be used. </param>
    [global::Microsoft.CodeAnalysis.EmbeddedAttribute]
    [GeneratedCode("Luna.Generators", "1.0.0.0")]
    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = true)]
    internal class AssociatedEnumAttribute<T>(string? ForwardMethod = null, string? BackwardMethod = "", T ForwardDefaultValue = default!, object? BackwardDefaultValue = null, string? Namespace = null, string? Class = null) : Attribute where T : Enum;
    
    /// <summary> The name to provide when <see cref="Luna.Generators.NamedEnumAttribute"/> is used for this enum. </summary>
    /// <typeparam name="T"> The type of the associated enum. </param>
    /// <param name="Value"> The associated value. If this is null, the name of the attributed value itself is used. </param>
    /// <param name="Associate"> Whether to associate this value from the enum or omit it and treat it as undefined. </param>
    /// <param name="DefaultName"> Whether to take the name from the attributed enum value, ignoring the provided value. </param>
    /// <remarks> This is only intended for enum values. If this is omitted, the name of the value itself is used. </remarks>
    [global::Microsoft.CodeAnalysis.EmbeddedAttribute]
    [GeneratedCode("Luna.Generators", "1.0.0.0")]
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    internal class AssociateAttribute<T>(T Value, bool Associate = true, bool DefaultName = false) : Attribute where T : Enum
    {
        [global::System.Runtime.CompilerServices.OverloadResolutionPriority(100)]
        public AssociateAttribute(bool Associate = true)
            : this(default!, Associate, true)
        {}
    }
}

