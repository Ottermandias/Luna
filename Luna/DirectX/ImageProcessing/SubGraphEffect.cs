using Dalamud.Interface.Textures.TextureWraps;

namespace Luna.DirectX;

/// <summary> Wraps an effect graph in a single effect. Can be used for multi-pass effects such as separable filters. </summary>
/// <param name="graph"> The effect graph to wrap. </param>
public class SubGraphEffect(EffectGraph graph) : IEffect, IDisposable, ITextureWrapProvider
{
    /// <summary> A list of outputs of effects of the wrapped graph, re-exported by this effect. </summary>
    public readonly List<TextureStandIn> Outputs = new(4);

    /// <summary> The wrapped effect graph. </summary>
    public EffectGraph EffectGraph
        => graph;

    /// <inheritdoc/>
    public int Count
        => Outputs.Count;

    /// <inheritdoc/>
    public ImTextureId this[int index]
        => Outputs[index];

    ~SubGraphEffect()
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
        => graph.Dispose();

    /// <inheritdoc/>
    public IEnumerable<IEffect> GetDependencies()
        => graph.SelectMany(effect => effect.GetDependencies()).Where(dependency => !graph.Contains(dependency));

    IDalamudTextureWrap? ITextureWrapProvider.GetTextureWrap(int index)
    {
        Outputs[index].TryGetWrap(out var wrap);
        return wrap;
    }

    /// <inheritdoc/>
    public Task Run(CancellationToken cancellationToken)
        => graph.Run(null, cancellationToken);
}
