namespace Luna;

/// <summary> A base class for windows that are indexed by an integer so that multiple windows of the same type can be opened. </summary>
public abstract class IndexedWindow : Window
{
    /// <summary> An event invoked when the window is closed. </summary>
    public event Action<IndexedWindow>? Closed;

    /// <summary> The name of the window type excluding the index. </summary>
    /// <remarks> Use <see cref="Dalamud.Interface.Windowing.Window.WindowName"/> for the name including the index. </remarks>
    public readonly string Name;

    /// <summary> The index of the window. </summary>
    public readonly int Index;

    /// <inheritdoc cref="Window"/>
    /// <remarks> Also adds itself to the parent window system, and is removed from it on closure. </remarks>
    protected IndexedWindow(string name, int index, WindowFlags flags = WindowFlags.None, bool forceMainWindow = false)
        : base($"{name}{index}", flags, forceMainWindow)
    {
        Name  = name;
        Index = index;
    }

    /// <summary> Close the window and invoke the <see cref="Closed"/> event. </summary>
    public override void OnClose()
        => Closed?.Invoke(this);
}
