namespace Luna;

/// <summary> An efficient bitset to store sets of indices with. </summary>
/// <param name="capacity"> The maximum index that may or may not be set. </param>
/// <param name="initiallyFull"> When true, all indices up to <paramref name="capacity"/> will be initially set, otherwise unset. </param>
public class IndexSet(int capacity, bool initiallyFull) : IEnumerable<int>
{
    private readonly BitArray _set   = new(capacity, initiallyFull);

    /// <summary> The number of set indices. </summary>
    public int Count { get; private set; } = initiallyFull ? capacity : 0;

    /// <summary> The capacity for the highest index. </summary>
    public int Capacity
        => _set.Count;

    /// <summary> Whether no index is set. </summary>
    public bool IsEmpty
        => Count == 0;

    /// <summary> Whether all possible indices are set. </summary>
    public bool IsFull
        => Count == _set.Count;

    /// <summary> Query whether a specific index is set, or set/unset it. </summary>
    /// <param name="index"> The index to query or change. </param>
    /// <returns> Whether the given index is set. </returns>
    public bool this[Index index]
    {
        get => _set[index];
        set
        {
            if (value)
                Add(index);
            else
                Remove(index);
        }
    }

    /// <summary> Set a specific index. </summary>
    /// <param name="index"> The index to set. </param>
    /// <returns> True when the index was previously unset. </returns>
    public bool Add(Index index)
    {
        var ret = !_set[index];
        if (ret)
        {
            ++Count;
            _set[index] = true;
        }

        return ret;
    }

    /// <summary> Unset a specific index. </summary>
    /// <param name="index"> The index to unset. </param>
    /// <returns> True when the index was previously set. </returns>
    public bool Remove(Index index)
    {
        var ret = _set[index];
        if (ret)
        {
            --Count;
            _set[index] = false;
        }

        return ret;
    }

    /// <summary> Set a range of indices. </summary>
    /// <param name="offset"> The first index to set. </param>
    /// <param name="length"> The number of indices from the first index to set. </param>
    /// <returns> The number of indices that are now set and that were previously unset. </returns>
    public int AddRange(int offset, int length)
    {
        var ret = 0;
        for (var idx = 0; idx < length; ++idx)
        {
            if (Add(offset + idx))
                ++ret;
        }

        return ret;
    }

    /// <summary> Unset a range of indices. </summary>
    /// <param name="offset"> The first index to unset. </param>
    /// <param name="length"> The number of indices from the first index to unset. </param>
    /// <returns> The number of indices that are no longer set. </returns>
    public int RemoveRange(int offset, int length)
    {
        var ret = 0;
        for (var idx = 0; idx < length; ++idx)
        {
            if (Remove(offset + idx))
                ++ret;
        }

        return ret;
    }

    /// <summary> Gets an enumerable that will return the indices that are either part of this set, or missing from it. </summary>
    /// <param name="start"> The beginning of the slice to enumerate. </param>
    /// <param name="length"> The maximum length of the slice to enumerate. </param>
    /// <param name="complement"> When false, returns the set indices. When true, returns the unset indices. </param>
    /// <returns> The index enumerable. </returns>
    public IEnumerable<int> Indices(int start = 0, int length = int.MaxValue, bool complement = false)
    {
        var end       = length is int.MaxValue ? _set.Count : Math.Min(start + length, _set.Count);
        var remaining = complement ? _set.Count - Count : Count;

        if (remaining <= 0)
            yield break;

        for (var i = start; i < end; ++i)
        {
            if (_set[i] == complement)
                continue;

            yield return i;

            if (--remaining == 0)
                yield break;
        }
    }

    /// <inheritdoc/>
    public IEnumerator<int> GetEnumerator()
        => Indices().GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => Indices().GetEnumerator();

    /// <summary> Gets an enumerable that will return the ranges of successive indices that are either part of this set, or missing from it. </summary>
    /// <param name="start"> The beginning of the slice to enumerate. </param>
    /// <param name="length"> The maximum length of the slice to enumerate. </param>
    /// <param name="complement">false (default) to get the ranges of indices that are part of this set, true to get those that are missing from it</param>
    /// <returns >The range enumerable. </returns>
    public IEnumerable<(int Start, int End)> Ranges(int start = 0, int length = int.MaxValue, bool complement = false)
    {
        var end       = length is int.MaxValue ? _set.Count : Math.Min(start + length, _set.Count);
        var remaining = complement ? _set.Count - Count : Count;

        if (remaining <= 0)
            yield break;

        for (var i = start; i < end; ++i)
        {
            if (_set[i] == complement)
                continue;

            var rangeStart = i;
            while (i < end && _set[i] != complement)
                ++i;

            yield return (rangeStart, i);

            remaining -= i - rangeStart;
            if (remaining == 0)
                yield break;
        }
    }
}
