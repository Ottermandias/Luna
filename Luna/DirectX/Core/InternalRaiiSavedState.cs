using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Luna.DirectX;

/// <summary> A saved rasterizer state, that will be restored on disposal. </summary>
internal unsafe ref struct SavedRasterizerState
{
    private readonly ID3D11DeviceContext*          _deviceContext;
    private          ComPtr<ID3D11RasterizerState> _rsState;

    /// <summary> Saves the current rasterizer state of the given device context, for restoration on disposal. </summary>
    /// <param name="deviceContext"> The device context of which to save the rasterizer state. </param>
    public SavedRasterizerState(ID3D11DeviceContext* deviceContext)
    {
        _deviceContext = deviceContext;
        deviceContext->RSGetState(_rsState.GetAddressOf());
    }

    /// <summary> Restores the saved rasterizer state. </summary>
    public void Dispose()
    {
        _deviceContext->RSSetState(_rsState);
        _rsState.Dispose();
    }
}

/// <summary> A saved render target state, that will be restored on disposal. </summary>
internal unsafe ref struct SavedRenderTargetViews
{
    // Poor man's static_assert. Ensures at compile time that we have the right number of saved render targets.
    private const uint _0 = D3D11.D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT == 8 ? 0 : -666;

    private readonly ID3D11DeviceContext*           _deviceContext;
    private          ComPtr<ID3D11RenderTargetView> _rtView0;
    private          ComPtr<ID3D11RenderTargetView> _rtView1;
    private          ComPtr<ID3D11RenderTargetView> _rtView2;
    private          ComPtr<ID3D11RenderTargetView> _rtView3;
    private          ComPtr<ID3D11RenderTargetView> _rtView4;
    private          ComPtr<ID3D11RenderTargetView> _rtView5;
    private          ComPtr<ID3D11RenderTargetView> _rtView6;
    private          ComPtr<ID3D11RenderTargetView> _rtView7;
    private          ComPtr<ID3D11DepthStencilView> _dsView;

    /// <summary> Saves the current render target state of the given device context, for restoration on disposal. </summary>
    /// <param name="deviceContext"> The device context of which to save the render target state. </param>
    public SavedRenderTargetViews(ID3D11DeviceContext* deviceContext)
    {
        _deviceContext = deviceContext;
        deviceContext->OMGetRenderTargets(8, _rtView0.GetAddressOf(), _dsView.GetAddressOf());
    }

    /// <summary> Restores the saved render target state. </summary>
    public void Dispose()
    {
        _deviceContext->OMSetRenderTargets(8, _rtView0.GetAddressOf(), _dsView);

        _rtView0.Dispose();
        _rtView1.Dispose();
        _rtView2.Dispose();
        _rtView3.Dispose();
        _rtView4.Dispose();
        _rtView5.Dispose();
        _rtView6.Dispose();
        _rtView7.Dispose();
        _dsView.Dispose();
    }
}

/// <summary> A saved depth-stencil state, that will be restored on disposal. </summary>
internal unsafe ref struct SavedDepthStencilState
{
    private readonly ID3D11DeviceContext*            _deviceContext;
    private          ComPtr<ID3D11DepthStencilState> _dsState;
    private          uint                            _stencilRef;

    /// <summary> Saves the current depth-stencil state of the given device context, for restoration on disposal. </summary>
    /// <param name="deviceContext"> The device context of which to save the depth-stencil state. </param>
    public SavedDepthStencilState(ID3D11DeviceContext* deviceContext)
    {
        _deviceContext = deviceContext;
        fixed (SavedDepthStencilState* pThis = &this)
        {
            deviceContext->OMGetDepthStencilState(_dsState.GetAddressOf(), &pThis->_stencilRef);
        }
    }

    /// <summary> Restores the saved depth-stencil state. </summary>
    public void Dispose()
    {
        _deviceContext->OMSetDepthStencilState(_dsState, _stencilRef);
        _dsState.Dispose();
    }
}
