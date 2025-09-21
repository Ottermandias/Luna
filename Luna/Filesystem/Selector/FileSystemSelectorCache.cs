namespace Luna;

public interface IFileSystemCacheNode<out TSelf> : IFlattenedTreeNode
    where TSelf : IFileSystemCacheNode<TSelf>
{
    public IFileSystemNode Node { get; }

    public bool Selected         { get; set; }
    public int  ParentIndex      { get; set; }
    public int  StartsLineTo     { get; set; }
    public int  IndentationDepth { get; set; }

    int IFlattenedTreeNode.ParentIndex
        => ParentIndex;

    int IFlattenedTreeNode.StartsLineTo
        => StartsLineTo;

    int IFlattenedTreeNode.IndentationDepth
        => IndentationDepth;

    public abstract static TSelf Create(IFileSystemNode node);
}

public abstract class FileSystemCacheNodeBase(IFileSystemNode node)
{
    public IFileSystemNode Node             { get; } = node;
    public bool            Selected         { get; set; }
    public int             ParentIndex      { get; set; }
    public int             StartsLineTo     { get; set; }
    public int             IndentationDepth { get; set; }
}

public class FileSystemSelectorCache<TCacheNode> : FilterCache<TCacheNode>
    where TCacheNode : IFileSystemCacheNode<TCacheNode>
{
    public           bool                SortExpansionDirty { get; private set; } = false;
    public readonly  IFilter<TCacheNode> Filter;
    private readonly BaseFileSystem      _fileSystem;
    private readonly FileSystemSelection _selection;

    public FileSystemSelectorCache(BaseFileSystem fileSystem, FileSystemSelection selection, IFilter<TCacheNode> filter)
    {
        _fileSystem = fileSystem;
        _selection  = selection;
        Filter      = filter;
        _fileSystem.Changed.Subscribe(OnFileSystemChange, uint.MinValue);
        _selection.Changed.Subscribe(OnSelectionChange, uint.MaxValue);
        Filter.FilterChanged += OnFilterChanged;
    }

    private void OnFilterChanged()
        => FilterDirty = true;

    private void OnSelectionChange(in FileSystemSelection.SelectionChangedEvent.Arguments arguments)
    { }

    private void OnFileSystemChange(in FileSystemChanged.Arguments arguments)
    {
        switch (arguments.Type)
        {
            case FileSystemChangeType.ObjectMoved:
                // TODO: delayed action to expand ancestors then set filter dirty.
                break;
        }

        Dirty |= IManagedCache.DirtyFlags.Custom;
    }

    public virtual ISortMode SortMode
        => ISortMode.FoldersFirst;

    protected override bool WouldBeVisible(in TCacheNode item, int globalIndex)
        => Filter.WouldBeVisible(item, globalIndex);

    protected override IEnumerable<TCacheNode> GetItems()
    {
        var ret = new List<TCacheNode>(_fileSystem.Root.TotalDescendants);

        void AddNode()
        foreach (var node in)
            IFlattenedTreeNode? lastNode;
        var currentParent = -1;
        foreach (var node in _fileSystem.Root.GetAllDescendants(SortMode))
        {
            var cache = TCacheNode.Create(node);
            cache.IndentationDepth = node.Depth;
            cache.ParentIndex      = currentParent;
            cache.Selected         = _selection.IsSelected(node);
            if (node is IFileSystemFolder folder)
            {
                if (!folder.IsExpanded)
                {
                    cache.StartsLineTo = -1;
                }
                else
                { }
            }
            else
            { }
        }
    }

    protected override void Dispose(bool disposing)
    {
        _fileSystem.Changed.Unsubscribe(OnFileSystemChange);
        _selection.Changed.Unsubscribe(OnSelectionChange);
        Filter.FilterChanged -= OnFilterChanged;
    }
}
