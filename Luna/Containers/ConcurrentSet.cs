namespace Luna;

/// <summary> An easy implementation of ConcurrentSet </summary>
public sealed class ConcurrentSet<T> : ConcurrentDictionary<T, NullValue> where T : notnull
{
    public new IEnumerator<T> GetEnumerator()
        => Keys.GetEnumerator();

    /// <summary> Try to add a value to the set. </summary>
    public bool TryAdd(T value)
        => base.TryAdd(value, NullValue.Void);

    /// <summary> Try to remove a value from the set. </summary>
    public bool TryRemove(T value)
        => base.TryRemove(value, out _);

    /// <remarks> Hide from public interface. </remarks>
    private new bool TryAdd(T key, NullValue value)
        => base.TryAdd(key, value);

    /// <remarks> Hide from public interface. </remarks>
    private new bool TryRemove(T value, out NullValue ret)
        => base.TryRemove(value, out ret);
}
