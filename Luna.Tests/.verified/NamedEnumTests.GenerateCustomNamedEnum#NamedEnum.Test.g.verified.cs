//HintName: NamedEnum.Test.g.cs
using System;
using System.CodeDom.Compiler;

#nullable enable

public static partial class TestExtensions
{
    /// <summary> Efficiently get a human-readable display name for this value. </summary>
    /// <remarks> For a UTF8 representation of the name, use <see cref="TestExtensions.ToCustomNameU8"/>. </remarks>
    [GeneratedCode("Luna.Generators", "1.0.0.0")]
    public static string ToCustomName(this global::Test value)
        => value switch
        {
            global::Test.A => "A",
            global::Test.B => "B",
            _ => "Unknown",
        };
    
    private static readonly global::ImSharp.StringU8 A_Name__GenU8 = new("A"u8);
    private static readonly global::ImSharp.StringU8 B_Name__GenU8 = new("B"u8);
    private static readonly global::ImSharp.StringU8 MissingEntry_Name__GenU8_ = new("Unknown"u8);
    
    /// <summary> Efficiently get a human-readable display name for this value. </summary>
    /// <remarks> For a UTF16 representation of the name, use <see cref="TestExtensions.ToCustomName"/>. </remarks>
    [GeneratedCode("Luna.Generators", "1.0.0.0")]
    public static global::ImSharp.StringU8 ToCustomNameU8(this global::Test value)
        => value switch
        {
            global::Test.A => A_Name__GenU8,
            global::Test.B => B_Name__GenU8,
            _ => MissingEntry_Name__GenU8_,
        };
}

