namespace Luna;

/// <summary> A header or footer that does nothing and is always collapsed, i.e. takes up no space and does not draw anything. </summary>
public sealed class NopHeaderFooter : IHeader, IFooter
{
    /// <inheritdoc cref="IHeader.Collapsed"/>
    public bool Collapsed
        => true;

    /// <inheritdoc cref="IHeader.Collapsed"/>
    public void Draw(Vector2 size)
    { }

    /// <inheritdoc cref="NopHeaderFooter"/>
    public static readonly NopHeaderFooter Instance = new();

    /// <summary> Use <see cref="Instance"/> instead. </summary>
    private NopHeaderFooter()
    {}
}
