namespace Luna;

/// <summary> A IReadOnlyList based on any other IReadOnlyList that applies a transforming step before fetching. </summary>
public readonly struct TransformList<TIn, TOut>(IReadOnlyList<TIn> items, Func<TIn, TOut> transform) : IReadOnlyList<TOut>
{
    /// <inheritdoc/>
    public IEnumerator<TOut> GetEnumerator()
        => items.Select(transform).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public int Count
        => items.Count;

    /// <inheritdoc/>
    public TOut this[int index]
        => transform(items[index]);
}
