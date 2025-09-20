namespace Luna;

/// <summary> A header. </summary>
public interface IHeader : IUiService
{
    /// <summary> Whether this should take up space and be drawn. </summary>
    public bool Collapsed { get; }

    /// <summary> Draw the object to a given size. </summary>
    public void Draw(Vector2 size);
}

/// <summary> A footer. </summary>
public interface IFooter : IUiService
{
    /// <inheritdoc cref="IHeader.Collapsed"/>
    public bool Collapsed { get; }

    /// <inheritdoc cref="IHeader.Draw"/>
    public void Draw(Vector2 size);
}
