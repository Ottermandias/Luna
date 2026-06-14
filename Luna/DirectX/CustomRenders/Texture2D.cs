using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Luna.DirectX;

/// <summary> A Direct3D 2D texture. </summary>
internal unsafe struct Texture2D : IDisposable
{
    /// <summary> The texture. </summary>
    public ComPtr<ID3D11Texture2D> Texture;

    /// <summary> A view of the texture, for use as a shader resource. </summary>
    public ComPtr<ID3D11ShaderResourceView> ShaderResourceView;

    /// <summary> Creates a new <see cref="Texture2D"/>. </summary>
    /// <param name="device"> The device to create the texture on. </param>
    /// <param name="width"> The desired width. </param>
    /// <param name="height"> The desired height. </param>
    /// <param name="format"> The desired format. </param>
    /// <param name="bind"> How this texture will be bound. </param>
    /// <param name="misc"> Miscellaneous flags for the texture. </param>
    /// <param name="withMips"> Whether to create a full mipmap chain. </param>
    public Texture2D(ID3D11Device* device, uint width, uint height, DXGI_FORMAT format, D3D11_BIND_FLAG bind, D3D11_RESOURCE_MISC_FLAG misc,
        bool withMips = false)
        : this(device, width, height, format, bind, misc, format, withMips)
    { }

    /// <summary> Creates a new <see cref="Texture2D"/>. </summary>
    /// <param name="device"> The device to create the texture on. </param>
    /// <param name="width"> The desired width. </param>
    /// <param name="height"> The desired height. </param>
    /// <param name="format"> The desired format. </param>
    /// <param name="bind"> How this texture will be bound. </param>
    /// <param name="misc"> Miscellaneous flags for the texture. </param>
    /// <param name="srvFormat"> The format for the shader resource view. </param>
    /// <param name="withMips"> Whether to create a full mipmap chain. </param>
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

    /// <summary> Wraps an existing texture in a <see cref="Texture2D"/>. Reference counts won't be incremented. </summary>
    /// <param name="texture"> The texture. </param>
    /// <param name="shaderResourceView"> The shader resource view. </param>
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

    /// <summary> Gets the specifications of this texture. </summary>
    /// <returns> The specifications. </returns>
    public D3D11_TEXTURE2D_DESC GetDescription()
        => DxUtility.GetDescription(Texture.Get());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImTextureId(in Texture2D texture)
        => new((nint)texture.ShaderResourceView.Get());
}
