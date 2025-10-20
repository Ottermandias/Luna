namespace Luna;

/// <summary> A base factory to create indexed windows of type T. </summary>
/// <typeparam name="TWindow"> The actual type of the indexed window. </typeparam>
/// <param name="log"> <inheritdoc cref="Log"/> </param>
/// <param name="windowSystem"> <inheritdoc cref="WindowSystem"/> </param>
public abstract class WindowFactory<TWindow>(Logger log, WindowSystem windowSystem) : IReadOnlyCollection<TWindow>
    where TWindow : IndexedWindow
{
    /// <summary> The logger used for logging messages from this factory. </summary>
    protected readonly Logger Log = log;

    /// <summary> The window system to add created windows to. </summary>
    protected readonly WindowSystem WindowSystem = windowSystem;

    /// <summary> The set of currently open windows. </summary>
    protected readonly HashSet<TWindow> Windows = [];

    /// <summary> The set of already used but now reusable indices. </summary>
    protected readonly HashSet<int> ReusableIndices = [];

    /// <summary> The next entirely unused index. </summary>
    protected int NextIndex;

    /// <summary> Obtain a free index for a new window. Reuse indices from closed windows if possible. </summary>
    /// <returns> The next free index. </returns>
    protected int GetFreeIndex()
    {
        foreach (var index in ReusableIndices)
        {
            ReusableIndices.Remove(index);
            return index;
        }

        Log.Debug($"Obtained new index {NextIndex}for {typeof(TWindow).Name} factory.");
        return NextIndex++;
    }

    protected abstract TWindow? CreateWindow(int index);

    /// <summary> Create a new window of type T and set it up on success. </summary>
    /// <returns> The newly created window on success, null otherwise. </returns>
    protected TWindow? CreateWindowInternal()
    {
        if (CreateWindow(GetFreeIndex()) is not { } window)
        {
            Log.Error($"Failed to create new {typeof(TWindow).Name} window.");
            return null;
        }

        SetupWindow(window);
        return window;
    }

    /// <summary> Set up the base attributes of a window created with this factory. </summary>
    protected virtual void SetupWindow(TWindow window)
    {
        window.Closed += OnWindowClosed;
        WindowSystem.AddWindow(window);
        window.IsOpen = true;
        Windows.Add(window);
        window.BringToFront();
        Log.Verbose($"Opened new {typeof(TWindow).Name} window [{window.Name}] with index {window.Index}.");
    }

    /// <summary> Handle the closing of a window created with this factory. </summary>
    protected virtual void OnWindowClosed(IndexedWindow obj)
    {
        if (obj is not TWindow window)
            return;

        WindowSystem.RemoveWindow(window);
        Windows.Remove(window);
        ReusableIndices.Add(window.Index);
        Log.Verbose($"Closed {typeof(TWindow).Name} window [{window.Name}] and returned index {window.Index}.");
    }

    /// <inheritdoc/>
    public IEnumerator<TWindow> GetEnumerator()
        => Windows.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <summary> The count of currently opened windows. </summary>
    public int Count
        => Windows.Count;
}
