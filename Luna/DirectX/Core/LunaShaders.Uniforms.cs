using Luna.Generators;

namespace Luna.DirectX;

partial class LunaShaders
{
    /// <summary> Input data for <see cref="ApplyIndex"/>. </summary>
    [StructLayout(LayoutKind.Sequential, Size = 0x210)]
    public struct ApplyIndexUniforms
    {
        /// <summary> An exponent to apply to the palette lookup/interpolation results. </summary>
        public Vector4 Exponent;

        /// <summary> The color palette. </summary>
        public Palette Palette;
    }

    /// <summary> Input data for <see cref="Blend4"/>. </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x30)]
    public struct Blend4Uniforms
    {
        /// <summary> The foreground UV transform. </summary>
        [FieldOffset(0)]
        public Vector4 ForegroundTransform;

        /// <summary> The foreground UV offset. </summary>
        [FieldOffset(0x10)]
        public Vector2 ForegroundOffset;

        /// <summary> The blend function to use. </summary>
        [FieldOffset(0x18)]
        public Blend Blend;

        /// <summary> Input data for the blend function. </summary>
        [FieldOffset(0x20)]
        public BlendParameters BlendParameters;
    }

    /// <summary> Input data for <see cref="Composite"/>. </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x50)]
    public struct CompositeUniforms
    {
        /// <summary> The foreground UV transform. </summary>
        [FieldOffset(0)]
        public Vector4 ForegroundTransform;

        /// <summary> The foreground UV offset. </summary>
        [FieldOffset(0x10)]
        public Vector2 ForegroundOffset;

        /// <summary> The blend function to use. </summary>
        [FieldOffset(0x18)]
        public Blend Blend;

        /// <summary> Porter/Duff compositing weights for color. X = Destination, Y = Source, Z = Both. </summary>
        /// <seealso cref="CompositeWeightPresets"/>
        [FieldOffset(0x20)]
        public Vector3 ColorCompositeWeights;

        /// <summary> Porter/Duff compositing weights for alpha. X = Destination, Y = Source, Z = Both. </summary>
        /// <seealso cref="CompositeWeightPresets"/>
        [FieldOffset(0x30)]
        public Vector3 AlphaCompositeWeights;

        /// <summary> Input data for the blend function. </summary>
        [FieldOffset(0x40)]
        public BlendParameters BlendParameters;
    }

    /// <summary> Input data for <see cref="CompositeControlled"/>. </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x70)]
    public struct CompositeControlledUniforms
    {
        /// <summary> The foreground UV transform. </summary>
        [FieldOffset(0)]
        public Vector4 ForegroundTransform;

        /// <summary> The foreground UV offset. </summary>
        [FieldOffset(0x10)]
        public Vector2 ForegroundOffset;

        /// <summary> The blend function to use. </summary>
        [FieldOffset(0x18)]
        public Blend Blend;

        /// <summary> Porter/Duff compositing weights. X = Destination, Y = Source, Z = Both. </summary>
        /// <seealso cref="CompositeWeightPresets"/>
        [FieldOffset(0x20)]
        public Vector3 CompositeWeights;

        /// <summary> The background control bias. </summary>
        [FieldOffset(0x2C)]
        public float BackgroundControl0;

        /// <summary> Porter/Duff compositing weights for the control mask. X = Destination, Y = Source, Z = Both. </summary>
        /// <seealso cref="CompositeWeightPresets"/>
        [FieldOffset(0x30)]
        public Vector3 ControlCompositeWeights;

        /// <summary> The foreground control bias. </summary>
        [FieldOffset(0x3C)]
        public float ForegroundControl0;

        /// <summary> The background control weights. </summary>
        [FieldOffset(0x40)]
        public Vector4 BackgroundControlWeights;

        /// <summary> The foreground control weights. </summary>
        [FieldOffset(0x50)]
        public Vector4 ForegroundControlWeights;

        /// <summary> Input data for the blend function. </summary>
        [FieldOffset(0x60)]
        public BlendParameters BlendParameters;
    }

