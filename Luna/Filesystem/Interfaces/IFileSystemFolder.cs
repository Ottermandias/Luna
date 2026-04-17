namespace Luna;

/// <summary> A read-only interface representing a folder in the file system. </summary>
public interface IFileSystemFolder : IFileSystemNode
{
    /// <summary> Get whether the folder is currently expanded/open. </summary>
    public bool Expanded { get; }

    /// <summary> Get or set whether the folder is temporarily expanded. This is not saved across sessions and does not incur events. </summary>
    public bool FilterExpanded { get; set; }

    /// <summary> The total number of descendants of this folder. </summary>
    public int TotalDescendants { get; }

    /// <summary> The total number of nodes containing data that descend from this folder. </summary>
    public int TotalDataNodes { get; }

    /// <summary> A specific color for this folder overwriting the default expanded color. </summary>
    public ColorParameter ExpandedColor { get; }

    /// <summary> A specific color for this folder overwriting the default collapsed color. </summary>
    public ColorParameter CollapsedColor { get; }

    /// <summary> A specific sort mode for this folder, overwriting the default sort mode. This does not apply to descendant folders. </summary>
    public ISortMode? SortMode { get; }

    /// <summary> Get the direct children of this folder. </summary>
    public IReadOnlyList<IFileSystemNode> Children { get; }

    /// <summary> Get only those direct children that are folders themselves. </summary>
    public IEnumerable<IFileSystemFolder> GetSubFolders()
        => Children.OfType<IFileSystemFolder>();

    /// <summary> Get only those direct children that are not folders. </summary>
    public IEnumerable<IFileSystemData> GetLeaves()
        => Children.OfType<IFileSystemData>();

    /// <summary> Get all descendants of this folder. </summary>
    public IEnumerable<IFileSystemNode> GetDescendants()
        => Children.SelectMany(c => c is IFileSystemFolder folder
            ? folder.GetDescendants().Prepend(c)
            : [c]);

    /// <summary> Get all children of this folder according to the given sort mode. </summary>
    /// <param name="mode"> The sort mode to use. </param>
    /// <returns> The direct children of this folder ordered by the sort mode. </returns>
    public IEnumerable<IFileSystemNode> GetChildren(ISortMode mode);

    /// <summary> Get all descendants of this folder according to the given sort mode. </summary>
    /// <param name="mode"> The sort mode to use. </param>
    /// <returns> All descendants of this folder ordered by the sort mode. </returns>
    /// <remarks> This does not include the folder itself. </remarks>
    public IEnumerable<IFileSystemNode> GetAllDescendants(ISortMode mode);
}
