using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;

namespace Luna.DirectX;

/// <summary> Base class for effects that process a single input texture as a Dalamud wrap. </summary>
/// <seealso cref="ITextureReadbackProvider"/>
public abstract class WrapEffectBase : IEffect
{
    /// <summary> The image to process. </summary>
    public TextureStandIn Input;

    /// <inheritdoc/>
    public abstract int Count { get; }

    /// <inheritdoc/>
    public abstract ImTextureId this[int index] { get; }

    /// <inheritdoc/>
    public IEnumerable<IEffect> GetDependencies()
    {
        if (Input.TryGetListAndIndex(out var list, out _) && list is IEffect effect)
            yield return effect;
    }

    /// <inheritdoc/>
    public Task Run(CancellationToken cancellationToken)
        => Input.InvokeWithWrap(wrap => Run(wrap, cancellationToken));

    /// <summary> Runs this effect. </summary>
    /// <param name="wrap"> The input texture. </param>
    /// <param name="cancellationToken"> A cancellation token. </param>
    /// <returns> A task that represents this effect running. </returns>
    protected abstract Task Run(IDalamudTextureWrap wrap, CancellationToken cancellationToken);
}
