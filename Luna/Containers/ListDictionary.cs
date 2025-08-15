namespace Luna;

/// <summary> A dictionary that stores a list of non-unique values per key, instead of a single one. </summary>
/// <typeparam name="TKey"> The type of the keys. </typeparam>
/// <typeparam name="TValue"> The type of the values. </typeparam>
/// <remarks> Keys can never yield an empty list but get removed instead. </remarks>
public class ListDictionary<TKey, TValue> : IReadOnlyCollection<KeyValuePair<TKey, TValue>> where TKey : notnull
{
    private readonly Dictionary<TKey, List<TValue>> _dict = [];

    /// <summary> Create an empty <see cref="ListDictionary{TKey,TValue}"/>. </summary>
    public ListDictionary()
    { }

    /// <summary> Create an empty <see cref="ListDictionary{TKey,TValue}"/> with pre-allocated storage. </summary>
    /// <param name="count"> The number of key-list pairs to allocate storage for. </param>
    public ListDictionary(int count)
        => _dict = new Dictionary<TKey, List<TValue>>(count);

    /// <summary> Create a <see cref="ListDictionary{TKey,TValue}"/> filled with the given items. </summary>
    /// <param name="values"> A list of items. Duplicate keys append values to their list. </param>
    public ListDictionary(IEnumerable<KeyValuePair<TKey, TValue>> values)
    {
        foreach (var kvp in values)
            TryAdd(kvp.Key, kvp.Value);
    }

    /// <summary> Create a <see cref="ListDictionary{TKey,TValue}"/> from an existing dictionary. </summary>
    /// <param name="dict"> The existing dictionary. </param>
    public ListDictionary(IReadOnlyDictionary<TKey, TValue> dict)
        => _dict = dict.ToDictionary(k => k.Key, v => new List<TValue> { v.Value });

    /// <summary> Iterate the keys grouped with their lists of values without unrolling them to single key-value pairs. </summary>
    public IEnumerable<KeyValuePair<TKey, IReadOnlyList<TValue>>> Grouped
    {
        get
        {
            foreach (var (key, list) in _dict)
                yield return new KeyValuePair<TKey, IReadOnlyList<TValue>>(key, list);
        }
    }

    /// <summary> Try to get the list of values associated with a given key. </summary>
    /// <param name="key"> The key to look for. </param>
    /// <param name="values"> The returned list of values, if the key was found. </param>
    /// <returns> True if the key was found, false otherwise. </returns>
    public bool TryGetValue(in TKey key, out IReadOnlyList<TValue> values)
    {
        if (_dict.TryGetValue(key, out var list))
        {
            values = list;
            return true;
        }

        values = [];
        return false;
    }

    /// <summary> Try to add a value for a given key. </summary>
    /// <param name="key"> The key to add a value for. </param>
    /// <param name="value"> The value to add. </param>
    /// <returns> True. </returns>
    /// <remarks> This can not normally fail, since values are not unique inside their list. </remarks>
    public bool TryAdd(in TKey key, in TValue value)
    {
        ++ValueCount;
        if (_dict.TryGetValue(key, out var list))
        {
            list.Add(value);
            return true;
        }

        list = [value];
        _dict.Add(key, list);
        return true;
    }

    /// <summary> Remove a given key and return the list of values associated with it, if it existed. </summary>
    /// <param name="key"> The key to remove. </param>
    /// <param name="values"> The values associated with the key, if the key was found. </param>
    /// <returns> True if the key was found, false otherwise. </returns>
    public bool Remove(in TKey key, [NotNullWhen(true)] out List<TValue>? values)
    {
        if (_dict.Remove(key, out values))
        {
            ValueCount -= values.Count;
            return true;
        }

        return false;
    }

    /// <summary> Remove a specific value from a key. </summary>
    /// <param name="key"> The key to remove the value from. </param>
    /// <param name="value"> The value to remove. </param>
    /// <returns> True if the key existed and was removed. </returns>
    public bool RemoveValue(in TKey key, TValue value)
    {
        if (!_dict.TryGetValue(key, out var list) || !list.Remove(value))
            return false;

        if (list.Count is 0)
            _dict.Remove(key);

        --ValueCount;
        return true;
    }

    /// <summary> Get whether the dictionary contains at least one value for the given key. </summary>
    /// <param name="key"> The key to search for. </param>
    /// <returns> True if the key was found. </returns>
    public bool ContainsKey(in TKey key)
        => _dict.ContainsKey(key);

    /// <summary> Get whether the dictionary contains the given value in at least one key. </summary>
    /// <param name="value"> The value to search for. </param>
    /// <returns> True if the value was found. </returns>
    public bool ContainsValue(TValue value)
        => _dict.Values.Any(l => l.Contains(value));

    /// <summary> Get the number of distinct keys in the dictionary. </summary>
    public int KeyCount
        => _dict.Count;

    /// <summary> Get the total number of values in the dictionary. </summary>
    public int ValueCount { get; private set; }

    /// <inheritdoc cref="ValueCount"/>
    public int Count
        => ValueCount;

    /// <summary> Get a collection of the keys in this dictionary. </summary>
    public IReadOnlyCollection<TKey> Keys
        => new CollectionAdapter<TKey>(_dict.Keys, _dict.Count);

    /// <summary> Get a collection of the values in this dictionary. </summary>
    public IReadOnlyCollection<TValue> Values
        => new CollectionAdapter<TValue>(_dict.Values.SelectMany(l => l), ValueCount);

    /// <summary> Iterate over all single key-value pairs in this dictionary. </summary>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var (key, list) in _dict)
        {
            foreach (var value in list)
                yield return new KeyValuePair<TKey, TValue>(key, value);
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}

public static class ListDictionaryExtensions
{
    /// <summary> Create a new <see cref="ListDictionary{TKey,TValue}"/> from the list of items. </summary>
    /// <typeparam name="T"> The type of items to convert. </typeparam>
    /// <typeparam name="TKey"> The type of the keys in the dictionary. </typeparam>
    /// <typeparam name="TValue"> The type of the values in the dictionary. </typeparam>
    /// <param name="data"> The list of items. </param>
    /// <param name="keySelector"> The function to turn an item into a key. </param>
    /// <param name="valueSelector"> The function to turn an item into a value. </param>
    /// <returns> The new dictionary. </returns>
    /// <remarks> Duplicate values for identical keys are appended. </remarks>
    public static ListDictionary<TKey, TValue> ToListDictionary<T, TKey, TValue>(this IEnumerable<T> data, Func<T, TKey> keySelector,
        Func<T, TValue> valueSelector) where TKey : notnull
    {
        var ret = new ListDictionary<TKey, TValue>();
        foreach (var obj in data)
        {
            var key   = keySelector(obj);
            var value = valueSelector(obj);
            ret.TryAdd(key, value);
        }

        return ret;
    }
}
