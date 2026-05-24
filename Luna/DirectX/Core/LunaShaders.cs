namespace Luna.DirectX;

/// <summary> A collection of built-in shaders. </summary>
public static class LunaShaders
{
    private static readonly Lazy<VertexShader> FsQuadVs = new(() => GetVertexShader("FsQuad"));

    private static readonly Lazy<PixelShader> ApplyIndexPs      = new(() => GetPixelShader("ApplyIndex"));
    private static readonly Lazy<PixelShader> ColorTransformPs  = new(() => GetPixelShader("ColorTransform"));
    private static readonly Lazy<PixelShader> DyeGlossOverlayPs = new(() => GetPixelShader("DyeGlossOverlay"));
    private static readonly Lazy<PixelShader> IdentityPs        = new(() => GetPixelShader("Identity"));
    private static readonly Lazy<PixelShader> Lanczos3Ps        = new(() => GetPixelShader("Lanczos3"));
    private static readonly Lazy<PixelShader> SymbolFilterPs    = new(() => GetPixelShader("SymbolFilter"));

    /// <summary> A generic full-screen quad vertex shader. </summary>
    /// <seealso cref="FullScreenQuad"/>
    public static VertexShader FsQuad
        => FsQuadVs.Value;

    /// <summary>
    ///   A pixel shader that applies a color palette according to an index map passed as input texture.
    ///   Use with <see cref="FsQuad"/> and <see cref="ApplyIndexUniforms"/>.
    /// </summary>
    public static PixelShader ApplyIndex
        => ApplyIndexPs.Value;

    /// <summary>
    ///   A pixel shader that applies a color transform to an input texture.
    ///   Use with <see cref="FsQuad"/> and <see cref="ColorTransformUniforms"/>.
    /// </summary>
    public static PixelShader ColorTransform
        => ColorTransformPs.Value;

    /// <summary>
    ///   A pixel shader used to render a dye gloss overlay in ImGui.
    ///   Use with <see cref="FsQuad"/> and <see cref="DyeGlossOverlayUniforms"/>.
    /// </summary>
    /// <seealso cref="FilterComboColors"/>
    public static PixelShader DyeGlossOverlay
        => DyeGlossOverlayPs.Value;

    /// <summary>
    ///   A pixel shader that passes its input texture through unchanged.
    ///   Use with <see cref="FsQuad"/>.
    /// </summary>
    public static PixelShader Identity
        => IdentityPs.Value;

    /// <summary>
    ///   A pixel shader that resamples its input texture using a Lanczos3 filter.
    ///   Use with <see cref="FsQuad"/>.
    /// </summary>
    public static PixelShader Lanczos3
        => Lanczos3Ps.Value;

    /// <summary>
    ///   A pixel shader that resamples its input texture using a filter designed around index map semantics.
    ///   Use with <see cref="FsQuad"/>.
    /// </summary>
    public static PixelShader SymbolFilter
        => SymbolFilterPs.Value;

    /// <summary> Input data for <see cref="ApplyIndex"/>. </summary>
    [StructLayout(LayoutKind.Sequential, Size = 0x210)]
    public struct ApplyIndexUniforms
    {
        /// <summary> An exponent to apply to the palette lookup/interpolation results. </summary>
        public Vector4 Exponent;

        /// <summary> The color palette. </summary>
        public Palette Palette;
    }

    /// <summary> A color palette. </summary>
    /// <seealso cref="ApplyIndex"/>
    [InlineArray(32)]
    public struct Palette
    {
        private Vector4 _element;
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

    private static VertexShader GetVertexShader(string shaderName)
        => new(ResourceProvider.GetManifestResourceBytes(shaderName + "_vs.dxbc"), shaderName);

    private static PixelShader GetPixelShader(string shaderName)
        => new(ResourceProvider.GetManifestResourceBytes(shaderName + "_ps.dxbc"), shaderName);

    private static GeometryShader GetGeometryShader(string shaderName)
        => new(ResourceProvider.GetManifestResourceBytes(shaderName + "_gs.dxbc"), shaderName);

    private static HullShader GetHullShader(string shaderName)
        => new(ResourceProvider.GetManifestResourceBytes(shaderName + "_hs.dxbc"), shaderName);

    private static DomainShader GetDomainShader(string shaderName)
        => new(ResourceProvider.GetManifestResourceBytes(shaderName + "_ds.dxbc"), shaderName);

    private static ComputeShader GetComputeShader(string shaderName)
        => new(ResourceProvider.GetManifestResourceBytes(shaderName + "_cs.dxbc"), shaderName);
}
