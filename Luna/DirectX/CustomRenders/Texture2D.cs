using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Luna.DirectX;

internal unsafe struct Texture2D : IDisposable
{
    public ComPtr<ID3D11Texture2D>          Texture;
    public ComPtr<ID3D11ShaderResourceView> ShaderResourceView;

    public Texture2D(ID3D11Device* device, uint width, uint height, DXGI_FORMAT format, D3D11_BIND_FLAG bind, D3D11_RESOURCE_MISC_FLAG misc,
        bool withMips = false)
        : this(device, width, height, format, bind, misc, format, withMips)
    { }

    public Texture2D(ID3D11Device* device, uint width, uint height, DXGI_FORMAT format, D3D11_BIND_FLAG bind, D3D11_RESOURCE_MISC_FLAG misc,
        DXGI_FORMAT srvFormat, bool withMips = false)
    {
        var texDesc = new D3D11_TEXTURE2D_DESC
        {
            Width          = width,
            Height         = height,
            MipLevels      = withMips ? 0u : 1u,
            ArraySize      = 1,
            Format         = format,
            SampleDesc     = new DXGI_SAMPLE_DESC(1, 0),
            Usage          = D3D11_USAGE.D3D11_USAGE_DEFAULT,
            BindFlags      = (uint)(bind | D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE),
            CPUAccessFlags = 0,
            MiscFlags      = (uint)misc,
        };
        var srvDesc = new D3D11_SHADER_RESOURCE_VIEW_DESC
        {
            ViewDimension = D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE2D,
            Format        = srvFormat,
            Texture2D = new D3D11_TEX2D_SRV
            {
                MostDetailedMip = 0,
                MipLevels       = 1,
            },
        };

        Marshal.ThrowExceptionForHR(device->CreateTexture2D(&texDesc, null, Texture.GetAddressOf()));
        try
        {
            Marshal.ThrowExceptionForHR(
                device->CreateShaderResourceView((ID3D11Resource*)Texture.Get(), &srvDesc, ShaderResourceView.GetAddressOf()));
        }
        catch
        {
            Texture.Dispose();
            throw;
        }
    }

    public Texture2D(ID3D11Texture2D* texture, ID3D11ShaderResourceView* shaderResourceView)
    {
        Texture.Attach(texture);
        ShaderResourceView.Attach(shaderResourceView);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        ShaderResourceView.Dispose();
        Texture.Dispose();
    }

    public void GetDescription(out D3D11_TEXTURE2D_DESC desc)
        => DxUtility.GetDescription(Texture.Get(), out desc);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImTextureId(in Texture2D texture)
        => new((nint)texture.ShaderResourceView.Get());
}
