using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using Luna.DirectX;
using TerraFX.Interop.DirectX;

namespace Luna;

/// <summary> A safe handle that wraps a game <see cref="Texture"/> object. </summary>
public unsafe class SafeTextureHandle : SafeHandle, ICloneable, IDalamudTextureWrap
{
    /// <summary> Gets the wrapped texture. </summary>
    public Texture* Texture
        => (Texture*)handle;

    /// <inheritdoc/>
    public override bool IsInvalid
        => handle == 0;

    /// <summary> Gets the dimensions (width and height) of this texture. </summary>
    public (int Width, int Height) Dimensions
        => Texture switch
        {
            null        => (0, 0),
            var texture => ((int)texture->AllocatedWidth, (int)texture->AllocatedHeight),
        };

    #region IDalamudTextureWrap implementation

    ImTextureID IDalamudTextureWrap.Handle
        => new(Texture switch
        {
            null        => 0,
            var texture => (nint)texture->D3D11ShaderResourceView,
        });

    int IDalamudTextureWrap.Width
        => Texture switch
        {
            null        => 0,
            var texture => (int)texture->AllocatedWidth,
        };

    int IDalamudTextureWrap.Height
        => Texture switch
        {
            null        => 0,
            var texture => (int)texture->AllocatedHeight,
        };

    Vector2 IDalamudTextureWrap.Size
        => Texture switch
        {
            null        => Vector2.Zero,
            var texture => new Vector2(texture->AllocatedWidth, texture->AllocatedHeight),
        };

    #endregion

    /// <summary> Constructs a new <see cref="SafeTextureHandle"/> wrapping the given texture. </summary>
    /// <param name="handle"> The game texture to wrap. </param>
    /// <param name="incRef"> Whether to increment the reference count of <paramref name="handle"/>. </param>
    /// <param name="ownsHandle"> Whether to decrement the reference count of <paramref name="handle"/> on disposal. </param>
    /// <exception cref="ArgumentException"> <paramref name="incRef"/> is <c>true</c> but <paramref name="ownsHandle"/> is false. </exception>
    public SafeTextureHandle(Texture* handle, bool incRef = true, bool ownsHandle = true)
        : base(0, ownsHandle)
    {
        if (incRef && !ownsHandle)
            throw new ArgumentException("Non-owning SafeTextureHandle with IncRef is unsupported");

        if (incRef && handle != null)
            handle->IncRef();
        SetHandle((nint)handle);
    }

    /// <summary> Creates a new <see cref="SafeTextureHandle"/> that wraps the same texture as this one. </summary>
    /// <returns> A new <see cref="SafeTextureHandle"/> that wraps the same texture as this one. </returns>
    public SafeTextureHandle Clone()
        => new(Texture);

    object ICloneable.Clone()
        => Clone();

    /// <summary> Exchanges the wrapped texture with the one at the given location. </summary>
    /// <param name="ppTexture"> The location with which to exchange textures. </param>
    public void Exchange(ref nint ppTexture)
    {
        lock (this)
        {
            handle = Interlocked.Exchange(ref ppTexture, handle);
        }
    }

    /// <summary> Creates an <see cref="Image"/> wrapping the same texture as this object. </summary>
    /// <returns> This object, as an <see cref="Image"/>. </returns>
    /// <exception cref="ObjectDisposedException"> This object is disposed or invalid. </exception>
    public Image ToImage()
        => Texture switch
        {
            null        => throw new ObjectDisposedException(GetType().FullName),
            var texture => new Image((ID3D11Texture2D*)texture->D3D11Texture2D, (ID3D11ShaderResourceView*)texture->D3D11ShaderResourceView),
        };

    /// <summary> Creates an invalid <see cref="SafeTextureHandle"/>. </summary>
    /// <returns> An invalid <see cref="SafeTextureHandle"/>. </returns>
    public static SafeTextureHandle CreateInvalid()
        => new(null, false);

    /// <inheritdoc/>
    protected override bool ReleaseHandle()
    {
        nint handle;
        lock (this)
        {
            handle      = this.handle;
            this.handle = 0;
        }

        if (handle != 0)
            ((Texture*)handle)->DecRef();

        return true;
    }
}
