//HintName: NamedEnumAttribute.g.cs
using System;
using System.CodeDom.Compiler;

#nullable enable

#pragma warning disable CS9113

namespace Luna.Generators
{
    /// <summary> Add a method returning names in UTF8 and UTF16 to the enum. Use with <see cref="Luna.Generators.NameAttribute"/>. </summary>
    /// <param name="Method"> The name of the UTF16 method provided. The UTF8 version has 'U8' appended to this name, if also provided. </param>
    /// <param name="Utf8"> Whether to provide a UTF8 version of the method. </param>
    /// <param name="Utf16"> Whether to provide a UTF16 version of the method. </param>
    /// <param name="Unknown"> The name to provide for omitted or undefined values of the enum. </param>
    /// <param name="Namespace"> The namespace to put the extension class into. If this is null, the namespace of the enum will be used. </param>
    /// <param name="Class"> The name of the static class containing the extension methods. If this is null, <c>[EnumName]Extensions</c> will be used. </param>
    [global::Microsoft.CodeAnalysis.EmbeddedAttribute]
    [GeneratedCode("Luna.Generators", "1.0.0.0")]
    [AttributeUsage(AttributeTargets.Enum)]
    internal class NamedEnumAttribute(string Method = "ToName", bool Utf8 = true, bool Utf16 = true, string Unknown = "Unknown", string? Namespace = null, string? Class = null) : Attribute;
    
    /// <summary> The name to provide when <see cref="Luna.Generators.NamedEnumAttribute"/> is used for this enum. </summary>
    /// <param name="Name"> The name to provide. If this is null, the name of the value itself is used. </param>
    /// <param name="Omit"> Whether to omit this value from the enum and treat it as undefined. </param>
    /// <remarks> This is only intended for enum values. If this is omitted, the name of the value itself is used. </remarks>
    [global::Microsoft.CodeAnalysis.EmbeddedAttribute]
    [GeneratedCode("Luna.Generators", "1.0.0.0")]
    [AttributeUsage(AttributeTargets.Field)]
    internal class NameAttribute(string? Name = null, bool Omit = false) : Attribute;
}

