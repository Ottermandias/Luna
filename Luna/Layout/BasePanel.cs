namespace Luna;

public class BasePanel(StringU8 id) : IPanel
{
    public readonly StringU8 Id = id;

    public BasePanel(ReadOnlySpan<byte> id)
        : this(new StringU8(id))
    { }

    public virtual void Draw()
    { }

    ReadOnlySpan<byte> IPanel.Id
        => Id.Span;
}
