namespace Luna.DirectX;

/// <summary> A pseudo-effect that does nothing and has no outputs. Can be used as placeholder. </summary>
/// <seealso cref="IEffect.Null"/>
public sealed class NullEffect : IEffect
{
    /// <inheritdoc/>
    public int Count
        => 0;

    /// <inheritdoc/>
    public ImTextureId this[int index]
        => throw new NotSupportedException();

    IList<TextureStandIn> IEffect.Inputs
        => Array.Empty<TextureStandIn>();

    /// <inheritdoc/>
    public IEnumerable<IEffect> GetDependencies()
        => [];

    /// <inheritdoc/>
    public Task Run(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
