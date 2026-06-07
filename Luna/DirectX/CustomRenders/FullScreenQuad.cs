using System.Collections.Immutable;
using TerraFX.Interop.DirectX;

namespace Luna.DirectX;

/// <summary> A full-screen quad with a custom pixel shader, and a constant buffer with resolution and reciprocal resolution. </summary>
/// <param name="pixelShader"> The pixel shader to use to render this quad. </param>
/// <param name="outputFormats"> The output formats of this quad. </param>
/// <param name="description"> A description of this object, for debugging and logging purposes. </param>
public class FullScreenQuad(PixelShader pixelShader, ImmutableArray<DXGI_FORMAT> outputFormats, string? description)
    : ICustomRenderable, IDisposable
{
    /// <summary> A default output format for Standard Dynamic Range renders. </summary>
    public const DXGI_FORMAT DefaultOutputFormat = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM;

    /// <summary> A default output format for High Dynamic Range renders. </summary>
    public const DXGI_FORMAT DefaultHdrOutputFormat = DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT;

    /// <summary> The pixel shader to use to render this quad. </summary>
    protected PixelShader PixelShader = pixelShader;

    /// <summary> A description of this object, for debugging and logging purposes. </summary>
    protected string? Description = description;

    private uint _savedWidth;
    private uint _savedHeight;

    private ConstantBuffer<Vector4>? _resolutionBuffer;

    /// <inheritdoc/>
    public virtual int OutputCount
        => outputFormats.Length;

    /// <inheritdoc/>
    public virtual int KeepAliveDuration
        => 1;

    /// <inheritdoc/>
    public virtual long Version
        => 0;

    /// <inheritdoc/>
    public virtual ITargetClearStrategy? ClearStrategy
        => ITargetClearStrategy.Simple;

    /// <inheritdoc/>
    public virtual D3D11_DEPTH_STENCIL_DESC DepthStencilState
        => ICustomRenderable.DefaultDepthStencilState;

    /// <inheritdoc/>
    public virtual D3D11_RASTERIZER_DESC RasterizerState
        => ICustomRenderable.DefaultRasterizerState;

    /// <summary> Creates a new <see cref="FullScreenQuad"/>. </summary>
    /// <param name="pixelShader"> The pixel shader to use to render this quad. </param>
    /// <param name="description"> A description of this object, for debugging and logging purposes. </param>
    public FullScreenQuad(PixelShader pixelShader, string? description)
        : this(pixelShader, [DefaultOutputFormat], description)
    { }

    ~FullScreenQuad()
        => Dispose(false);

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary> Releases the resources used by this object. </summary>
    /// <param name="disposing"> True if called explicitly, false if garbage collected. </param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
            _resolutionBuffer?.Dispose();
    }

    /// <inheritdoc/>
    public override string? ToString()
        => Description ?? base.ToString();

    /// <inheritdoc/>
    public virtual DXGI_FORMAT GetOutputFormat(int outputIndex)
        => outputFormats[outputIndex];

    /// <summary> Gets the constant buffer for the output resolution, creating or updating it if necessary. </summary>
    /// <param name="width"> The output width. </param>
    /// <param name="height"> The output height. </param>
    /// <returns> The constant buffer. </returns>
    protected ConstantBuffer<Vector4> GetOrCreateResolutionBuffer(uint width, uint height)
    {
        if (_resolutionBuffer is not null)
        {
            if (width != _savedWidth || height != _savedHeight)
            {
                _resolutionBuffer.Contents = CalculateResolutionVector(width, height);
                _resolutionBuffer.SetDirty();
                _savedWidth  = width;
                _savedHeight = height;
            }

            return _resolutionBuffer;
        }

        _resolutionBuffer = new ConstantBuffer<Vector4>(CalculateResolutionVector(width, height));

        _savedWidth  = width;
        _savedHeight = height;
        return _resolutionBuffer;
    }

    private static Vector4 CalculateResolutionVector(uint width, uint height)
        => new(width, height, 1.0f / width, 1.0f / height);

    /// <inheritdoc/>
    public virtual unsafe void Render(uint width, uint height, ID3D11DeviceContext* deviceContext)
    {
        // Bind the vertex shader (see FsQuad_vs.hlsl), no geometry/hull/domain shaders, and the pixel shader supplied by inheritors or callers.
        // The vertex shader takes no cbuffers, resources or samplers.
        deviceContext->VSSetShader(LunaShaders.FsQuad.GetOrCreateShader(), null, 0);
        deviceContext->HSSetShader(null, null, 0);
        deviceContext->DSSetShader(null, null, 0);
        deviceContext->GSSetShader(null, null, 0);
        BindPixelShader(width, height, deviceContext);

        // The vertex shader takes no inputs except the vertex ID, which is managed by the system.
        // We are drawing a triangle strip of 4 vertices starting at 0:
        // - The vertex shader will get called with SV_VertexID = 0, 1, 2 and 3, and no other input.
        // - One triangle will be assembled with the output of 0, 1 and 2, another with the outputs of 1, 2 and 3.
        // - The two resulting triangles will be shaded as usual, using the inheritor/caller's pixel shader.
        deviceContext->IASetInputLayout(null);
        deviceContext->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY.D3D11_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP);
        deviceContext->IASetVertexBuffers(0, 0, null, null, null);
        deviceContext->IASetIndexBuffer(null, DXGI_FORMAT.DXGI_FORMAT_UNKNOWN, 0);
        deviceContext->Draw(4, 0);
    }

    /// <summary> Binds the pixel shader and its inputs to the given device context. </summary>
    /// <param name="width"> The output width. </param>
    /// <param name="height"> The output height. </param>
    /// <param name="deviceContext"> The device context to run commands on. </param>
    protected virtual unsafe void BindPixelShader(uint width, uint height, ID3D11DeviceContext* deviceContext)
    {
        // This default implementation binds the pixel shader, with the resolution cbuffer at slot 0 (see FsQuad.hlsli).
        deviceContext->PSSetShader(PixelShader.GetOrCreateShader(), null, 0);
        var resolutionBuffer = GetOrCreateResolutionBuffer(width, height).GetOrCreateBuffer(deviceContext);
        deviceContext->PSSetConstantBuffers(0, 1, &resolutionBuffer);
    }
}
