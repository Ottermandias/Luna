namespace Luna;

/// <summary> A header or footer that does nothing and is not collapsed, i.e. takes up space but does not draw anything. </summary>
public sealed class EmptyHeaderFooter : IHeader, IFooter
{
    /// <inheritdoc cref="IHeader.Collapsed"/>
    public bool Collapsed
        => false;

    /// <inheritdoc cref="IHeader.Draw"/>
    public void Draw(Vector2 size)
    { }

    /// <inheritdoc cref="EmptyHeaderFooter"/>
    public static readonly EmptyHeaderFooter Instance = new();

    /// <summary> Use <see cref="Instance"/> instead. </summary>
    private EmptyHeaderFooter()
    { }
}
