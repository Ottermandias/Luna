namespace Luna;

/// <summary> A read-only interface representing an arbitrary node in the file system. </summary>
public interface IFileSystemNode
{
    /// <summary> The parent node of this node, which is set unless this node is a Root. </summary>
    public IFileSystemFolder? Parent { get; }

    /// <summary> The full path of this node. </summary>
    public string FullPath { get; }

    /// <summary> The name of this node, i.e. the part of the <see cref="FullPath"/> after the last forward slash. </summary>
    public ReadOnlySpan<char> Name { get; }

    /// <summary> The depth of this node in the file system, i.e. the number of non-root parents. </summary>
    /// <remarks> Direct children of the root have depth 0, while the root itself has depth <see cref="FileSystemNode.RootDepth"/>. </remarks>
    public int Depth { get; }

    /// <summary> The index of this node in the unordered children of its <see cref="Parent"/>. </summary>
    public int IndexInParent { get; }

    /// <summary> Whether this node is the root node. </summary>
    public bool IsRoot
        => Parent is null;

    /// <summary> An arbitrary runtime identifier for this node that is unique during execution. </summary>
    public FileSystemIdentifier Identifier { get; }

    /// <summary> Get a list of all ancestors of this node up to but excluding the root. </summary>
    /// <remarks> This is ordered from the direct parent to the parent's parent etc. </remarks>
    public IReadOnlyList<IFileSystemFolder> GetAncestors();

    /// <summary> Whether this node is locked. The meaning of this depends on the view implementation. </summary>
    public bool Locked { get; }

    /// <summary> Whether this node is selected. The meaning of this depends on the view implementation. </summary>
    public bool Selected { get; }
}
