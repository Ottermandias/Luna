namespace Luna;

/// <summary> A utility struct to check whether a thread is within a specific method. </summary>
public readonly struct InMethodChecker
{
    private readonly ThreadLocal<uint> _inUpdate = new(() => 0, false);

    /// <summary> The current recursion depth within this thread. </summary>
    public uint RecursionDepth
        => _inUpdate.IsValueCreated ? _inUpdate.Value : 0;

    /// <summary> Whether we are currently within a checked method or not in this thread. </summary>
    public bool InMethod
        => _inUpdate is { IsValueCreated: true, Value: > 0 };

    /// <summary> Notify the checker that we have entered a tracked method. </summary>
    /// <returns> A disposable that notifies the checker that the tracked method has been exited on disposal. Use with using. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public RaiiSetter EnterMethod()
        => new(_inUpdate);

    /// <summary> Create a new InMethodChecker. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public InMethodChecker()
    { }

    /// <summary> The disposable scope-based setter used to notify of entering and exiting methods. </summary>
    public readonly ref struct RaiiSetter : IDisposable
    {
        private readonly ThreadLocal<uint> _threadLocal;

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal RaiiSetter(ThreadLocal<uint> threadLocal)
        {
            _threadLocal = threadLocal;
            ++_threadLocal.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void Dispose()
            => --_threadLocal.Value;
    }
}