    /// <summary> Input data for <see cref="ColorTransform"/>. </summary>
    [StructLayout(LayoutKind.Sequential, Size = 0x50)]
    public struct ColorTransformUniforms
    {
        /// <summary> The red basis vector of the transform. </summary>
        public Vector4 BasisRed;

        /// <summary> The green basis vector of the transform. </summary>
        public Vector4 BasisGreen;

        /// <summary> The blue basis vector of the transform. </summary>
        public Vector4 BasisBlue;

        /// <summary> The alpha basis vector of the transform. </summary>
        public Vector4 BasisAlpha;

        /// <summary> The origin point of the transform. </summary>
        public Vector4 Origin;
    }

    /// <summary> Input data for <see cref="DyeGlossOverlay"/>. </summary>
    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct DyeGlossOverlayUniforms
    {
        /// <summary> The radius of the rounding of frame corners, in clockwise order starting from upper left. </summary>
        /// <seealso cref="Im.ImGuiStyle.FrameRounding"/>
        public Vector4 Rounding;
    }

    /// <summary> Input data for <see cref="KawaseDownsample"/>, <see cref="KawaseUpsample"/> and <see cref="KawaseUpsampleComposite"/>. </summary>
    [StructLayout(LayoutKind.Sequential, Size = 0x30)]
    public struct KawaseUniforms
    {
        /// <summary> The UV coordinates of the upper-left corner of the blurred rectangle. </summary>
        public Vector2 BlurRectUvMin;

        /// <summary> The UV coordinates of the lower-right corner of the blurred rectangle. </summary>
        public Vector2 BlurRectUvMax;

        /// <summary> The radius of the rounding of frame corners for the blurred rectangle, in clockwise order starting from upper left. </summary>
        /// <seealso cref="Im.ImGuiStyle.FrameRounding"/>
        public Vector4 BlurRectRounding;

        /// <summary> Kawase spread factor; typical range 0.5 – 4. </summary>
        public float BlurStrength;

        /// <summary> The opacity of the regions left untouched by the blur. </summary>
        public float UnblurredOpacity;
    }

    /// <summary> Input data for <see cref="RefractionRaycast"/>. </summary>
    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct RefractionRaycastUniforms
    {
        /// <summary> The index of refraction of the surface represented by the input normal map. </summary>
        public float IndexOfRefraction;

        /// <summary> The depth beyond the surface. </summary>
        public float Depth;
    }

    /// <summary>
    ///   Linkage data for <see cref="Blend4"/>, <see cref="Composite"/>, <see cref="CompositeControlled"/> and <see cref="Resample"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct FilterLinkage
    {
        /// <summary> The filter to use to sample the input. </summary>
        public Filter Filter;
    }

    /// <summary> Input data for <c>blend_function</c> implementations. </summary>
    /// <seealso cref="Blend"/>
    /// <seealso cref="Blend4Uniforms"/>
    /// <seealso cref="CompositeUniforms"/>
    /// <seealso cref="CompositeControlledUniforms"/>
    [StructLayout(LayoutKind.Explicit, Size = 0x10)]
    public struct BlendParameters
    {
        [FieldOffset(0)]
        public Vector4 LerpWeights;
    }

    /// <summary> A color palette. </summary>
    /// <seealso cref="ApplyIndex"/>
    [InlineArray(Length)]
    public struct Palette
    {
        /// <summary> The number of items in this struct. </summary>
        public const int Length = 32;

        private Vector4 _element;
    }

    /// <summary> Pre-defined vectors of Porter/Duff compositing weights. X = Destination, Y = Source, Z = Both. </summary>
    /// <seealso cref="CompositeFunction"/>
    /// <seealso cref="ToWeights"/>
    public static class CompositeWeightPresets
    {
        /// <inheritdoc cref="CompositeFunction.Over"/>
        public static readonly Vector3 Over = Vector3.One;

        /// <inheritdoc cref="CompositeFunction.Source"/>
        public static readonly Vector3 Source = new(0.0f, 1.0f, 1.0f);

