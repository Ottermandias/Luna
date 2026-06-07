using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Luna.DirectX;

/// <summary> A Direct3D read/write 2D texture. </summary>
internal unsafe struct RwTexture2D : IDisposable
{
    /// <summary> The texture. </summary>
    public Texture2D Texture;

    /// <summary> A Direct3D unordered access view over this texture. </summary>
    public ComPtr<ID3D11UnorderedAccessView> UnorderedAccessView;

    /// <summary> Creates a new <see cref="RwTexture2D"/>. </summary>
    /// <param name="device"> The Direct3D device to create the texture on. </param>
    /// <param name="width"> The width of the texture. </param>
    /// <param name="height"> The height of the texture. </param>
    /// <param name="format"> The format of the texture. </param>
    /// <param name="generateMips"> Whether to automatically generate mipmaps after writing to that texture. </param>
    public RwTexture2D(ID3D11Device* device, uint width, uint height, DXGI_FORMAT format, bool generateMips = false)
    {
        var uavDesc = new D3D11_UNORDERED_ACCESS_VIEW_DESC
        {
            ViewDimension = D3D11_UAV_DIMENSION.D3D11_UAV_DIMENSION_TEXTURE2D,
            Format        = format,
        };
        Texture = new Texture2D(device, width, height, format, D3D11_BIND_FLAG.D3D11_BIND_UNORDERED_ACCESS,
            generateMips ? D3D11_RESOURCE_MISC_FLAG.D3D11_RESOURCE_MISC_GENERATE_MIPS : 0, generateMips);
        try
        {
            Marshal.ThrowExceptionForHR(device->CreateUnorderedAccessView((ID3D11Resource*)Texture.Texture.Get(), &uavDesc,
                UnorderedAccessView.GetAddressOf()));
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
        UnorderedAccessView.Dispose();
        Texture.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImTextureId(in RwTexture2D texture)
        => texture.Texture;
}
