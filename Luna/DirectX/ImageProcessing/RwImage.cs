using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;
using TerraFX.Interop.DirectX;

namespace Luna.DirectX;

/// <summary> A read/write image manipulated by the GPU using DirectX. </summary>
/// <remarks> The pixel data of this image can be retrieved using <see cref="ITextureReadbackProvider"/>. </remarks>
public unsafe class RwImage : IDalamudTextureWrap, IUnorderedAccessViewWrap
{
    private RwTexture2D _texture;

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
            return ((int)desc.Width, (int)desc.Height);
            var desc = GetDescription();
        }
    }

    /// <summary> Gets the format of this image. </summary>
    public DXGI_FORMAT Format
    {
        get
        {
            var desc = GetDescription();
            return desc.Format;
        }
    }

    /// <summary> Gets the underlying Direct3D texture object. </summary>
    public ID3D11Texture2D* Texture
        => _texture.Texture.Texture.Get();

    /// <summary> Gets the underlying Direct3D shader resource view object. </summary>
    public ID3D11ShaderResourceView* ShaderResourceView
        => _texture.Texture.ShaderResourceView.Get();

    /// <summary> Gets the underlying Direct3D unordered access view object. </summary>
    public ID3D11UnorderedAccessView* UnorderedAccessView
        => _texture.UnorderedAccessView.Get();

    #region IDalamudTextureWrap implementation

    ImTextureID IDalamudTextureWrap.Handle
        => new(_texture.Texture.ShaderResourceView);

    int IDalamudTextureWrap.Width
    {
        get
        {
            var desc = GetDescription();
            return (int)desc.Width;
        }
    }

    int IDalamudTextureWrap.Height
    {
        get
        {
            var desc = GetDescription();
            return (int)desc.Height;
        }
    }

    Vector2 IDalamudTextureWrap.Size
    {
        get
        {
            var desc = GetDescription();
            return new Vector2(desc.Width, desc.Height);
        }
    }

    #endregion

    #region IUnorderedAccessViewWrap implementation

    ImTextureId IUnorderedAccessViewWrap.Id
        => _texture;

    nint IUnorderedAccessViewWrap.Handle
        => (nint)_texture.UnorderedAccessView.Get();

    uint IUnorderedAccessViewWrap.InitialOffset
        => uint.MaxValue;

    #endregion

    /// <summary> Constructs a new read/write image of the given specifications. </summary>
    /// <param name="width"> The width of the image. </param>
    /// <param name="height"> The height of the image. </param>
    /// <param name="format"> The format of the image. </param>
    public RwImage(uint width, uint height, DXGI_FORMAT format)
        => _texture = new RwTexture2D(CustomRenderManager.Instance.Device, width, height, format);

    /// <summary> Constructs a new <see cref="RwImage"/> wrapping the given DirectX texture. </summary>
    /// <param name="texture"> The texture to wrap. </param>
    /// <param name="srv"> An existing shader resource view of <paramref name="texture"/>. </param>
    /// <param name="uav"> An existing unordered access view of <paramref name="texture"/>. </param>
    /// <param name="addRefTexture"> Whether to increment the reference count of <paramref name="texture"/>. </param>
    /// <param name="addRefSrv"> Whether to increment the reference count of <paramref name="srv"/>. </param>
    /// <param name="addRefUav"> Whether to increment the reference count of <paramref name="uav"/>. </param>
    /// <remarks> Passing views over a resource that isn't the given texture will cause undefined behavior. </remarks>
    public RwImage(ID3D11Texture2D* texture, ID3D11ShaderResourceView* srv, ID3D11UnorderedAccessView* uav, bool addRefTexture = true,
        bool addRefSrv = true, bool addRefUav = true)
    {
        if (addRefTexture)
            texture->AddRef();
        if (addRefSrv)
            srv->AddRef();
        if (addRefUav)
            uav->AddRef();

        _texture.Texture = new Texture2D(texture, srv);
        _texture.UnorderedAccessView.Attach(uav);
    }

    /// <summary> Constructs a new <see cref="RwImage"/> wrapping the same DirectX texture as the given object. </summary>
    /// <param name="other"> An existing object wrapping the wanted texture. </param>
    public RwImage(RwImage other)
        : this(in other._texture)
    { }

    /// <summary> Constructs a new <see cref="RwImage"/> wrapping the given DirectX texture. </summary>
    /// <param name="texture"> The texture to wrap. </param>
    /// <param name="addRef"> Whether to increment the reference counts of <paramref name="texture"/>. </param>
    internal RwImage(in RwTexture2D texture, bool addRef = true)
    {
        if (addRef)
        {
            texture.Texture.Texture.Get()->AddRef();
            texture.Texture.ShaderResourceView.Get()->AddRef();
            texture.UnorderedAccessView.Get()->AddRef();
        }

        _texture = texture;
    }

    private RwImage()
    { }

    ~RwImage()
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

    /// <summary> Creates an uninitialized <see cref="RwImage"/>. It must be initialized with <see cref="Recreate"/> before use. </summary>
    /// <returns> An uninitialized read/write image. </returns>
    /// <remarks> Any other use of the returned image before calling <see cref="Recreate"/> on it will cause undefined behavior. </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RwImage UnsafeCreateUninitialized()
        => new();

    /// <summary>
    ///   Recreates this read/write image with new specifications.
    ///   If other objects were created from this one, the link will be broken, and they will continue wrapping the old image.
    /// </summary>
    /// <param name="width"> The new width of the image. </param>
    /// <param name="height"> The new height of the image. </param>
    /// <param name="format"> The new format of the image. </param>
    public void Recreate(uint width, uint height, DXGI_FORMAT format)
    {
        _texture.Dispose();
        _texture = new RwTexture2D(CustomRenderManager.Instance.Device, width, height, format);
    }

    /// <summary> Creates an <see cref="Image"/> wrapping the same texture as this object. </summary>
    /// <returns> This object, as an <see cref="Image"/>. </returns>
    public Image ToImage()
        => new(in _texture.Texture);

    public static implicit operator ImTextureId(RwImage image)
        => image._texture;

    private D3D11_TEXTURE2D_DESC GetDescription()
    {
        ObjectDisposedException.ThrowIf(!_texture.Texture.Texture.Valid, this);
        return _texture.Texture.GetDescription();
    }
}
