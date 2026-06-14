using TerraFX.Interop.DirectX;

namespace Luna.DirectX;

/// <summary> Encapsulates a depth-stencil buffer and a set of render targets. </summary>
public unsafe interface IRenderTargetProvider
{
    /// <summary> Clears the depth-stencil buffer and the render targets according to a given strategy. </summary>
    /// <param name="strategy"> The clear strategy to use. </param>
    /// <param name="deviceContext"> The Direct3D device context to issue clear commands on. </param>
    public void Clear(ITargetClearStrategy strategy, ID3D11DeviceContext* deviceContext);

    /// <summary> Binds the depth-stencil buffer and the render targets to a Direct3D device context. </summary>
    /// <param name="deviceContext"> The Direct3D device context to bind the targets to. </param>
    public void Bind(ID3D11DeviceContext* deviceContext);

    /// <summary> Performs implementation-defined processing on the render targets after rendering. </summary>
    /// <param name="deviceContext"> The Direct3D device context to perform the processing in. </param>
    public void PostProcess(ID3D11DeviceContext* deviceContext);
}
