using TerraFX.Interop.DirectX;

namespace Luna.DirectX;

/// <summary> Represents a strategy to use to clear depth-stencil buffers and render targets before rendering. </summary>
public interface ITargetClearStrategy
{
    /// <summary> A clear strategy that clears all depth buffers with 1, all stencil buffers with 0, and all render targets with transparent pixels. </summary>
    public static readonly ITargetClearStrategy Simple = new SimpleTargetClearStrategy(1.0f, 0, Vector4.Zero, (0u, 0u, 0u, 0u));

    /// <summary> Clears the given depth-stencil buffer. </summary>
    /// <param name="deviceContext"> The device context on which to run operations. </param>
    /// <param name="depthStencilView"> The depth-stencil buffer. </param>
    public unsafe void ClearDepthStencil(ID3D11DeviceContext* deviceContext, ID3D11DepthStencilView* depthStencilView);

    /// <summary> Clears the given render target. </summary>
    /// <param name="deviceContext"> The device context on which to run operations. </param>
    /// <param name="outputIndex"> The render target index. </param>
    /// <param name="renderTargetView"> The render target. </param>
    public unsafe void ClearRenderTarget(ID3D11DeviceContext* deviceContext, int outputIndex, ID3D11RenderTargetView* renderTargetView);

    /// <summary> Clears the given unordered access view. </summary>
    /// <param name="deviceContext"> The device context on which to run operations. </param>
    /// <param name="outputIndex"> The unordered access view index. </param>
    /// <param name="unorderedAccessView"> The unordered access view. </param>
    public unsafe void ClearUnorderedAccessView(ID3D11DeviceContext* deviceContext, int outputIndex,
        ID3D11UnorderedAccessView* unorderedAccessView);
}
