using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Utility;

namespace Luna.DirectX;

/// <summary> An effect that loads an image. </summary>
/// <param name="loader"> The actual load operation. </param>
public class LoadEffect(Task<IDalamudTextureWrap> loader, bool leaveWrapOpen = false) : IEffect, ITextureWrapProvider, IDisposable
{
    /// <inheritdoc/>
    public int Count
        => 1;

    /// <inheritdoc/>
    public ImTextureId this[int index]
        => GetTextureWrap(index).Id;

    IList<TextureStandIn> IEffect.Inputs
        => Array.Empty<TextureStandIn>();

    ~LoadEffect()
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
    {
        if (!disposing || leaveWrapOpen)
            return;

        if (!loader.IsCompleted)
            loader.ToContentDisposedTask(true);
        else if (loader.IsCompletedSuccessfully)
            loader.Result.Dispose();
    }

    /// <inheritdoc/>
    public IEnumerable<IEffect> GetDependencies()
        => [];

    /// <inheritdoc/>
    public IDalamudTextureWrap GetTextureWrap(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(index, 0);

        if (!loader.IsCompleted)
            throw new InvalidOperationException("This texture is not ready");

        return loader.Result;
    }

    /// <inheritdoc/>
    public async Task Run(CancellationToken cancellationToken)
        => await loader.ConfigureAwait(false);
}
