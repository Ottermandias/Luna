namespace Luna;

/// <summary> A comparer that compares two file system nodes by their name only. </summary>
/// <param name="baseComparer"> The base comparer to compare the strings with. </param>
internal readonly struct NameComparer(IComparer<ReadOnlySpan<char>> baseComparer) : IComparer<IFileSystemNode>
{
    /// <summary> The base comparer used to compare the strings. </summary>
    public IComparer<ReadOnlySpan<char>> BaseComparer
        => baseComparer;

    /// <inheritdoc/>
    public int Compare(IFileSystemNode? x, IFileSystemNode? y)
    {
        if (ReferenceEquals(x, y))
            return 0;
        if (y is null)
            return 1;
        if (x is null)
            return -1;

        return baseComparer.Compare(x.Name, y.Name);
    }
}

/// <summary> The default comparer used when no other comparer is specified for a file system. See <see cref="StringComparison.OrdinalIgnoreCase"/>. </summary>
internal sealed class OrdinalSpanComparer : IComparer<ReadOnlySpan<char>>
{
    /// <inheritdoc/>
    public int Compare(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
        => x.CompareTo(y, StringComparison.OrdinalIgnoreCase);
}
