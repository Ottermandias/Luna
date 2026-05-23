namespace Luna;

/// <summary> An interface for lists implementing more <see cref="List{T}"/> functionality than <see cref="IList{T}"/> demands. </summary>
/// <typeparam name="T"> The type of object. </typeparam>
public interface IAdvancedList<T> : IList<T>, IReadOnlyList<T>
{
    /// <inheritdoc cref="List{T}.TrimExcess"/>
    public void TrimExcess();

    /// <inheritdoc cref="List{T}.RemoveRange"/>
    public void RemoveRange(int index, int count);

    /// <inheritdoc cref="List{T}.EnsureCapacity"/>
    public int EnsureCapacity(int capacity);

    /// <inheritdoc cref="List{T}.AddRange"/>
    public void AddRange(IEnumerable<T> items);

    /// <inheritdoc cref="IList{T}.Count"/>
    public new int Count { get; }

    /// <inheritdoc cref="IList{T}.this"/>
    public new T this[int index] { get; set; }

    /// <inheritdoc/>
    int ICollection<T>.Count
        => Count;

    /// <inheritdoc/>
    int IReadOnlyCollection<T>.Count
        => Count;

    /// <inheritdoc/>
    T IList<T>.this[int index]
    {
        get => this[index];
        set => this[index] = value;
    }

    /// <inheritdoc/>
    T IReadOnlyList<T>.this[int index]
    {
        get => this[index];
    }
}
