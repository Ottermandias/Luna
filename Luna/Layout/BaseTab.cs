namespace Luna;

/// <summary> A basic tab for a tab bar. </summary>
public interface ITab : IUiService
{
    /// <summary> The label displayed in the tab bar for this tab. </summary>
    public ReadOnlySpan<byte> Label { get; }

    /// <summary> Whether the tab should be shown in the tab bar. </summary>
    public bool IsVisible
        => true;

    /// <summary> Additional flags to control this tabs behavior in the tab bar. </summary>
    public TabItemFlags Flags
        => TabItemFlags.None;

    /// <summary> Draw the content of the tab when it is selected and visible. </summary>
    public void DrawContent();

    /// <summary> Invoked after the tab's button is drawn in the tab bar. </summary>
    public void PostTabButton()
    { }
}

/// <summary> A tab associated with an enum identifier. </summary>
/// <typeparam name="T"> The type of the identifier. </typeparam>
public interface ITab<out T> : ITab
    where T : unmanaged, Enum
{
    /// <summary> The identifier for this specific tab. </summary>
    public T Identifier { get; }
}
