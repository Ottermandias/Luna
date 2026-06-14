using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;

namespace Luna.DirectX;

/// <summary> An image processing effect that saves its input to a byte array. </summary>
/// <param name="readbackProvider"> Dalamud's texture readback provider. </param>
/// <param name="containerGuid"> The WIC container GUID of the format to save as. </param>
/// <param name="props"> Properties to pass to the encoder. </param>
/// <seealso cref="ITextureReadbackProvider.SaveToStreamAsync"/>
public class SaveToBytesEffect(
    ITextureReadbackProvider readbackProvider,
    Guid containerGuid,
    IReadOnlyDictionary<string, object>? props = null) : WrapEffectBase
{
    private byte[] _bytes = [];

    /// <summary> The last run's image bytes. </summary>
    public byte[] Bytes
        => _bytes;

    /// <inheritdoc/>
    public override int Count
        => 0;

    /// <inheritdoc/>
    public override ImTextureId this[int index]
        => throw new NotSupportedException();

    /// <inheritdoc/>
    protected override async Task Run(IDalamudTextureWrap wrap, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        await readbackProvider.SaveToStreamAsync(wrap, containerGuid, stream, props, true, true, cancellationToken).ConfigureAwait(false);
        _bytes = stream.ToArray();
    }
}
