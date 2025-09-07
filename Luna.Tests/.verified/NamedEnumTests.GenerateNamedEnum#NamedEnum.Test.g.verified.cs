//HintName: NamedEnum.Test.g.cs
using System;
using System.CodeDom.Compiler;

#nullable enable

public static partial class TestExtensions
{
    /// <summary> Efficiently get a human-readable display name for this value. </summary>
    /// <remarks> For a UTF8 representation of the name, use <see cref="global::TestExtensions.ToNameU8"/>. </remarks>
    [GeneratedCode("Luna.Generators", "1.0.0.0")]
    public static string ToName(this global::Test value)
        => value switch
        {
            global::Test.A => "A",
            global::Test.B => "B",
            _ => "Unknown",
        };
    
    /// <summary> Efficiently get a human-readable display name for this value. </summary>
    /// <remarks> For a UTF16 representation of the name, use <see cref="global::TestExtensions.ToName"/>. </remarks>
    [GeneratedCode("Luna.Generators", "1.0.0.0")]
    public static ReadOnlySpan<byte> ToNameU8(this global::Test value)
        => value switch
        {
            global::Test.A => "A"u8,
            global::Test.B => "B"u8,
            _ => "Unknown"u8,
        };
}

