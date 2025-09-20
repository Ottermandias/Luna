namespace Luna;

/// <summary> A child panel within a layout. </summary>
public interface IPanel : IUiService
{
    /// <summary> The ID for the panel. </summary>
    public ReadOnlySpan<byte> Id { get; }

    /// <summary> Draw the content of the panel. Should not draw an encompassing child. </summary>
    public void Draw();
}
