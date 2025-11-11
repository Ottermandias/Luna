namespace Luna;

/// <summary> An interface for different sort modes that can be used with a file system. </summary>
public interface ISortMode : IEquatable<ISortMode>
{
    /// <summary> The display name of the sort mode for combo selection. </summary>
    public ReadOnlySpan<byte> Name { get; }

    /// <summary> The description of the sort mode for combo tooltips. </summary>
    public ReadOnlySpan<byte> Description { get; }

    /// <summary> The method the sort mode uses to get the children of a folder in its specific order. </summary>
    /// <param name="folder"> The folder to fetch children from. </param>
    /// <returns> The children of <paramref name="folder"/> ordered according to this sort mode. </returns>
    public IEnumerable<IFileSystemNode> GetChildren(IFileSystemFolder folder);

    /// <summary> See <see cref="FoldersFirstT.Description"/>. </summary>
    public static readonly ISortMode FoldersFirst = new FoldersFirstT();

    /// <summary> See <see cref="LexicographicalT.Description"/>. </summary>
    public static readonly ISortMode Lexicographical = new LexicographicalT();

    /// <summary> See <see cref="InverseFoldersFirstT.Description"/>. </summary>
    public static readonly ISortMode InverseFoldersFirst = new InverseFoldersFirstT();

    /// <summary> See <see cref="InverseLexicographicalT.Description"/>. </summary>
    public static readonly ISortMode InverseLexicographical = new InverseLexicographicalT();

    /// <summary> See <see cref="FoldersLastT.Description"/>. </summary>
    public static readonly ISortMode FoldersLast = new FoldersLastT();

    /// <summary> See <see cref="InverseFoldersLastT.Description"/>. </summary>
    public static readonly ISortMode InverseFoldersLast = new InverseFoldersLastT();

    /// <summary> See <see cref="InternalOrderT.Description"/>. </summary>
    public static readonly ISortMode InternalOrder = new InternalOrderT();

    /// <summary> See <see cref="InverseInternalOrderT.Description"/>. </summary>
    public static readonly ISortMode InverseInternalOrder = new InverseInternalOrderT();

    /// <inheritdoc/>
    bool IEquatable<ISortMode>.Equals(ISortMode? other)
        => GetType() == other?.GetType();

    private struct FoldersFirstT : ISortMode
    {
        public ReadOnlySpan<byte> Name
            => "Folders First"u8;

        public ReadOnlySpan<byte> Description
            => "In each folder, sort all subfolders lexicographically, then sort all data nodes lexicographically."u8;

        public IEnumerable<IFileSystemNode> GetChildren(IFileSystemFolder folder)
            => folder.GetSubFolders().Cast<IFileSystemNode>().Concat(folder.GetLeaves());
    }

    private struct LexicographicalT : ISortMode
    {
        public ReadOnlySpan<byte> Name
            => "Lexicographical"u8;

        public ReadOnlySpan<byte> Description
            => "In each folder, sort all children lexicographically."u8;

        public IEnumerable<IFileSystemNode> GetChildren(IFileSystemFolder folder)
            => folder.Children;
    }

    private struct InverseFoldersFirstT : ISortMode
    {
        public ReadOnlySpan<byte> Name
            => "Folders First (Inverted)"u8;

        public ReadOnlySpan<byte> Description
            => "In each folder, sort all subfolders in inverse lexicographical order, then sort all leaves in inverse lexicographical order."u8;

        public IEnumerable<IFileSystemNode> GetChildren(IFileSystemFolder folder)
            => folder.GetSubFolders().Cast<IFileSystemNode>().Reverse().Concat(folder.GetLeaves().Reverse());
    }

    public struct InverseLexicographicalT : ISortMode
    {
        public ReadOnlySpan<byte> Name
            => "Lexicographical (Inverted)"u8;

        public ReadOnlySpan<byte> Description
            => "In each folder, sort all children in inverse lexicographical order."u8;

        public IEnumerable<IFileSystemNode> GetChildren(IFileSystemFolder folder)
            => folder.Children.Reverse();
    }

    public struct FoldersLastT : ISortMode
    {
        public ReadOnlySpan<byte> Name
            => "Folders Last"u8;

        public ReadOnlySpan<byte> Description
            => "In each folder, sort all leaves lexicographically, then sort all subfolders lexicographically."u8;

        public IEnumerable<IFileSystemNode> GetChildren(IFileSystemFolder folder)
            => folder.GetLeaves().Cast<IFileSystemNode>().Concat(folder.GetSubFolders());
    }

    public struct InverseFoldersLastT : ISortMode
    {
        public ReadOnlySpan<byte> Name
            => "Folders Last (Inverted)"u8;

        public ReadOnlySpan<byte> Description
            => "In each folder, sort all leaves in inverse lexicographical order, then sort all subfolders in inverse lexicographical order."u8;

        public IEnumerable<IFileSystemNode> GetChildren(IFileSystemFolder folder)
            => folder.GetLeaves().Cast<IFileSystemNode>().Reverse().Concat(folder.GetSubFolders().Reverse());
    }

    public struct InternalOrderT : ISortMode
    {
        public ReadOnlySpan<byte> Name
            => "Internal Order"u8;

        public ReadOnlySpan<byte> Description
            => "In each folder, sort all children in order of their identifiers (i.e. in order of their creation in the filesystem)."u8;

        public IEnumerable<IFileSystemNode> GetChildren(IFileSystemFolder folder)
            => folder.Children.OrderBy(c => c.Identifier);
    }

    public struct InverseInternalOrderT : ISortMode
    {
        public ReadOnlySpan<byte> Name
            => "Internal Order (Inverted)"u8;

        public ReadOnlySpan<byte> Description
            => "In each folder, sort all children in inverse order of their identifiers (i.e. in inverse order of their creation in the filesystem)."u8;

        public IEnumerable<IFileSystemNode> GetChildren(IFileSystemFolder folder)
            => folder.Children.OrderByDescending(c => c.Identifier);
    }
}
