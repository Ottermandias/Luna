namespace Luna;

/// <summary> A folder in the file system with an arbitrary amount of children. </summary>
/// <param name="identifier"> The identifier ID of this folder. </param>
internal sealed class FileSystemFolder(FileSystemIdentifier identifier)
    : FileSystemNode(identifier), IFileSystemFolder
{
    /// <summary> The total number of descendants of this folder. </summary>
    public int TotalDescendants { get; internal set; }

    /// <summary> The total number of nodes containing data that descend from this folder. </summary>
    public int TotalDataNodes { get; internal set; }

    /// <summary> All direct children of this folder. </summary>
    internal readonly List<FileSystemNode> Children = [];

    /// <summary> Whether this folder is marked as expanded/open. </summary>
    public bool IsExpanded
        => Flags.HasFlag(PathFlags.Expanded);

    /// <inheritdoc/>
    IReadOnlyList<IFileSystemNode> IFileSystemFolder.Children
        => Children;

    /// <summary> Get all direct children of this folder that are also folders. </summary>
    internal IEnumerable<FileSystemFolder> GetSubFolders()
        => Children.OfType<FileSystemFolder>();

    /// <summary> Get all children of this folder that contain the specified data type. </summary>
    internal IEnumerable<FileSystemData<T>> GetDataNodes<T>() where T : class, IFileSystemValue<T>
        => Children.OfType<FileSystemData<T>>();

    /// <summary> Get all children of this folder that contain data. </summary>
    internal IEnumerable<IFileSystemData> GetDataNodes()
        => Children.OfType<IFileSystemData>();

    /// <inheritdoc/>
    public override string FullPath { get; internal set; } = string.Empty;

    /// <summary> Update the depth of this folder according to its parent. Also updates all descendants on a change of its own depth. </summary>
    internal override void UpdateDepth()
    {
        var newDepth = Parent is null ? RootDepth : unchecked((byte)(Parent.Depth + 1));
        if (newDepth == Depth)
            return;

        // Also update all descendants.
        foreach (var desc in GetDescendants())
            desc.UpdateDepth();
    }

    /// <inheritdoc/>
    public IEnumerable<IFileSystemNode> GetChildren(ISortMode mode)
        => mode.GetChildren(this);

    /// <inheritdoc/>
    public IEnumerable<IFileSystemNode> GetAllDescendants(ISortMode mode)
    {
        return GetChildren(mode).SelectMany(p => p is FileSystemFolder f
            ? f.GetAllDescendants(mode).Prepend(f)
            : [p]);
    }

    /// <summary> Get all descendants as writeable types without specific ordering. </summary>
    /// <remarks> This does not include the folder itself. </remarks>
    internal IEnumerable<FileSystemNode> GetDescendants()
    {
        return Children.SelectMany(p => p is FileSystemFolder f
            ? f.GetDescendants().Prepend(f)
            : [p]);
    }
}
