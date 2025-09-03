//HintName: NamedEnum.Test.Test.g.cs
using System;
using System.CodeDom.Compiler;

#nullable enable

namespace Test.Name.Space
{
    public static partial class TempClass
    {
        /// <summary> Efficiently get a human-readable display name for this value. </summary>
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static string ToComplexName(this global::Complex.Test.Test value)
            => value switch
            {
                global::Complex.Test.Test.B => "Not B",
                global::Complex.Test.Test.C => "C",
                global::Complex.Test.Test.D => "D",
                global::Complex.Test.Test.F => "Not F",
                _ => "ERROR",
            };
    }
}

