namespace Luna;

/// <summary> A slice out of any list without reallocation. </summary>
/// <typeparam name="T"> The type of the list items. </typeparam>
public readonly struct SubList<T> : IReadOnlyList<T>
{
    /// <summary> An empty list. </summary>
    public static readonly SubList<T> Empty = new();

    /// <summary> The base list used to slice. </summary>
    public readonly IList<T> BaseList = Array.Empty<T>();

    /// <summary> The starting index inside the base list for the slice. </summary>
    public readonly int StartIndex = 0;

    /// <summary> The number of elements in the slice. </summary>
    public int Count { get; }

    /// <summary> Create a slice of the given list from a given start index up to the current end of the list. </summary>
    /// <param name="list"> The base list. </param>
    /// <param name="startIndex"> The starting index inside the base list. This is capped between 0 and the current list count. </param>
    public SubList(IList<T> list, int startIndex = 0)
    {
        BaseList   = list;
        StartIndex = Math.Clamp(startIndex, 0, list.Count);
        Count      = list.Count - startIndex;
    }

    /// <summary> Create a slice of the given list from a given start index containing up to <paramref name="count"/> items. </summary>
    /// <param name="list"> The base list. </param>
    /// <param name="startIndex"> The starting index inside the base list. This is capped between 0 and the current list count. </param>
    /// <param name="count"> The maximum number of items in the slice. This is reduced if the base list is too small. </param>
    public SubList(IList<T> list, int startIndex, int count)
    {
        BaseList   = list;
        StartIndex = Math.Clamp(startIndex, 0, list.Count);
        Count      = Math.Clamp(count,      0, list.Count - startIndex);
    }

    /// <summary> Get the element at a specific index within the slice. </summary>
    /// <param name="i"> The index within the slice. </param>
    /// <returns> The element at that index. </returns>
    /// <exception cref="IndexOutOfRangeException"> If the index does not fit inside the current slice or the base list has gotten too small for the current slice. </exception>
    public T this[int i]
    {
        get
        {
            var start = i + StartIndex;
            var end   = Count + StartIndex;
            if (start > end)
                throw new IndexOutOfRangeException();

            return BaseList[start];
        }
        set
        {
            var start = i + StartIndex;
            var end   = Count + StartIndex;
            if (start > end)
                throw new IndexOutOfRangeException();

            BaseList[start] = value;
        }
    }

    /// <summary> Enumerate all elements within the current slice. </summary>
    public IEnumerator<T> GetEnumerator()
        => Count == 0       ? Enumerable.Empty<T>().GetEnumerator() :
            StartIndex == 0 ? BaseList.Take(Count).GetEnumerator() :
                              BaseList.Skip(StartIndex).Take(Count).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
