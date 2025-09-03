//HintName: DataEnumAttribute.g.cs
using System;
using System.CodeDom.Compiler;

#nullable enable

#pragma warning disable CS9113

namespace Luna.Generators
{
    /// <summary> Add a method returning an associated data point for this enum. Use with <see cref="Luna.Generators.DataAttribute"/>. </summary>
    /// <param name="DataType"> The type of the associated data points. </param>
    /// <param name="Method"> The name of the method going from this enum to the data points. This also has to be added to the <see cref="Luna.Generators.DataAttribute"/> attributes. </param>
    /// <param name="DefaultValue"> The text for the default value used for unknown or omitted values in the method. If this is empty, <c>default</c> is used. </param>
    /// <param name="Nullable"> Whether the returned value can be null and is marked as nullable or not. </param>
    /// <param name="Namespace"> The namespace to put the extension class into. If this is null, the namespace of the enum will be used. </param>
    /// <param name="Class"> The name of the static class containing the extension methods. If this is null, <c>[EnumName]Extensions</c> will be used. </param>
    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = true)]
    [GeneratedCode("Luna.Generators", "1.0.0.0")]
    internal class DataEnumAttribute(Type Data, string Method, string DefaultValue = "", bool Nullable = true, string? Namespace = null, string? Class = null) : Attribute;
    
    /// <summary> The data to provide when <see cref="Luna.Generators.DataEnumAttribute"/> is used for this enum. </summary>
    /// <param name="Method"> The method to attach to. </param>
    /// <param name="Data"> The data to provide. </param>
    /// <param name="Omit"> Whether to omit this value from the enum and treat it as undefined. Same behavior as not providing an attribute. </param>
    /// <remarks> This is intended to provide code replacements as data points, so the text entered is not escaped and will be compiled as is. </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    [GeneratedCode("Luna.Generators", "1.0.0.0")]
    internal class DataAttribute(string Method, string Data, bool Omit = false) : Attribute;
}

