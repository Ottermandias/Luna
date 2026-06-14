using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;

namespace Luna.DirectX;

/// <summary> Base class for effects that retrieve the pixel data and process it on the CPU side. </summary>
/// <param name="readbackProvider"> Dalamud's texture readback provider. </param>
public abstract class ReadbackEffect(ITextureReadbackProvider readbackProvider) : WrapEffectBase
{
    /// <inheritdoc/>
    public override int Count
        => 0;

    /// <inheritdoc/>
    public override ImTextureId this[int index]
        => throw new NotSupportedException();

    /// <summary> The texture modification arguments to pass to <see cref="ITextureReadbackProvider.GetRawImageAsync"/>. </summary>
    protected virtual TextureModificationArgs TextureModificationArgs
        => default;

    /// <inheritdoc/>
    protected override async Task Run(IDalamudTextureWrap wrap, CancellationToken cancellationToken)
    {
        var (specification, rawData) = await readbackProvider.GetRawImageAsync(wrap, TextureModificationArgs, true, cancellationToken)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        await Run(specification, rawData, cancellationToken).ConfigureAwait(false);
    }

    /// <summary> Runs this effect. </summary>
    /// <param name="specification"> The image specification of the input texture. </param>
    /// <param name="rawData"> The raw pixel data of the input texture. </param>
    /// <param name="cancellationToken"> A cancellation token. </param>
    /// <returns> A task that represents this effect running. </returns>
    protected abstract Task Run(RawImageSpecification specification, byte[] rawData, CancellationToken cancellationToken = default);
}
