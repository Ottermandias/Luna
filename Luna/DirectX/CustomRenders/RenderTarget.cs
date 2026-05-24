using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Luna.DirectX;

/// <summary> A Direct3D render target. </summary>
internal unsafe struct RenderTarget : IDisposable
{
    /// <summary> The texture that holds the rendered pixels. </summary>
    public Texture2D Texture;

    /// <summary> A Direct3D render target view over this texture. </summary>
    public ComPtr<ID3D11RenderTargetView> RenderTargetView;

    /// <summary> Creates a new render target. </summary>
    /// <param name="device"> The Direct3D device to create the texture on. </param>
    /// <param name="width"> The width of the texture. </param>
    /// <param name="height"> The height of the texture. </param>
    /// <param name="format"> The format of the texture. </param>
    /// <param name="generateMips"> Whether to automatically generate mipmaps after rendering to that target. </param>
    public RenderTarget(ID3D11Device* device, uint width, uint height, DXGI_FORMAT format, bool generateMips = false)
    {
        var rtvDesc = new D3D11_RENDER_TARGET_VIEW_DESC
        {
            ViewDimension = D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_TEXTURE2D,
            Format        = format,
        };
        Texture = new Texture2D(device, width, height, format, D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET,
            generateMips ? D3D11_RESOURCE_MISC_FLAG.D3D11_RESOURCE_MISC_GENERATE_MIPS : 0, generateMips);
        try
        {
            Marshal.ThrowExceptionForHR(device->CreateRenderTargetView((ID3D11Resource*)Texture.Texture.Get(), &rtvDesc,
                RenderTargetView.GetAddressOf()));
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
        RenderTargetView.Dispose();
        Texture.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImTextureId(in RenderTarget renderTarget)
        => renderTarget.Texture;
}