        /// <inheritdoc cref="CompositeFunction.Atop"/>
        public static readonly Vector3 Atop = new(1.0f, 0.0f, 1.0f);

        /// <inheritdoc cref="CompositeFunction.In"/>
        public static readonly Vector3 In = Vector3.UnitZ;

        /// <inheritdoc cref="CompositeFunction.Xor"/>
        public static readonly Vector3 Xor = new(1.0f, 1.0f, 0.0f);

        /// <inheritdoc cref="CompositeFunction.Out"/>
        public static readonly Vector3 Out = Vector3.UnitY;

        /// <inheritdoc cref="CompositeFunction.DestinationOut"/>
        public static readonly Vector3 DestinationOut = Vector3.UnitX;

        /// <inheritdoc cref="CompositeFunction.Clear"/>
        public static readonly Vector3 Clear = Vector3.Zero;
    }

    /// <summary> Porter/Duff compositing functions. </summary>
    /// <seealso cref="CompositeWeightPresets"/>
    [NamedEnum]
    public enum CompositeFunction
    {
        /// <summary> The source is drawn over the destination. This is the standard compositing operator. </summary>
        Over = 7,

        /// <summary> The destination is only kept where it overlaps with the source. </summary>
        Source = 6,

        /// <summary> The source is only kept where it overlaps with the destination. </summary>
        Atop = 5,

        /// <summary> Both source and destination are only kept where they overlap. </summary>
        In = 4,

        /// <summary> Both source and destination are only kept where they do not overlap. </summary>
        Xor = 3,

        /// <summary> The source is only kept where it doesn't overlap with the destination. </summary>
        Out = 2,

        /// <summary> The destination is only kept where it doesn't overlap with the source. </summary>
        [Name("Destination Out")]
        DestinationOut = 1,

        /// <summary> Nothing is kept. </summary>
        Clear = 0,
    }

    /// <summary> Blend functions. </summary>
    /// <seealso cref="Blend4"/>
    /// <seealso cref="Composite"/>
    /// <seealso cref="CompositeControlled"/>
    [NamedEnum]
    [Flags]
    public enum Blend : uint
    {
        /// <summary> The Normal blend mode. </summary>
        [Name("Normal")]
        Source = 0,

        /// <summary> Darkens the colors by multiplying the source and destination values. </summary>
        [Name("Multiply")]
        Multiply = 1,

        /// <summary> Darkens the source where the destination is darker, and lightens the source where the destination is lighter. </summary>
        [Name("Overlay")]
        Overlay = 2,

        /// <summary> Linearly interpolates between the destination and source values. </summary>
        [Name("Linear Interpolation")]
        Lerp = 3,

        /// <summary> Lightens the colors by adding the source and destination values, also known as Linear Dodge. </summary>
        [Name("Add")]
        Add = 4,

        /// <summary> Darkens the colors by subtracting the source from the destination. </summary>
        [Name("Subtract")]
        Subtract = 5,

        /// <summary> Takes the absolute value of the difference between source and destination. </summary>
        [Name("Difference")]
        Difference = 6,

        /// <summary> Lightens the colors by dividing the destination by the source. </summary>
        [Name("Divide")]
        Divide = 7,

        /// <summary> Darkens the colors by taking the smallest between source and destination. </summary>
        [Name("Darken Only")]
        Min = 8,

        /// <summary> Lightens the colors by taking the largest between source and destination. </summary>
        [Name("Lighten Only")]
        Max = 9,

        /// <summary> Mask to extract the base function, removing the invert/swap modifiers. </summary>
        [Name(Omit: true)]
        FunctionMask = 0x07FFFFFF,

        /// <summary> Does not clamp the result of the function between 0 and 1. </summary>
        [Name(Omit: true)]
        Hdr = 0x08000000,

        /// <summary> Inverts the destination before applying the function. </summary>
        [Name(Omit: true)]
        InvertDestination = 0x10000000,

