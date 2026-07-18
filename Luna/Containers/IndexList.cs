namespace Luna;

/// <summary> An interface for an object that knows its own index inside a container. </summary>
public interface IIndexed
{
    /// <summary> The index of this object inside its parent container. </summary>
    public int Index { get; }

    /// <summary> Update the index of this object inside its parent container. </summary>
    /// <remarks> Should only be used by <see cref="IndexList{T}"/> and not manually. </remarks>
    public void SetIndex(int newIndex);
}

/// <summary> A list of objects that know their own index that keeps this index invariantly correct. </summary>
/// <typeparam name="T"> The type of object. </typeparam>
public sealed class IndexList<T> : IAdvancedList<T>
    where T : class, IIndexed
{
    private T[] _items = [];
    private int _size;

    /// <inheritdoc cref="List{T}.AddRange"/>
    public void AddRange(IEnumerable<T> items)
    {
        switch (items)
        {
            case ICollection<T> c:         AddCountedItems(items, c.Count); break;
            case IReadOnlyCollection<T> r: AddCountedItems(items, r.Count); break;
            default:
            {
                foreach (var item in items)
                    Add(item);
                break;
            }
        }
    }

    /// <inheritdoc cref="IList{T}.Count"/>
    public int Count
        => _size;

    /// <inheritdoc cref="List{T}.Capacity"/>
    public int Capacity
    {
        get => _items.Length;
        set
        {
            if (value < _size)
                return;

            if (value == _items.Length)
                return;

            if (value is 0)
            {
                _items = [];
            }
            else
            {
                var newItems = new T[value];
                if (_size > 0)
                    Array.Copy(_items, newItems, _size);
                _items = newItems;
            }
        }
    }

    /// <inheritdoc cref="List{T}(int)"/>
    public IndexList(int capacity)
    {
        if (capacity > 0)
            _items = new T[capacity];
    }

    /// <inheritdoc cref="List{T}(IEnumerable{T})"/>
    public IndexList(params IReadOnlyCollection<T> collection)
        : this((IEnumerable<T>)collection)
    { }

    /// <inheritdoc cref="List{T}(IEnumerable{T})"/>
    public IndexList(IEnumerable<T> collection)
    {
        switch (collection)
        {
            case ICollection<T> c:         AddCountedItems(collection, c.Count); break;
            case IReadOnlyCollection<T> r: AddCountedItems(collection, r.Count); break;
            default:
            {
                foreach (var obj in collection)
                    Add(obj);
                break;
            }
        }
    }

    /// <inheritdoc cref="List{T}.GetEnumerator"/>
    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < _size; ++i)
            yield return _items[i];
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc cref="List{T}.Add"/>
    /// <remarks> This sets the index of the object. </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        if (_size == _items.Length)
            Grow(_size + 1);
        item.SetIndex(_size);
        _items[_size] = item;
        ++_size;
    }

    /// <inheritdoc cref="List{T}.Clear"/>
    /// <remarks> This does not remove the index from the objects. </remarks>
    public void Clear()
    {
        if (_size > 0)
            Array.Clear(_items, 0, _size);
        _size = 0;
    }

    /// <inheritdoc cref="List{T}.Contains"/>
    public bool Contains(T item)
        => _size > 0 && IndexOf(item) >= 0;

    /// <inheritdoc cref="List{T}.CopyTo(T[],int)"/>
    public void CopyTo(T[] array, int arrayIndex)
        => Array.Copy(_items, 0, array, arrayIndex, _size);

    /// <inheritdoc cref="List{T}.Remove"/>
    /// <remarks> This does not remove the index from the object, but adapts all other objects for their new indices. </remarks>
    public bool Remove(T item)
    {
        var idx = IndexOf(item);
        if (idx < 0)
            return false;

        RemoveAt(idx);
        return true;
    }

    /// <inheritdoc/>
    public int IndexOf(T item)
    {
        if (item.Index >= 0 && item.Index < _size && ReferenceEquals(_items[item.Index], item))
            return item.Index;

        return -1;
    }

    /// <inheritdoc/>
    /// <remarks> This sets the index of the inserted objects and adapts the indices of all items after the insertion point. </remarks>
    public void Insert(int index, T item)
    {
        if (_size == _items.Length)
            InsertGrow(index, 1);
        else if (index < _size)
            for (var i = _size - 1; i >= index; --i)
            {
                var shiftedItem = _items[i];
                shiftedItem.SetIndex(i + 1);
                _items[shiftedItem.Index] = shiftedItem;
            }

        item.SetIndex(index);
        _items[index] = item;
        ++_size;
    }

    /// <inheritdoc/>
    /// <remarks> This does not remove the index from the object, but adapts all other objects for their new indices.</remarks>
    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _size)
            return;

        --_size;
        if (index < _size)
            for (var i = index; i < _size; ++i)
            {
                var shiftedItem = _items[index + 1];
                shiftedItem.SetIndex(index);
                _items[index]     = shiftedItem;
            }

        _items[_size] = null!;
    }

    /// <inheritdoc cref="List{T}.TrimExcess"/>
    public void TrimExcess()
    {
        var threshold = _items.Length * 9 / 10;
        if (_size < threshold)
            Capacity = _size;
    }

    /// <inheritdoc cref="List{T}.RemoveRange"/>
    /// <remarks> This does not remove the index from any removed object, but corrects the index of the moved objects. </remarks>
    public void RemoveRange(int index, int count)
    {
        if (!ListExtensions.AdaptRangeIndices(ref index, ref count, _size))
            return;

        _size -= count;
        if (index < _size)
            for (var i = index; i < _size - index; ++i)
            {
                var item = _items[i + count];
                item.SetIndex(i);
                _items[i]  = item;
            }

        Array.Clear(_items, _size, count);
    }

    /// <inheritdoc cref="List{T}.RemoveAll"/>
    public int RemoveAll(Predicate<T> match)
    {
        var removedIndex = 0;
        while (removedIndex < _size && !match(_items[removedIndex]))
            ++removedIndex;

        if (removedIndex == _size)
            return 0;

        var keptIndex = removedIndex + 1;
        while (keptIndex < _size)
        {
            while (keptIndex < _size && match(_items[keptIndex]))
                ++keptIndex;

            if (keptIndex < _size)
            {
                var keptItem = _items[keptIndex++];
                keptItem.SetIndex(removedIndex);
                _items[removedIndex++] = keptItem;
            }
        }

        Array.Clear(_items, removedIndex, _size - removedIndex);
        var ret = _size - removedIndex;
        _size = removedIndex;
        return ret;
    }

    /// <inheritdoc cref="List{T}.EnsureCapacity"/>
    public int EnsureCapacity(int capacity)
    {
        if (_items.Length < capacity)
            Grow(capacity);
        return _items.Length;
    }

    /// <inheritdoc cref="IList{T}.this"/>
    /// <remarks> This does not remove the index from any overwritten object, but sets the index of the overwriting object. </remarks>
    public T this[int index]
    {
        get => _items[index];
        set
        {
            _items[index] = value;
            value.SetIndex(index);
        }
    }

    /// <inheritdoc/>
    public bool IsReadOnly
        => false;

    private void Grow(int capacity)
    {
        var newCapacity = _items.Length is 0 ? 4 : 2 * _items.Length;
        if (newCapacity < capacity)
            newCapacity = capacity;

        Capacity = newCapacity;
    }

    private void InsertGrow(int where, int count)
    {
        var requiredCapacity = _size + count;
        var newCapacity      = _items.Length is 0 ? 4 : 2 * _items.Length;
        if (newCapacity < requiredCapacity)
            newCapacity = requiredCapacity;

        var newItems = new T[newCapacity];
        if (where is not 0)
            Array.Copy(_items, newItems, where);
        if (where != _size)
            for (var i = where; i < _size; ++i)
            {
                var item = _items[i];
                item.SetIndex(i + count);
                newItems[item.Index] = item;
            }

        _items = newItems;
    }

    private void AddCountedItems(IEnumerable<T> items, int itemCount)
    {
        if (itemCount is 0)
            return;

        var newSize = _size + Count;
        if (_items.Length < newSize)
            Grow(newSize);
        var idx = _size;
        foreach (var item in items)
        {
            item.SetIndex(idx);
            _items[idx++] = item;
        }

        _size += itemCount;
    }
}
