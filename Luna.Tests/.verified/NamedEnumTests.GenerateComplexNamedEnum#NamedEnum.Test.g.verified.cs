//HintName: NamedEnum.Test.g.cs
using System;
using System.CodeDom.Compiler;

#nullable enable

namespace Test.Name.Space
{
    public static partial class TempClass
    {
        private static readonly global::ImSharp.StringU8 B_Name__GenU8 = new("Not B"u8);
        private static readonly global::ImSharp.StringU8 C_Name__GenU8 = new("C"u8);
        private static readonly global::ImSharp.StringU8 D_Name__GenU8 = new("D"u8);
        private static readonly global::ImSharp.StringU8 F_Name__GenU8 = new("Not F"u8);
        private static readonly global::ImSharp.StringU8 MissingEntry_Name__GenU8_ = new("ERROR"u8);
        
        /// <summary> Efficiently get a human-readable display name for this value. </summary>
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static global::ImSharp.StringU8 ToComplexNameU8(this global::Complex.Test.Test value)
            => value switch
            {
                global::Complex.Test.Test.B => B_Name__GenU8,
                global::Complex.Test.Test.C => C_Name__GenU8,
                global::Complex.Test.Test.D => D_Name__GenU8,
                global::Complex.Test.Test.F => F_Name__GenU8,
                _ => MissingEntry_Name__GenU8_,
            };
    }
}

