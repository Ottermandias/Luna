namespace Luna;

/// <summary> A header. </summary>
public interface IHeader : IUiService
{
    /// <summary> Whether this should take up space and be drawn. </summary>
    public bool Collapsed { get; }

    /// <summary> Draw the object to a given size. </summary>
    public void Draw(Vector2 size);

    /// <summary> Actions to execute if the object is collapsed. </summary>
    public void PostCollapsed()
    { }
}
