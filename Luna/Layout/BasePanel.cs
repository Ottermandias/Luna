namespace Luna;

/// <summary> A base type for a simple panel. </summary>
/// <param name="id"> The label or ID for the panel. </param>
public class BasePanel(StringU8 id) : IPanel
{
    /// <summary> The label or ID for the panel. </summary>
    public readonly StringU8 Id = id;

    /// <summary> Create a new panel with the given ID. </summary>
    /// <param name="id"> The label or ID for the panel. </param>
    public BasePanel(ReadOnlySpan<byte> id)
        : this(new StringU8(id))
    { }

    /// <inheritdoc/>
    public virtual void Draw()
    { }

    /// <inheritdoc/>
    ReadOnlySpan<byte> IPanel.Id
        => Id.Span;
}
