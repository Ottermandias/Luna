namespace Luna;

/// <inheritdoc cref="ReadOnlyWrapper{TKey1,TKey2,TValue,T1}"/>
public readonly struct ReadOnlyWrapper<TKey1, TKey2, TValue>(IReadOnlyDictionary<TKey1, Dictionary<TKey2, TValue>> data)
    : IReadOnlyDictionary<TKey1, IReadOnlyDictionary<TKey2, TValue>> where TKey2 : notnull
{
    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TKey1, IReadOnlyDictionary<TKey2, TValue>>> GetEnumerator()
        => data.Select(kvp => new KeyValuePair<TKey1, IReadOnlyDictionary<TKey2, TValue>>(kvp.Key, kvp.Value)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public int Count
        => data.Count;

    /// <inheritdoc/>
    public bool ContainsKey(TKey1 key)
        => data.ContainsKey(key);

    /// <inheritdoc/>
    public bool TryGetValue(TKey1 key, [MaybeNullWhen(false)] out IReadOnlyDictionary<TKey2, TValue> value)
    {
        if (data.TryGetValue(key, out var v))
        {
            value = v;
            return true;
        }

        value = null;
        return false;
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<TKey2, TValue> this[TKey1 key]
        => data[key];

    /// <inheritdoc/>
    public IEnumerable<TKey1> Keys
        => data.Keys;

    /// <inheritdoc/>
    public IEnumerable<IReadOnlyDictionary<TKey2, TValue>> Values
        => data.Values;
}

/// <summary> A wrapper around a dictionary of dictionaries to make it immutable. </summary>
/// <typeparam name="TKey1"> The key type of the outer dictionary. </typeparam>
/// <typeparam name="TKey2"> The key type of the inner dictionary. </typeparam>
/// <typeparam name="TValue"> The value type of the inner dictionary. </typeparam>
/// <typeparam name="T1"> An additional type for value tuples on the inner dictionary. </typeparam>
/// <param name="data"> The mutable base data. </param>
public readonly struct ReadOnlyWrapper<TKey1, TKey2, TValue, T1>(IReadOnlyDictionary<TKey1, (Dictionary<TKey2, TValue>, T1)> data)
    : IReadOnlyDictionary<TKey1, (IReadOnlyDictionary<TKey2, TValue>, T1)> where TKey2 : notnull
{
    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TKey1, (IReadOnlyDictionary<TKey2, TValue>, T1)>> GetEnumerator()
        => data.Select(kvp => new KeyValuePair<TKey1, (IReadOnlyDictionary<TKey2, TValue>, T1)>(kvp.Key, (kvp.Value.Item1, kvp.Value.Item2)))
            .GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public int Count
        => data.Count;

    /// <inheritdoc/>
    public bool ContainsKey(TKey1 key)
        => data.ContainsKey(key);

    /// <inheritdoc/>
    public bool TryGetValue(TKey1 key, out (IReadOnlyDictionary<TKey2, TValue>, T1) value)
    {
        if (data.TryGetValue(key, out var v))
        {
            value = v;
            return true;
        }

        value = (null, default)!;
        return false;
    }

    /// <inheritdoc/>
    public (IReadOnlyDictionary<TKey2, TValue>, T1) this[TKey1 key]
        => data[key];

    /// <inheritdoc/>
    public IEnumerable<TKey1> Keys
        => data.Keys;

    /// <inheritdoc/>
    public IEnumerable<(IReadOnlyDictionary<TKey2, TValue>, T1)> Values
        => data.Values.Select(p => ((IReadOnlyDictionary<TKey2, TValue>)p.Item1, p.Item2));
}
