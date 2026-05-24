using System.Collections.Immutable;
using TerraFX.Interop.DirectX;

namespace Luna.DirectX;

/// <summary> A full-screen quad with a custom pixel shader, two constant buffers (resolution and custom), and textures. </summary>
/// <param name="pixelShader"> The pixel shader to use to render this quad. </param>
/// <param name="uniforms"> A constant buffer with custom data to pass to the pixel shader. </param>
/// <param name="outputFormats"> The output formats of this quad. </param>
/// <param name="description"> A description of this object, for debugging and logging purposes. </param>
public class FullScreenQuadWithUniformsAndTextures(
    PixelShader pixelShader,
    ConstantBufferBase? uniforms,
    ImmutableArray<DXGI_FORMAT> outputFormats,
    string? description)
    : FullScreenQuadWithUniforms(pixelShader, uniforms, outputFormats, description)
{
    /// <summary> The textures to pass to the pixel shader. </summary>
    public readonly List<TextureStandIn> Textures = new(D3D11.D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT);

    /// <summary> The samplers to pass to the pixel shader. </summary>
    public readonly List<Sampler?> Samplers = new(D3D11.D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT);

    /// <summary> Creates a new <see cref="FullScreenQuadWithUniformsAndTextures"/>. </summary>
    /// <param name="pixelShader"> The pixel shader to use to render this quad. </param>
    /// <param name="uniforms"> A constant buffer with custom data to pass to the pixel shader. </param>
    /// <param name="description"> A description of this object, for debugging and logging purposes. </param>
    public FullScreenQuadWithUniformsAndTextures(PixelShader pixelShader, ConstantBufferBase? uniforms, string? description)
        : this(pixelShader, uniforms, [DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM], description)
    { }

    /// <inheritdoc/>
    protected override unsafe void BindPixelShader(uint width, uint height, ID3D11DeviceContext* deviceContext)
    {
        base.BindPixelShader(width, height, deviceContext);
        // This is split in two separate functions so the stackallocs don't add up
        // (on top of each function being kinda logically independent).
        BindTextures(deviceContext);
        BindSamplers(deviceContext);
    }

    private unsafe void BindTextures(ID3D11DeviceContext* deviceContext)
    {
        var count = Textures.Count;
        if (count <= 0)
            return;

        if (count > D3D11.D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT)
            throw new InvalidOperationException(
                $"DxShaderEffect input count exceeds DirectX resource limit ({D3D11.D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT})");

        var views = stackalloc ID3D11ShaderResourceView*[count];
        for (var i = 0; i < count; ++i)
            views[i] = (ID3D11ShaderResourceView*)Textures[i].Id.Value;
        deviceContext->PSSetShaderResources(0, (uint)count, views);
    }

    private unsafe void BindSamplers(ID3D11DeviceContext* deviceContext)
    {
        var count = Samplers.Count;
        if (count <= 0)
            return;

        if (count > D3D11.D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT)
            throw new InvalidOperationException(
                $"DxShaderEffect sampler count exceeds DirectX resource limit ({D3D11.D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT})");

        var samplers = stackalloc ID3D11SamplerState*[count];
        for (var i = 0; i < count; ++i)
            samplers[i] = Samplers[i] is { } sampler ? sampler.GetOrCreateSampler() : null;
        deviceContext->PSSetSamplers(0, (uint)count, samplers);
    }
}
