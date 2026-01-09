//HintName: StrongTypeAttribute.g.cs
using System;
using System.CodeDom.Compiler;

#nullable enable

#pragma warning disable CS9113

namespace Luna.Generators
{
    /// <summary> Flags that control which functionality this strong type should implement. </summary>
    [global::Microsoft.CodeAnalysis.EmbeddedAttribute]
    [GeneratedCode("Luna.Generators", "1.0.0.0")]
    [Flags]
    internal enum StrongTypeFlag : ulong
    {
        /// <summary> Whether the strong type is equatable to itself, including equality operators. </summary>
        EquatableSelf              = 1 << 0,
        /// <summary> Whether the strong type is equatable to its base type, including equality operators. </summary>
        EquatableBase              = 1 << 1,
        /// <summary> Whether the strong type is comparable to itself, including comparison operators, also implies <see cref="EquatableSelf"/>. </summary>
        ComparableSelf             = 1 << 2,
        /// <summary> Whether the strong type is comparable to its base type, including comparison operators, also implies <see cref="EquatableBase"/>. </summary>
        ComparableBase             = 1 << 3,
        /// <summary> Whether the strong type supports increment operators (++). </summary>
        Incrementable              = 1 << 4,
        /// <summary> Whether the strong type supports decrement operators (--). </summary>
        Decrementable              = 1 << 5,
        /// <summary> Whether the strong type supports addition with itself. </summary>
        AdditionSelf               = 1 << 6,
        /// <summary> Whether the strong type supports addition with its base type (in both directions). </summary>
        AdditionBase               = 1 << 7,
        /// <summary> Whether the strong type supports subtraction with itself (with itself as a return type). </summary>
        SubtractionSelf            = 1 << 8,
        /// <summary> Whether the strong type supports subtraction with its base type (with itself as a return type, only one direction). </summary>
        SubtractionBase            = 1 << 9,
        /// <summary> Whether the strong type can be implicitly converted from its base type. Otherwise it will still support explicit conversion. </summary>
        ImplicitConversionFromBase = 1 << 10,
        /// <summary> Whether the strong type can be explicitly converted back to its base type. Ignored if <see cref="ImplicitConversionToBase"/> is set. </summary>
        ExplicitConversionToBase   = 1 << 11,
        /// <summary> Whether the strong type can be implicitly converted back to its base type. </summary>
        ImplicitConversionToBase   = 1 << 12,
        /// <summary> Whether the strong type contains and applies a Newtonsoft.Json Converter. </summary>
        NewtonsoftConverter        = 1 << 13,
        /// <summary> Whether the strong type contains and applies a System.Text.Json Converter. </summary>
        SystemConverter            = 1 << 14,
        /// <summary> Whether the strong type contains a Zero entry. </summary>
        HasZero                    = 1 << 15,
        /// <summary> Whether the strong type contains a One entry. </summary>
        HasOne                     = 1 << 16,
        
        /// <summary> The default functionality for a basic type. </summary>
        Default = EquatableSelf | EquatableBase | ComparableBase | ComparableSelf | Incrementable | Decrementable | AdditionBase | SubtractionBase | ImplicitConversionFromBase | ExplicitConversionToBase | HasZero,
    }
    
    /// <summary> Create a strongly typed ID type struct. </summary>
    [global::Microsoft.CodeAnalysis.EmbeddedAttribute]
    [GeneratedCode("Luna.Generators", "1.0.0.0")]
    [AttributeUsage(AttributeTargets.Struct)]
    internal class StrongTypeAttribute<T>(string FieldName = "Value", StrongTypeFlag Flags = StrongTypeFlag.Default) : Attribute where T : unmanaged, System.Numerics.INumber<T>;
}

