//HintName: DataEnum.Test_ToType.g.cs
using System;
using System.CodeDom.Compiler;

#nullable enable

public static partial class TestExtensions
{
    /// <summary> Get the associated data point of type <see cref="global::System.Type"/> value. </summary>
    [GeneratedCode("Luna.Generators", "1.0.0.0")]
    public static global::System.Type? ToType(this global::Test value)
        => value switch
        {
            global::Test.A => typeof(int),
            global::Test.B => typeof(string),
            _ => default,
        };
}

