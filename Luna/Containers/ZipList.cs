namespace Luna;

/// <summary> A simple zipped list that combines two IReadOnlyLists at once and keeps random access and count. </summary>
/// <typeparam name="T1"> The first data type. </typeparam>
/// <typeparam name="T2"> The second data type. </typeparam>
public readonly struct ZipList<T1, T2> : IReadOnlyList<(T1, T2)>
{
    public readonly IList<T1> List1 = Array.Empty<T1>();
    public readonly IList<T2> List2 = Array.Empty<T2>();
    public          int       Count { get; } = 0;

    /// <summary> Create a combined list of two lists. </summary>
    /// <param name="list1"> The first list. </param>
    /// <param name="list2"> The second list. </param>
    /// <remarks> The smaller of both lists limits the total count. </remarks>
    public ZipList(IList<T1> list1, IList<T2> list2)
    {
        List1 = list1;
        List2 = list2;
        Count = Math.Min(list1.Count, list2.Count);
    }

    /// <inheritdoc/>
    public IEnumerator<(T1, T2)> GetEnumerator()
    {
        for (var i = 0; i < Count; ++i)
            yield return (List1[i], List2[i]);
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public (T1, T2) this[int index]
        => (List1[index], List2[index]);
}

public static class ZipList
{
    /// <summary> Create a <see cref="ZipList{T1,T2}"/> from an existing <see cref="SortedList{T1,T2}"/>. </summary>
    /// <typeparam name="T1"> The key type of the sorted list. </typeparam>
    /// <typeparam name="T2"> The value type of the sorted list. </typeparam>
    /// <param name="list"> The input sorted list. </param>
    /// <returns> A zip list that implements <c>IReadOnlyList</c>, which sorted lists do not. </returns>
    public static ZipList<T1, T2> FromSortedList<T1, T2>(SortedList<T1, T2> list) where T1 : notnull
        => new(list.Keys, list.Values);
}
