namespace Luna;

/// <summary> An adapter to make a counted <see cref="IReadOnlyCollection{T}"/> from any <see cref="IEnumerable{T}"/> and its known item count. </summary>
/// <typeparam name="T"> The type of the contained values. </typeparam>
/// <param name="enumerable"> The enumerable. </param>
/// <param name="count"> The known count, which should at be at most as big as the number of items yielded by <paramref name="enumerable"/>. </param>
public readonly struct CollectionAdapter<T>(IEnumerable<T> enumerable, int count) : IReadOnlyCollection<T>
{
    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator()
        => enumerable.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public int Count
        => count;
}
