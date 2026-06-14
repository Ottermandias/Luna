using TerraFX.Interop.DirectX;

namespace Luna.DirectX;

/// <summary> Categories of <see cref="DXGI_FORMAT"/> by component types. </summary>
public enum DxgiFormatComponentType
{
    /// <summary> Unknown. </summary>
    Unknown,

    /// <summary> The shader receives the value as its own floating-point representation of the appropriate size. </summary>
    Float,

    /// <summary> The shader receives the value as an unsigned integer of the appropriate size. </summary>
    UInt,

    /// <summary> The shader receives the value as a signed integer of the appropriate size. </summary>
    SInt,

    /// <summary> The shader receives the value as a floating-point value normalized to the range [0, 1], but the stored value is an unsigned integer of the appropriate size. </summary>
    UNorm,

    /// <summary> The shader receives the value as a floating-point value normalized to the range [-1, 1], but the stored value is a signed integer of the appropriate size. </summary>
    SNorm,
}
