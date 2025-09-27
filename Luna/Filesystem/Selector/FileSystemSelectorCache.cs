namespace Luna;

public abstract class FileSystemSelectorCache<TCacheNode> : BasicCache
    where TCacheNode : FileSystemCacheNodeBase<TCacheNode>
{
    public readonly  IFilter<TCacheNode> Filter;
    private readonly BaseFileSystem      _fileSystem;
    private readonly FileSystemSelection _selection;
    private          TCacheNode          _root     = null!;
    private readonly List<TCacheNode>    _flatList = [];

    public IReadOnlyList<TCacheNode> FlatList
        => _flatList;

    public bool FlatListDirty { get; private set; } = true;

    public ISortMode SortMode
    {
        get;
        set
        {
            if (ReferenceEquals(field, value))
                return;

            field         = value;
            FlatListDirty = true;
        }
    } = ISortMode.FoldersFirst;

    public FileSystemSelectorCache(BaseFileSystem fileSystem, FileSystemSelection selection, IFilter<TCacheNode> filter)
    {
        _fileSystem = fileSystem;
        _selection  = selection;
        Filter      = filter;
        _fileSystem.Changed.Subscribe(OnFileSystemChange, uint.MinValue);
        _selection.Changed.Subscribe(OnSelectionChange, uint.MaxValue);
        Filter.FilterChanged += OnFilterChanged;
    }

    public override void Update()
    {
        DataUpdate();
        FlatListUpdate();
        Dirty = IManagedCache.DirtyFlags.Clean;
    }

    protected virtual void DataUpdate()
    {
        if (!CustomDirty)
            return;

        Dirty         &= IManagedCache.DirtyFlags.Custom;
        _root         =  ConvertNode(_fileSystem.Root);
        FlatListDirty =  true;
    }

    protected virtual void FlatListUpdate()
    {
        if (!FlatListDirty)
            return;

        _flatList.Clear();
        FlatListDirty = false;
        foreach (var child in _root.Children!)
            AddNode(child, -1, 0);
        return;

        void AddNode(TCacheNode node, int parentIndex, int depth)
        {
            node.IndentationDepth = depth;
            node.ParentIndex      = parentIndex;

            var index = _flatList.Count;
            var added = false;
            if (Filter.WouldBeVisible(node, -1))
            {
                _flatList.Add(node);
                added = true;
            }

            if (node is { Children: { } children, Expanded: true })
            {
                if (!added)
                    _flatList.Add(node);

                foreach (var child in children)
                    AddNode(child, index, depth + 1);

                if (!added && _flatList.Count == index)
                    _flatList.RemoveAt(index);
                else
                    node.StartsLineTo = _flatList.Count - 1;
            }
        }
    }


    protected abstract TCacheNode ConvertData(IFileSystemNode data);


    protected virtual TCacheNode ConvertNode(IFileSystemNode node)
    {
        var ret = ConvertData(node);
        ret.Children = node switch
        {
            IFileSystemFolder folder => folder.Children.Select(ConvertData).ToArray(),
            _                        => throw new ArgumentException("Node is neither folder nor data.", nameof(node)),
        };

        if (_selection.IsSelected(node))
            ret.Selected = true;
        return ret;
    }


    protected override void Dispose(bool disposing)
    {
        _fileSystem.Changed.Unsubscribe(OnFileSystemChange);
        _selection.Changed.Unsubscribe(OnSelectionChange);
        Filter.FilterChanged -= OnFilterChanged;
    }


    private void OnFilterChanged()
        => FlatListDirty = true;

    private void OnSelectionChange(in FileSystemSelection.SelectionChangedEvent.Arguments arguments)
    {
        if (arguments.Added is { } added && _root.GetDescendants().First(n => ReferenceEquals(added, n.Node)) is { } addedCache)
            addedCache.Selected = true;
        if (arguments.Removed is { } removed && _root.GetDescendants().First(n => ReferenceEquals(removed, n.Node)) is { } removedCache)
            removedCache.Selected = false;
    }

    private void OnFileSystemChange(in FileSystemChanged.Arguments arguments)
    {
        switch (arguments.Type)
        {
            case FileSystemChangeType.ObjectRenamed:
            case FileSystemChangeType.ObjectRemoved:
            case FileSystemChangeType.FolderAdded:
            case FileSystemChangeType.DataAdded:
            case FileSystemChangeType.FolderMerged:
            case FileSystemChangeType.PartialMerge:
            case FileSystemChangeType.Reload:
                Dirty |= IManagedCache.DirtyFlags.Custom;
                break;
            case FileSystemChangeType.ReloadStarting:
            case FileSystemChangeType.LockedChange:
                // Nothing, not cached.
                break;
            case FileSystemChangeType.ExpandedChange:
                // 
                FlatListDirty = true;
                break;
        }
    }
}
