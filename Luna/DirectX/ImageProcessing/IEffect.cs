namespace Luna.DirectX;

/// <summary> An image processing effect, and the list of its outputs. </summary>
public interface IEffect : IReadOnlyList<ImTextureId>
{
    /// <summary> A pseudo-effect that does nothing and has no inputs and no outputs. Can be used as a placeholder. </summary>
    public static readonly IEffect Null = new NullEffect();

    /// <summary> Gets the list of this effect's inputs. This may be fixed-size, or even read-only. </summary>
    public IList<TextureStandIn> Inputs { get; }

    /// <summary> Gets the dependencies of this effect. </summary>
    /// <returns> The effects whose outputs are inputs to this effect. </returns>
    /// <remarks> This may return duplicates. </remarks>
    public IEnumerable<IEffect> GetDependencies();

    /// <summary> Runs this effect. </summary>
    /// <param name="cancellationToken"> A cancellation token. </param>
    /// <returns> A task that represents this effect running. </returns>
    /// <remarks> This function must be called at a point where the Direct3D device is usable. </remarks>
    public Task Run(CancellationToken cancellationToken);

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    IEnumerator<ImTextureId> IEnumerable<ImTextureId>.GetEnumerator()
    {
        var count = Count;
        for (var i = 0; i < count; ++i)
            yield return this[i];
    }
}
