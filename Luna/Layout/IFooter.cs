namespace Luna;

/// <summary> A footer. </summary>
public interface IFooter : IUiService
{
    /// <inheritdoc cref="IHeader.Collapsed"/>
    public bool Collapsed { get; }

    /// <inheritdoc cref="IHeader.Draw"/>
    public void Draw(Vector2 size);

    /// <inheritdoc cref="IHeader.PostCollapsed"/>
    public void PostCollapsed()
    {}
}
