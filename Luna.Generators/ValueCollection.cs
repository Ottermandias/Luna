using System.Collections;

namespace Luna.Generators;

/// <summary> A collection adapter for equality comparison of collections by element. </summary>
/// <typeparam name="T"> The type of the items. </typeparam>
/// <param name="collection"> The base collection to use. </param>
internal readonly struct ValueCollection<T>(IReadOnlyCollection<T> collection) : IEquatable<ValueCollection<T>>, IReadOnlyCollection<T>
    where T : IEquatable<T>
{
    /// <summary> The base collection to use. </summary>
    public readonly IReadOnlyCollection<T> Collection = collection;

    /// <summary> Compares two collections on being sequentially equal. </summary>
    public bool Equals(ValueCollection<T> other)
        => Count == other.Count && Collection.SequenceEqual(other.Collection);

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator()
        => Collection.GetEnumerator();

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is ValueCollection<T> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
        => Collection.Aggregate(Count, (current, item) => HashCode.Combine(current, item.GetHashCode()));

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public static bool operator ==(ValueCollection<T> left, ValueCollection<T> right)
        => left.Equals(right);

    public static bool operator !=(ValueCollection<T> left, ValueCollection<T> right)
        => !left.Equals(right);

    /// <inheritdoc/>
    public int Count
        => Collection.Count;
}
