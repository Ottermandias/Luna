namespace Luna.DirectX;

/// <summary> A collection of built-in shaders. </summary>
public static partial class LunaShaders
{
    private static readonly Lazy<VertexShader> FsQuadVs = new(() => GetVertexShader("FsQuad"));

    private static readonly Lazy<PixelShader> ApplyIndexPs              = new(() => GetPixelShader("ApplyIndex"));
    private static readonly Lazy<PixelShader> Blend4Ps                  = new(() => GetPixelShader("Blend4"));
    private static readonly Lazy<PixelShader> ColorTransformPs          = new(() => GetPixelShader("ColorTransform"));
    private static readonly Lazy<PixelShader> CompositePs               = new(() => GetPixelShader("Composite"));
    private static readonly Lazy<PixelShader> CompositeControlledPs     = new(() => GetPixelShader("CompositeControlled"));
    private static readonly Lazy<PixelShader> DivideByMaxPs             = new(() => GetPixelShader("DivideByMax"));
    private static readonly Lazy<PixelShader> DyeGlossOverlayPs         = new(() => GetPixelShader("DyeGlossOverlay"));
    private static readonly Lazy<PixelShader> KawaseDownsamplePs        = new(() => GetPixelShader("KawaseDownsample"));
    private static readonly Lazy<PixelShader> KawaseUpsampleCompositePs = new(() => GetPixelShader("KawaseUpsampleComposite"));
    private static readonly Lazy<PixelShader> KawaseUpsamplePs          = new(() => GetPixelShader("KawaseUpsample"));
    private static readonly Lazy<PixelShader> ResamplePs                = new(() => GetPixelShader("Resample"));

    private static readonly Lazy<ComputeShader> RefractionRaycastCs = new(() => GetComputeShader("RefractionRaycast"));
    private static readonly Lazy<ComputeShader> MaxCs               = new(() => GetComputeShader("Max"));

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
    ///   A pixel shader that rotates and scales a foreground input texture, then blends it onto a background input texture.
    ///   Alpha is not treated as opacity, and just gets blended along red, green and blue without special treatment.
    ///   Use with <see cref="FsQuad"/>, <see cref="Blend4Uniforms"/> and <see cref="FilterLinkage"/>.
    /// </summary>
    public static PixelShader Blend4
        => Blend4Ps.Value;

    /// <summary>
    ///   A pixel shader that applies a color transform to an input texture.
    ///   Use with <see cref="FsQuad"/> and <see cref="ColorTransformUniforms"/>.
    /// </summary>
    public static PixelShader ColorTransform
        => ColorTransformPs.Value;

    /// <summary>
    ///   A pixel shader that rotates and scales a foreground input texture, then blends and composites it onto a background input texture.
    ///   Alpha is treated as opacity.
    ///   Use with <see cref="FsQuad"/>, <see cref="CompositeUniforms"/> and <see cref="FilterLinkage"/>.
    /// </summary>
    public static PixelShader Composite
        => CompositePs.Value;

    /// <summary>
    ///   A pixel shader that rotates and scales a foreground input texture, then blends and composites it onto a background input texture.
    ///   Alpha is not treated as opacity. Instead, auxiliary control masks have to be provided.
    ///   Use with <see cref="FsQuad"/>, <see cref="CompositeUniforms"/> and <see cref="FilterLinkage"/>.
    /// </summary>
    public static PixelShader CompositeControlled
        => CompositeControlledPs.Value;

    /// <summary>
    ///   A pixel shader used to render a dye gloss overlay in ImGui.
    ///   Use with <see cref="FsQuad"/> and <see cref="DyeGlossOverlayUniforms"/>.
    /// </summary>
    /// <seealso cref="FilterComboColors"/>
    public static PixelShader DyeGlossOverlay
        => DyeGlossOverlayPs.Value;

    /// <summary>
    ///   A pixel shader that divides the contents of its input UInt texture by a maximum stored in a second 1x1 UInt texture.
    ///   Use with <see cref="FsQuad"/>.
    /// </summary>
    /// <seealso cref="Max"/>
    public static PixelShader DivideByMax
        => DivideByMaxPs.Value;

    /// <summary>
    ///   A pixel shader that resamples its input texture according to configurable filters.
    ///   Use with <see cref="FsQuad"/> and <see cref="FilterLinkage"/>.
    /// </summary>
    public static PixelShader Resample
        => ResamplePs.Value;

    /// <summary>
    ///   A pixel shader that applies a Dual Kawase downsampling pass to its input texture.
    ///   Use with <see cref="FsQuad"/> and <see cref="KawaseUniforms"/>.
    /// </summary>
    public static PixelShader KawaseDownsample
        => KawaseDownsamplePs.Value;

    /// <summary>
    ///   A pixel shader that applies a Dual Kawase upsampling pass to its input texture.
    ///   Use with <see cref="FsQuad"/> and <see cref="KawaseUniforms"/>.
    /// </summary>
    public static PixelShader KawaseUpsample
        => KawaseUpsamplePs.Value;

    /// <summary>
    ///   A pixel shader that applies a Dual Kawase upsampling + compositing pass to its input textures.
    ///   Use with <see cref="FsQuad"/> and <see cref="KawaseUniforms"/>.
    /// </summary>
    public static PixelShader KawaseUpsampleComposite
        => KawaseUpsampleCompositePs.Value;

    /// <summary>
    ///   A compute shader that casts rays refracted according to its input normal map and counts hits.
    ///   Use with <see cref="RefractionRaycastUniforms"/>.
    /// </summary>
    /// <remarks> This is implemented very naively and should only serve as a test/demo of Luna.DirectX. </remarks>
    public static ComputeShader RefractionRaycast
        => RefractionRaycastCs.Value;

    /// <summary>
    ///   A compute shader that calculates the highest value found in an input UInt texture, into a 1x1 output UInt texture.
    /// </summary>
    public static ComputeShader Max
        => MaxCs.Value;

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