        /// <summary> Inverts the source before applying the function. </summary>
        [Name(Omit: true)]
        InvertSource = 0x20000000,

        /// <summary> Inverts the result after applying the function. </summary>
        [Name(Omit: true)]
        InvertResult = 0x40000000,

        /// <summary> Swaps the source and destination. Applied before <see cref="InvertDestination"/> and <see cref="InvertSource"/>. </summary>
        [Name(Omit: true)]
        SwapInputs = 0x80000000,

        /// <summary> Lightens the colors by multiplying the inverted source and destination values, and inverting the result again. </summary>
        [Name("Screen")]
        Screen = Multiply | InvertSource | InvertDestination | InvertResult,

        /// <summary> Lightens the destination by dividing it by the inverted source. </summary>
        [Name("Color Dodge")]
        ColorDodge = Divide | InvertSource,

        /// <summary> Darkens the destination by inverting it, dividing it by the source, and inverting it again. </summary>
        [Name("Color Burn")]
        ColorBurn = Divide | InvertDestination | InvertResult,
    }

    /// <summary> Resampling filters. </summary>
    /// <seealso cref="Blend4"/>
    /// <seealso cref="Composite"/>
    /// <seealso cref="CompositeControlled"/>
    /// <seealso cref="Resample"/>
    [NamedEnum]
    public enum Filter : uint
    {
        /// <summary> Performs a simple texture <c>Sample</c> call. </summary>
        [Name("Simple")]
        Simple = 0,

        /// <summary> Samples its texture using a Lanczos3 filter. </summary>
        [Name("Lanczos")]
        Lanczos3 = 1,

        /// <summary>
        ///   Samples its input texture using a bespoke filter that treats the red channel values as symbols,
        ///   and conditionally applies bilinear, nearest-neighbor, marching squares, or a blend thereof, depending on these symbols' equality.
        ///   Designed around index map semantics.
        /// </summary>
        [Name("Symbol Filter")]
        SymbolFilter = 2,
    }

    /// <summary> Extension methods for <see cref="CompositeFunction"/>. </summary>
    /// <param name="fn"> The compositing function. </param>
    extension(CompositeFunction fn)
    {
        /// <summary> Gets this Porter/Duff compositing function as a vector of weights. X = Destination, Y = Source, Z = Both. </summary>
        /// <exception cref="InvalidEnumArgumentException"> This compositing function is invalid. </exception>
        /// <seealso cref="CompositeWeightPresets"/>
        public Vector3 Weights
        {
            get
            {
                var numFn = (int)fn;
                if ((numFn & ~7) is not 0)
                    throw new InvalidEnumArgumentException(nameof(fn), (int)fn, typeof(CompositeFunction));

                return new Vector3(
                    (numFn & 1) is not 0 ? 1.0f : 0.0f,
                    (numFn & 2) is not 0 ? 1.0f : 0.0f,
                    (numFn & 4) is not 0 ? 1.0f : 0.0f);
            }
        }
    }

    /// <summary> Extension methods for <see cref="Blend"/>. </summary>
    /// <param name="blend"> The blend function. </param>
    extension(Blend blend)
    {
        /// <summary> Gets whether this blend function is commutative, that is, will always return the same result if its inputs are swapped. </summary>
        public bool Commutative
            => (blend & Blend.FunctionMask) is Blend.Multiply or Blend.Add or Blend.Difference or Blend.Min or Blend.Max
             && (blend & Blend.InvertDestination) is not 0 == (blend & Blend.InvertSource) is not 0;

        /// <summary> Gets a list of the values of this enum designating well-known functions, excluding the mask and modifier values. </summary>
        public static IEnumerable<Blend> WellKnownFunctions
            =>
            [
                Blend.Source, Blend.Multiply, Blend.Screen, Blend.Overlay, Blend.ColorDodge, Blend.ColorBurn, Blend.Lerp, Blend.Add,
                Blend.Subtract, Blend.Difference, Blend.Divide, Blend.Min, Blend.Max,
            ];
    }
}
