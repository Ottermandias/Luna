namespace Luna;

/// <summary> A dictionary with ReadWrite locks on actions that also exposes its own lock. </summary>
public class ReadWriteDictionary<TKey, TValue>() : ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion), IDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _dict = [];

    /// <summary> Dispose the lock as well as inheritors dispose functions. </summary>
    public new void Dispose()
    {
        Dispose(true);
        base.Dispose();
    }

    protected virtual void Dispose(bool _)
    { }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        EnterReadLock();
        try
        {
            foreach (var kvp in _dict)
                yield return kvp;
        }
        finally
        {
            ExitReadLock();
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public void Add(KeyValuePair<TKey, TValue> item)
    {
        using var @lock = new WriteLock(this);
        _dict.Add(item.Key, item.Value);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        using var @lock = new WriteLock(this);
        _dict.Clear();
    }

    /// <inheritdoc/>
    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        using var @lock = new ReadLock(this);
        return _dict.ContainsKey(item.Key);
    }

    /// <inheritdoc/>
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        using var @lock = new ReadLock(this);
        ((ICollection<KeyValuePair<TKey, TValue>>)_dict).CopyTo(array, arrayIndex);
    }

    /// <inheritdoc/>
    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        using var @lock = new WriteLock(this);
        return _dict.Remove(item.Key);
    }

    /// <inheritdoc/>
    public int Count
    {
        get
        {
            using var @lock = new ReadLock(this);
            return _dict.Count;
        }
    }

    /// <inheritdoc/>
    public bool IsReadOnly
        => false;

    /// <inheritdoc/>
    public void Add(TKey key, TValue value)
    {
        using var @lock = new WriteLock(this);
        _dict.Add(key, value);
    }

    /// <inheritdoc/>
    public bool ContainsKey(TKey key)
    {
        using var @lock = new ReadLock(this);
        return _dict.ContainsKey(key);
    }

    /// <inheritdoc/>
    public bool Remove(TKey key)
    {
        using var @lock = new WriteLock(this);
        return _dict.Remove(key);
    }

    /// <inheritdoc cref="Remove(TKey)"/>
    public bool Remove(TKey key, [NotNullWhen(true)] out TValue? value)
    {
        using var @lock = new WriteLock(this);
        return _dict.Remove(key, out value!);
    }

    /// <inheritdoc/>
    public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue? value)
    {
        using var @lock = new ReadLock(this);
        return _dict.TryGetValue(key, out value!);
    }

    /// <inheritdoc/>
    public TValue this[TKey key]
    {
        get
        {
            using var @lock = new ReadLock(this);
            return _dict[key];
        }
        set
        {
            using var @lock = new ReadLock(this);
            _dict[key] = value;
        }
    }

    /// <inheritdoc/>
    public ICollection<TKey> Keys
    {
        get
        {
            using var @lock = new ReadLock(this);
            return _dict.Keys;
        }
    }

    /// <inheritdoc/>
    public ICollection<TValue> Values
    {
        get
        {
            using var @lock = new ReadLock(this);
            return _dict.Values;
        }
    }

    /// <summary> Disposable to enter a write lock and exit on leaving scope. </summary>
    private readonly ref struct WriteLock
    {
        private readonly ReaderWriterLockSlim _lock;

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public WriteLock(ReaderWriterLockSlim @lock)
        {
            _lock = @lock;
            _lock.EnterWriteLock();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void Dispose()
            => _lock.ExitWriteLock();
    }

    /// <summary> Disposable to enter a read lock and exit on leaving scope. </summary>
    private readonly ref struct ReadLock
    {
        private readonly ReaderWriterLockSlim _lock;

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public ReadLock(ReaderWriterLockSlim @lock)
        {
            _lock = @lock;
            _lock.EnterReadLock();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void Dispose()
            => _lock.ExitReadLock();
    }
}
