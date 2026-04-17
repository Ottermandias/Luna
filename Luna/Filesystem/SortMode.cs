// ReSharper disable MemberHidesStaticFromOuterClass

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

    /// <summary> See <see cref="Types.FoldersFirst.Description"/>. </summary>
    public static readonly ISortMode FoldersFirst = new Types.FoldersFirst();

    /// <summary> See <see cref="Types.Lexicographical.Description"/>. </summary>
    public static readonly ISortMode Lexicographical = new Types.Lexicographical();

    /// <summary> See <see cref="Types.InverseFoldersFirst.Description"/>. </summary>
    public static readonly ISortMode InverseFoldersFirst = new Types.InverseFoldersFirst();

    /// <summary> See <see cref="Types.InverseLexicographical.Description"/>. </summary>
    public static readonly ISortMode InverseLexicographical = new Types.InverseLexicographical();

    /// <summary> See <see cref="Types.FoldersLast.Description"/>. </summary>
    public static readonly ISortMode FoldersLast = new Types.FoldersLast();

    /// <summary> See <see cref="Types.InverseFoldersLast.Description"/>. </summary>
    public static readonly ISortMode InverseFoldersLast = new Types.InverseFoldersLast();

    /// <summary> See <see cref="Types.InternalOrder.Description"/>. </summary>
    public static readonly ISortMode InternalOrder = new Types.InternalOrder();

    /// <summary> See <see cref="Types.InverseInternalOrder.Description"/>. </summary>
    public static readonly ISortMode InverseInternalOrder = new Types.InverseInternalOrder();

    /// <inheritdoc/>
    bool IEquatable<ISortMode>.Equals(ISortMode? other)
        => Equals(this, other);

    /// <summary> Whether two sort modes are equal. </summary>
    public static bool Equals(ISortMode? lhs, ISortMode? rhs)
        => lhs is null ? rhs is null : rhs is not null && lhs.GetType() == rhs.GetType();

    private static class Types
    {
        public struct FoldersFirst : ISortMode
        {
            public ReadOnlySpan<byte> Name
                => "Folders First"u8;

            public ReadOnlySpan<byte> Description
                => "In each folder, sort all subfolders lexicographically, then sort all data nodes lexicographically."u8;

            public IEnumerable<IFileSystemNode> GetChildren(IFileSystemFolder folder)
                => GetFolderLike(folder).Concat(GetLeaveLike(folder));
        }

        public struct Lexicographical : ISortMode
        {
            public ReadOnlySpan<byte> Name
                => "Lexicographical"u8;

            public ReadOnlySpan<byte> Description
                => "In each folder, sort all children lexicographically."u8;

            public IEnumerable<IFileSystemNode> GetChildren(IFileSystemFolder folder)
                => folder.Children;
        }

        public struct InverseFoldersFirst : ISortMode
        {
            public ReadOnlySpan<byte> Name
                => "Folders First (Inverted)"u8;

            public ReadOnlySpan<byte> Description
                => "In each folder, sort all subfolders in inverse lexicographical order, then sort all leaves in inverse lexicographical order."u8;

            public IEnumerable<IFileSystemNode> GetChildren(IFileSystemFolder folder)
                => GetFolderLike(folder).Reverse().Concat(GetLeaveLike(folder)).Reverse();
        }

        public struct InverseLexicographical : ISortMode
        {
            public ReadOnlySpan<byte> Name
                => "Lexicographical (Inverted)"u8;

            public ReadOnlySpan<byte> Description
                => "In each folder, sort all children in inverse lexicographical order."u8;

            public IEnumerable<IFileSystemNode> GetChildren(IFileSystemFolder folder)
                => folder.Children.Reverse();
        }

        public struct FoldersLast : ISortMode
        {
            public ReadOnlySpan<byte> Name
                => "Folders Last"u8;

            public ReadOnlySpan<byte> Description
                => "In each folder, sort all leaves lexicographically, then sort all subfolders lexicographically."u8;

            public IEnumerable<IFileSystemNode> GetChildren(IFileSystemFolder folder)
                => GetLeaveLike(folder).Concat(GetFolderLike(folder));
        }

        public struct InverseFoldersLast : ISortMode
        {
            public ReadOnlySpan<byte> Name
                => "Folders Last (Inverted)"u8;

            public ReadOnlySpan<byte> Description
                => "In each folder, sort all leaves in inverse lexicographical order, then sort all subfolders in inverse lexicographical order."u8;

            public IEnumerable<IFileSystemNode> GetChildren(IFileSystemFolder folder)
                => GetLeaveLike(folder).Reverse().Concat(GetFolderLike(folder)).Reverse();
        }

        public struct InternalOrder : ISortMode
        {
            public ReadOnlySpan<byte> Name
                => "Internal Order"u8;

            public ReadOnlySpan<byte> Description
                => "In each folder, sort all children in order of their identifiers (i.e. in order of their creation in the filesystem)."u8;

            public IEnumerable<IFileSystemNode> GetChildren(IFileSystemFolder folder)
                => folder.Children.OrderBy(c => c.Identifier);
        }

        public struct InverseInternalOrder : ISortMode
        {
            public ReadOnlySpan<byte> Name
                => "Internal Order (Inverted)"u8;

            public ReadOnlySpan<byte> Description
                => "In each folder, sort all children in inverse order of their identifiers (i.e. in inverse order of their creation in the filesystem)."u8;

            public IEnumerable<IFileSystemNode> GetChildren(IFileSystemFolder folder)
                => folder.Children.OrderByDescending(c => c.Identifier);
        }
    }

    /// <summary> Get all children of a folder that behave like leaves. </summary>
    /// <param name="folder"></param>
    /// <returns></returns>
    public static IEnumerable<IFileSystemNode> GetLeaveLike(IFileSystemFolder folder)
        => folder.Children.Where(c => !c.BehavesLikeFolder);

    public static IEnumerable<IFileSystemNode> GetFolderLike(IFileSystemFolder folder)
        => folder.Children.Where(c => c.BehavesLikeFolder);
}
