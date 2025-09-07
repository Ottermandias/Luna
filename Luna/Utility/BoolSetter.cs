namespace Luna;

/// <summary> A simple wrapper disposable that sets a bool on creation and resets it on disposal. </summary>
/// <remarks> Use with using. </remarks>
public readonly ref struct BoolSetter : IDisposable
{
    private readonly ref bool _value;

    /// <summary> Create a setter referencing a specific bool and setting it to the provided value. </summary>
    /// <param name="value"> The reference to manipulate. </param>
    /// <param name="setTo"> The value to set the reference to. Note that disposal always resets it to false, not to the opposite of what was set here. </param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public BoolSetter(ref bool value, bool setTo = true)
    {
        _value = ref value;
        _value = setTo;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        _value = false;
    }
}
