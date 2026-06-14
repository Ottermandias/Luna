using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Luna.DirectX;

/// <summary> A Direct3D depth-stencil buffer. </summary>
internal unsafe struct DepthStencil : IDisposable
{
    /// <summary> The texture that holds the depth-stencil data. </summary>
    public Texture2D Texture;

    /// <summary> A Direct3D depth-stencil view over this buffer. </summary>
    public ComPtr<ID3D11DepthStencilView> DepthStencilView;

    /// <summary> Creates a new depth-stencil buffer. </summary>
    /// <param name="device"> The Direct3D device to create the buffer on. </param>
    /// <param name="dimensions"> The dimensions of the buffer. </param>
    public DepthStencil(ID3D11Device* device, Dimensions dimensions)
    {
        var dsvDesc = new D3D11_DEPTH_STENCIL_VIEW_DESC
        {
            ViewDimension = D3D11_DSV_DIMENSION.D3D11_DSV_DIMENSION_TEXTURE2D,
            Format        = DXGI_FORMAT.DXGI_FORMAT_D32_FLOAT,
        };
        Texture = new Texture2D(device, dimensions, DXGI_FORMAT.DXGI_FORMAT_R32_TYPELESS, D3D11_BIND_FLAG.D3D11_BIND_DEPTH_STENCIL, 0,
            DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT);
        try
        {
            Marshal.ThrowExceptionForHR(device->CreateDepthStencilView((ID3D11Resource*)Texture.Texture.Get(), &dsvDesc,
                DepthStencilView.GetAddressOf()));
        }
        catch
        {
            Texture.Dispose();
            throw;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        DepthStencilView.Dispose();
        Texture.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImTextureId(in DepthStencil depthStencil)
        => depthStencil.Texture;
}
