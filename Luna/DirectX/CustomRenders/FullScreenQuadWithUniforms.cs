using System.Collections.Immutable;
using TerraFX.Interop.DirectX;

namespace Luna.DirectX;

/// <summary> A full-screen quad with a custom pixel shader and two constant buffers, one with resolution and reciprocal resolution, and a custom one. </summary>
/// <param name="pixelShader"> The pixel shader to use to render this quad. </param>
/// <param name="uniforms"> A constant buffer with custom data to pass to the pixel shader. </param>
/// <param name="outputFormats"> The output formats of this quad. </param>
/// <param name="description"> A description of this object, for debugging and logging purposes. </param>
public class FullScreenQuadWithUniforms(
    PixelShader pixelShader,
    Buffer? uniforms,
    ImmutableArray<DXGI_FORMAT> outputFormats,
    string? description)
    : FullScreenQuad(pixelShader, outputFormats, description)
{
    private long _version = 0;

    /// <summary> The custom pixel shader input data. </summary>
    public Buffer? Uniforms
        => uniforms;

    /// <inheritdoc/>
    public override long Version
        => _version;

    /// <summary> Creates a new <see cref="FullScreenQuadWithUniforms"/>. </summary>
    /// <param name="pixelShader"> The pixel shader to use to render this quad. </param>
    /// <param name="uniforms"> A constant buffer with custom data to pass to the pixel shader. </param>
    /// <param name="description"> A description of this object, for debugging and logging purposes. </param>
    public FullScreenQuadWithUniforms(PixelShader pixelShader, Buffer? uniforms, string? description)
        : this(pixelShader, uniforms, [DefaultOutputFormat], description)
    { }

    /// <summary> Increments this object's version, invalidating all cached renders. Use after modifying the input data. </summary>
    public void Update()
    {
        uniforms?.SetDirty();
        ++_version;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            uniforms?.Dispose();
        base.Dispose(disposing);
    }

    /// <inheritdoc/>
    protected override unsafe void BindPixelShader(uint width, uint height, ID3D11DeviceContext* deviceContext)
    {
        // Call the default implementation, then bind the uniforms cbuffer at slot 1.
        base.BindPixelShader(width, height, deviceContext);
        var uniformsBuffer = uniforms is not null ? uniforms.GetOrCreateBuffer(deviceContext) : null;
        deviceContext->PSSetConstantBuffers(1, 1, &uniformsBuffer);
    }
}
