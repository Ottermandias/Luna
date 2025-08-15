namespace Luna;

/// <summary> A container for an arbitrary set of <see cref="IDisposable"/>s that disposes all of them on disposal. </summary>
/// <param name="disposables"> Any number of <see cref="IDisposable"/>s. </param>
public struct DisposableContainer(params IEnumerable<IDisposable?> disposables) : IDisposable
{
    /// <summary> A value that gets default-initialized to false and prevents multiple-disposal. </summary>
    public bool Alive { get; private set; }

    /// <summary> Create an empty container. </summary>
    public DisposableContainer()
        : this(Array.Empty<IDisposable?>())
    { }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!Alive)
            return;

        foreach (var disposable in disposables)
            disposable?.Dispose();
        Alive = false;
    }

    /// <summary> A static instance of an empty disposable container. </summary>
    public static readonly DisposableContainer Empty = new();
}
