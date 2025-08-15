namespace Luna;

/// <summary> An adapter to keep an arbitrary list sorted according to a custom comparer. </summary>
/// <typeparam name="T"> The type of the objects in the list. </typeparam>
/// <param name="list"> The base list to keep sorted. </param>
/// <param name="comparer"> The comparer to compare objects with. </param>
/// <remarks> Items do not have to be unique, but the sort is not stable and order of items comparing equal is not guaranteed. </remarks>
public readonly struct SortedListAdapter<T>(List<T> list, IComparer<T>? comparer = null) : IList<T>, IReadOnlyList<T>
{
    /// <summary> The comparer used to sort the list. </summary>
    public readonly IComparer<T> Comparer = comparer ?? Comparer<T>.Default;

    /// <summary> The base list. </summary>
    public IReadOnlyList<T> List
        => list;

    /// <summary> Create an adapter keeping the given list sorted according to the provided comparison. </summary>
    /// <param name="list"> The base list to keep sorted. </param>
    /// <param name="comparison"> The comparison method to use for the comparer. </param>
    public SortedListAdapter(List<T> list, Comparison<T> comparison)
        : this(list, Comparer<T>.Create(comparison))
    { }

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator()
        => list.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <summary> Add an item at the correctly sorted position in the list. </summary>
    /// <param name="item"> The item to add. </param>
    public void Add(T item)
    {
        var idx = list.BinarySearch(item, Comparer);
        if (idx < 0)
            list.Insert(~idx, item);
        else
            list.Insert(idx, item);
    }

    /// <summary> Add an item at the correctly sorted position in the list if no item compares equal to it. </summary>
    /// <param name="item"> The item to add. </param>
    /// <returns> True if the item was added, false otherwise. </returns>
    public bool AddUnique(T item)
    {
        var idx = list.BinarySearch(item, Comparer);
        if (idx < 0)
        {
            list.Insert(~idx, item);
            return true;
        }

        return false;
    }

    /// <summary> Add an arbitrary list of items to the list and then resort it. </summary>
    /// <param name="items"> The list of items to add. </param>
    public void AddMany(IEnumerable<T> items)
    {
        list.AddRange(items);
        list.Sort(Comparer);
    }

    /// <inheritdoc/>
    public void Clear()
        => list.Clear();

    /// <inheritdoc/>
    public bool Contains(T item)
        => list.BinarySearch(item, Comparer) >= 0;

    /// <inheritdoc/>
    public void CopyTo(T[] array, int arrayIndex)
        => list.CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public bool Remove(T item)
        => list.Remove(item);

    /// <inheritdoc/>
    public int Count
        => list.Count;

    /// <inheritdoc/>
    public bool IsReadOnly
        => false;

    /// <inheritdoc/>
    public int IndexOf(T item)
        => Math.Max(list.BinarySearch(item), -1);

    /// <summary> Inserting an item at an arbitrary position is not supported. </summary>
    public void Insert(int index, T item)
        => throw new NotSupportedException("Can not insert value in sorted list.");

    /// <inheritdoc/>
    public void RemoveAt(int index)
        => list.RemoveAt(index);

    /// <summary> Get the item at a given position. Setting items at arbitrary positions is not supported. </summary>
    public T this[int index]
    {
        get => list[index];
        set => throw new NotSupportedException("Can not set value in sorted list.");
    }
}
