namespace Luna;

/// <summary> The base class for file system nodes. </summary>
/// <param name="identifier"> The runtime identifier of this node. </param>
internal abstract class FileSystemNode(FileSystemIdentifier identifier) : IFileSystemNode
{
    /// <summary> The depth that signals a root node. </summary>
    public const byte RootDepth = byte.MaxValue;

    /// <summary> The parent folder of this node. This is always set except for the root object created by <see cref="CreateRoot"/>. </summary>
    public FileSystemFolder? Parent { get; internal set; }

    /// <summary> Flags that contain information about this node. </summary>
    public PathFlags Flags { get; internal set; }

    /// <summary> The runtime identifier of this node. </summary>
    public FileSystemIdentifier Identifier { get; } = identifier;

    /// <inheritdoc/>
    IReadOnlyList<IFileSystemFolder> IFileSystemNode.GetAncestors()
        => GetAncestors();

    /// <inheritdoc/>
    public bool Locked
    {
        get => Flags.HasFlag(PathFlags.Locked);
        set => Flags = value ? Flags | PathFlags.Locked : Flags & ~PathFlags.Locked;
    }

    /// <inheritdoc/>
    IFileSystemFolder? IFileSystemNode.Parent
        => Parent;

    /// <inheritdoc/>
    public abstract string FullPath { get; internal set; }

    /// <summary> The offset into the name part of the full path.  </summary>
    internal int NameOffset;

    /// <inheritdoc/>
    public ReadOnlySpan<char> Name
        => FullPath.AsSpan(NameOffset);

    /// <inheritdoc/>
    public int Depth { get; private set; }

    /// <inheritdoc/>
    public int IndexInParent { get; private set; }

    /// <inheritdoc/>
    public bool IsRoot
        => Depth is RootDepth;

    /// <summary> Set this node to be locked or not. </summary>
    /// <param name="value"> Whether the node should be locked or not. </param>
    internal void SetLocked(bool value)
        => Flags = value ? Flags | PathFlags.Locked : Flags & ~PathFlags.Locked;

    /// <summary> Update the depth of this node according to its current parent. </summary>
    internal virtual void UpdateDepth()
    {
        if (Parent is null)
            Depth = RootDepth;
        else
            Depth = unchecked((byte)(Parent.Depth + 1));
    }

    /// <summary> Update the index of this node inside its parent. </summary>
    /// <param name="index"> If the index is already known, it can be passed here. Otherwise, pass a negative value and it is computed. </param>
    internal void UpdateIndex(int index)
    {
        if (Parent is null)
            index = 0;
        else if (index < 0)
            index = Parent.Children.IndexOf(this);
        IndexInParent = (ushort)(index < 0 ? 0 : index);
    }

    /// <inheritdoc/>
    public override string ToString()
        => FullPath;

    /// <summary> Creates the specific root element. </summary>
    /// <remarks> The name is set to empty due to it being fixed in the constructor. </remarks>
    internal static FileSystemFolder CreateRoot()
        => new(FileSystemIdentifier.Zero)
        {
            FullPath   = string.Empty,
            Depth      = RootDepth,
            NameOffset = 0,
        };

    /// <inheritdoc cref="IFileSystemNode.GetAncestors"/>
    public FileSystemFolder[] GetAncestors()
    {
        // Skip the root node.
        if (Parent is not { } parent || parent.IsRoot)
            return [];

        var ret = new FileSystemFolder[Depth];
        for (var i = Depth - 1; i >= 0; i--)
        {
            ret[i] = parent!;
            parent = parent!.Parent;
        }

        return ret;
    }
}
