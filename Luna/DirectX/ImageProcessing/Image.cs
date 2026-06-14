using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Luna.DirectX;

/// <summary> An image loaded on the GPU using DirectX. </summary>
/// <remarks> The pixel data of this image can be retrieved using <see cref="ITextureReadbackProvider"/>. </remarks>
public unsafe class Image : IDalamudTextureWrap
{
    private Texture2D _texture;

    /// <summary> Gets a texture handle suitable for direct use with ImGui functions. </summary>
    public ImTextureId Handle
        => _texture;

    /// <summary> Gets the description (dimensions and format) of this image as a Dalamud <see cref="RawImageSpecification"/>. </summary>
    public RawImageSpecification ImageSpecification
    {
        get
        {
            var desc = GetDescription();
            return new RawImageSpecification((int)desc.Width, (int)desc.Height, (int)desc.Format);
        }
    }

    /// <summary> Gets the dimensions (width and height) of this image. </summary>
    public (int Width, int Height) Dimensions
    {
        get
        {
            var desc = GetDescription();
            return ((int)desc.Width, (int)desc.Height);
        }
    }

    /// <summary> Gets the format of this image. </summary>
    public DXGI_FORMAT Format
        => GetDescription().Format;

    /// <summary> Gets the underlying Direct3D texture object. </summary>
    public ID3D11Texture2D* Texture
        => _texture.Texture.Get();

    /// <summary> Gets the underlying Direct3D shader resource view object. </summary>
    public ID3D11ShaderResourceView* View
        => _texture.ShaderResourceView.Get();

    #region IDalamudTextureWrap implementation

    ImTextureID IDalamudTextureWrap.Handle
        => new(_texture.ShaderResourceView);

    int IDalamudTextureWrap.Width
        => (int)GetDescription().Width;

    int IDalamudTextureWrap.Height
        => (int)GetDescription().Height;

    Vector2 IDalamudTextureWrap.Size
    {
        get
        {
            var desc = GetDescription();
            return new Vector2(desc.Width, desc.Height);
        }
    }

    #endregion

    /// <summary> Constructs a new <see cref="Image"/> wrapping the given DirectX texture. A shader resource view will be created automatically. </summary>
    /// <param name="texture"> The texture to wrap. </param>
    /// <param name="addRef"> Whether to increment the reference count of <paramref name="texture"/>. </param>
    public Image(ID3D11Texture2D* texture, bool addRef = true)
        : this(texture, CreateView(texture), addRef, false)
    { }

    /// <summary> Constructs a new <see cref="Image"/> wrapping the texture of the given DirectX shader resource view. </summary>
    /// <param name="view"> A shader resource view of the texture to wrap. </param>
    /// <param name="addRef"> Whether to increment the reference count of <paramref name="view"/>. </param>
    /// <remarks> Passing a view over a resource that isn't a 2D texture will throw an exception. </remarks>
    public Image(ID3D11ShaderResourceView* view, bool addRef = true)
        : this(GetTexture(view), view, false, addRef)
    { }

    /// <summary> Constructs a new <see cref="Image"/> wrapping the given ImGui texture. </summary>
    /// <param name="id"> An ImGui texture ID. </param>
    public Image(ImTextureId id)
        : this((ID3D11ShaderResourceView*)id.Value)
    { }

    /// <summary> Constructs a new <see cref="Image"/> wrapping the given DirectX texture. </summary>
    /// <param name="texture"> The texture to wrap. </param>
    /// <param name="view"> An existing shader resource view of <paramref name="texture"/>. </param>
    /// <param name="addRefTexture"> Whether to increment the reference count of <paramref name="texture"/>. </param>
    /// <param name="addRefView"> Whether to increment the reference count of <paramref name="view"/>. </param>
    /// <remarks> Passing a view over a resource that isn't the given texture will cause undefined behavior. </remarks>
    public Image(ID3D11Texture2D* texture, ID3D11ShaderResourceView* view, bool addRefTexture = true, bool addRefView = true)
    {
        if (addRefTexture)
            texture->AddRef();
        if (addRefView)
            view->AddRef();

        _texture = new Texture2D(texture, view);
    }

    /// <summary> Constructs a new <see cref="Image"/> wrapping the same DirectX texture as the given object. </summary>
    /// <param name="wrap"> An existing object wrapping the wanted texture. </param>
    public Image(IDalamudTextureWrap wrap)
        : this(wrap is Image image ? image._texture.Texture : GetTexture((ID3D11ShaderResourceView*)(nuint)wrap.Handle.Handle),
            (ID3D11ShaderResourceView*)(nuint)wrap.Handle.Handle, wrap is Image)
    { }

    /// <summary> Constructs a new <see cref="Image"/> wrapping the given DirectX texture. </summary>
    /// <param name="texture"> The texture to wrap. </param>
    /// <param name="addRef"> Whether to increment the reference counts of <paramref name="texture"/>. </param>
    internal Image(in Texture2D texture, bool addRef = true)
    {
        if (addRef)
        {
            texture.Texture.Get()->AddRef();
            texture.ShaderResourceView.Get()->AddRef();
        }

        _texture = texture;
    }

    ~Image()
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
        => _texture.Dispose();

    public static implicit operator ImTextureId(Image image)
        => image._texture;

    private D3D11_TEXTURE2D_DESC GetDescription()
    {
        ObjectDisposedException.ThrowIf(!_texture.Texture.Valid, this);
        return _texture.GetDescription();
    }

    private static ID3D11ShaderResourceView* CreateView(ID3D11Texture2D* texture)
    {
        var texDesc = DxUtility.GetDescription(texture);
        var srvDesc = new D3D11_SHADER_RESOURCE_VIEW_DESC
        {
            ViewDimension = D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE2D,
            Format        = texDesc.Format,
            Texture2D = new D3D11_TEX2D_SRV
            {
                MostDetailedMip = 0,
                MipLevels       = texDesc.MipLevels - 1,
            },
        };

        using var device = new ComPtr<ID3D11Device>();
        texture->GetDevice(device.GetAddressOf());
        ID3D11ShaderResourceView* view;
        Marshal.ThrowExceptionForHR(device.Get()->CreateShaderResourceView((ID3D11Resource*)texture, &srvDesc, &view));
        return view;
    }

    private static ID3D11Texture2D* GetTexture(ID3D11ShaderResourceView* view)
    {
        using var resource = new ComPtr<ID3D11Resource>();
        view->GetResource(resource.GetAddressOf());
        var texture = new ComPtr<ID3D11Texture2D>();
        Marshal.ThrowExceptionForHR(resource.As(ref texture));
        return texture;
    }
}
