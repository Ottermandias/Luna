namespace Luna;

/// <summary> An empty structure. Can be used as value of a concurrent dictionary, to use it as a set. </summary>
public readonly struct NullValue : IEquatable<NullValue>
{
    /// <summary> The default NullValue. </summary>
    public static readonly NullValue Void = new();

    /// <inheritdoc/>
    public bool Equals(NullValue other)
        => true;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is NullValue;

    /// <inheritdoc/>
    public override int GetHashCode()
        => 0;

    public static bool operator ==(NullValue _1, NullValue _2)
        => true;

    public static bool operator !=(NullValue _1, NullValue _2)
        => false;
}
