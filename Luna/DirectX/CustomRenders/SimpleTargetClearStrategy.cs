using TerraFX.Interop.DirectX;

namespace Luna.DirectX;

/// <summary> A target clear strategy that clears all the buffers with constant values. </summary>
/// <param name="depth"> The depth value. </param>
/// <param name="stencil"> The stencil value. </param>
/// <param name="color"> The color value. </param>
public sealed class SimpleTargetClearStrategy(float depth, byte stencil, Vector4 color) : ITargetClearStrategy
{
    private readonly Vector4 _color = color;

    /// <inheritdoc/>
    public unsafe void ClearDepthStencil(ID3D11DeviceContext* deviceContext, ID3D11DepthStencilView* depthStencilView)
        => deviceContext->ClearDepthStencilView(depthStencilView,
            (uint)(D3D11_CLEAR_FLAG.D3D11_CLEAR_DEPTH | D3D11_CLEAR_FLAG.D3D11_CLEAR_STENCIL), depth, stencil);

    /// <inheritdoc/>
    public unsafe void ClearRenderTarget(ID3D11DeviceContext* deviceContext, int outputIndex, ID3D11RenderTargetView* renderTargetView)
    {
        fixed (Vector4* pColor = &_color)
            deviceContext->ClearRenderTargetView(renderTargetView, (float*)pColor);
    }
}
