//HintName: AssociatedEnum.Test_Test2.g.cs
using System;
using System.CodeDom.Compiler;

#nullable enable

public static partial class TestExtensions
{
    /// <summary> Get the associated <see cref="global::Test2"/> value. </summary>
    [GeneratedCode("Luna.Generators", "1.0.0.0")]
    public static global::Test2 ToTest2(this global::Test value)
        => value switch
        {
            global::Test.A => global::Test2.B,
            global::Test.B => global::Test2.B,
            _ => default,
        };
}

