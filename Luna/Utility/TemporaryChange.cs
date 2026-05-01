namespace Luna;

/// <summary> Allows temporarily changing a variable, a field, an array/span element, or any <c>ref</c>-able. </summary>
public static class TemporaryChange
{
    /// <summary> Temporarily sets the given location to the given value. Returns a disposable to undo the change, for use in a <c>using</c> block. </summary>
    /// <param name="location"> The variable, field, array/span element or other <c>ref</c>-able to change. </param>
    /// <param name="value"> The new value to set <paramref name="location"/> to. </param>
    /// <param name="condition"> If this is <c>false</c>, do not actually enact the change, and do not undo it on disposal. </param>
    /// <typeparam name="T"> The type of the changed variable. </typeparam>
    public static TemporaryChange<T> Set<T>(ref T location, T value, bool condition = true)
        => new(ref location, value, condition);
}

/// <summary> Allows temporarily changing a variable, a field, an array/span element, or any <c>ref</c>-able with a <c>using</c> block. </summary>
/// <typeparam name="T"> The type of the changed variable. </typeparam>
public ref struct TemporaryChange<T>
{
    private readonly ref T _location;
    private readonly     T _savedValue;

    /// <summary> Whether to undo the change on disposal. </summary>
    public bool Restore;

    /// <summary> Creates a new <see cref="TemporaryChange{T}"/>. </summary>
    /// <param name="location"> The variable, field, array/span element or other <c>ref</c>-able to change. </param>
    /// <param name="value"> The new value to set <paramref name="location"/> to. </param>
    /// <param name="condition"> If this is <c>false</c>, do not actually enact the change, and do not undo it on disposal. </param>
    public TemporaryChange(ref T location, T value, bool condition = true)
    {
        _location   = ref location;
        _savedValue = location;
        if (condition)
            location = value;
        Restore = condition;
    }

    /// <summary> Restores the saved value, unless disabled. </summary>
    public void Dispose()
    {
        if (!Restore)
            return;

        _location = _savedValue;
    }
}
