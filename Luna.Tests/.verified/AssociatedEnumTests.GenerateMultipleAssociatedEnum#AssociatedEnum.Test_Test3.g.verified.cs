//HintName: AssociatedEnum.Test_Test3.g.cs
using System;
using System.CodeDom.Compiler;

#nullable enable

public static partial class TestExtensions
{
    /// <summary> Get the associated <see cref="global::Test3"/> value. </summary>
    [GeneratedCode("Luna.Generators", "1.0.0.0")]
    public static global::Test3 ToTest3(this global::Test value)
        => value switch
        {
            global::Test.A => global::Test3.A,
            global::Test.B => global::Test3.B,
            global::Test.C => global::Test3.C,
            global::Test.D => global::Test3.D,
            _ => global::Test3.A,
        };
}

