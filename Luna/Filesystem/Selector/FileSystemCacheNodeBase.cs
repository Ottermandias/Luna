namespace Luna;

public abstract class FileSystemCacheNodeBase<TSelf>(IFileSystemNode node) : IFlattenedTreeNode
    where TSelf : FileSystemCacheNodeBase<TSelf>
{
    public IFileSystemNode Node             { get; } = node;
    public TSelf[]?        Children         { get; set; }
    public StringU8        Label            { get; set; } = StringU8.Empty;
    public int             ParentIndex      { get; set; }
    public int             IndentationDepth { get; set; }
    public int             StartsLineTo     { get; set; }

    public bool Expanded
        => Node is IFileSystemFolder { Expanded: true };

    public bool Locked
        => Node.Locked;

    public bool Selected { get; set; }

    public virtual void Draw()
    {
        var flags = TreeNodeFlags.NoTreePushOnOpen;
        if (Children is not null)
            Im.Tree.SetNextOpen(Expanded, Condition.Always);
        else
            flags |= TreeNodeFlags.Leaf;
        Im.Tree.Node(Label, flags);
    }

    int IFlattenedTreeNode.ParentIndex
        => ParentIndex;

    int IFlattenedTreeNode.StartsLineTo
        => StartsLineTo;

    int IFlattenedTreeNode.IndentationDepth
        => IndentationDepth;

    public IEnumerable<TSelf> GetDescendants()
        => Children?.SelectMany(c => c.GetDescendants().Prepend(c)) ?? [];
}
