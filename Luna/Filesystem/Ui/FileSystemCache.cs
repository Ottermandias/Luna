namespace Luna;

public abstract class FileSystemCache : BasicCache
{
    public          bool             FilterDirty { get; protected set; } = true;
    public readonly FileSystemDrawer Parent;

    public BaseFileSystem FileSystem
        => Parent.FileSystem;

    protected readonly Dictionary<IFileSystemNode, IFileSystemNodeCache> AllNodes          = [];
    protected readonly List<IFlattenedTreeNode>                          VisibleNodes      = [];
    internal readonly  HashSet<IFileSystemNode>                          DraggedNodes      = [];
    internal           StringU8                                          DraggedNodeString = StringU8.Empty;
    internal           IFileSystemNode?                                  DraggedNode;

    public IReadOnlyList<IFlattenedTreeNode> Visible
        => VisibleNodes;

    public FileSystemCache(FileSystemDrawer drawer)
    {
        Parent = drawer;
        FileSystem.Changed.Subscribe(OnFileSystemChange, 0);
        Parent.SortModeChanged += OnSortModeChanged;
    }

    public abstract void Draw();

    public Vector4 LineColor { get; set; } = Vector4.One;

    internal sealed class FileSystemTreeNode(FileSystemCache cache, IFileSystemNode parentNode, IFileSystemNodeCache nodeData)
        : IFlattenedTreeNode
    {
        public int ParentIndex      { get; set; }
        public int StartsLineTo     { get; set; }
        public int IndentationDepth { get; set; }

        public void Draw()
        {
            if (nodeData.Dirty)
            {
                nodeData.Update(cache, parentNode);
                nodeData.Dirty = false;
            }

            if (nodeData.Draw(cache, parentNode))
            { }
        }
    }

    protected virtual IFileSystemNodeCache ConvertNodeInternal(in IFileSystemNode node)
    {
        if (node is IFileSystemFolder)
            return new FileSystemFolderCache();

        return ConvertNode(node);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        ClearNodes();
        FileSystem.Changed.Unsubscribe(OnFileSystemChange);
        Parent.SortModeChanged -= OnSortModeChanged;
    }

    protected virtual void OnFileSystemChange(in FileSystemChanged.Arguments arguments)
    {
        switch (arguments.Type)
        {
            case FileSystemChangeType.ObjectRenamed:
            case FileSystemChangeType.FolderAdded:
            case FileSystemChangeType.DataAdded:
            case FileSystemChangeType.ObjectMoved:
            case FileSystemChangeType.LockedChange:
            case FileSystemChangeType.ExpandedChange:
                if (!AllNodes.TryGetValue(arguments.ChangedObject, out var node))
                    AllNodes.TryAdd(arguments.ChangedObject, ConvertNodeInternal(arguments.ChangedObject));
                else
                    node.Dirty = true;
                break;
            case FileSystemChangeType.FolderMerged:
            case FileSystemChangeType.ObjectRemoved:
                if (AllNodes.Remove(arguments.ChangedObject, out var value))
                    (value as IDisposable)?.Dispose();
                break;
            case FileSystemChangeType.ReloadStarting: ClearNodes(); break;
            case FileSystemChangeType.Reload:         InitializeNodes(); break;
        }

        FilterDirty = true;
    }

    protected abstract IFileSystemNodeCache ConvertNode(in IFileSystemNode node);

    protected virtual void ClearNodes()
    {
        foreach (var node in AllNodes.Values)
            // ReSharper disable once SuspiciousTypeConversion.Global
            (node as IDisposable)?.Dispose();

        AllNodes.Clear();
    }

    protected void InitializeNodes()
    {
        foreach (var node in FileSystem.Root.GetDescendants())
            AllNodes[node] = ConvertNodeInternal(node);
    }

    private void OnSortModeChanged()
        => FilterDirty = true;

    public void ClearDragDrop()
    {
        DraggedNode = null;
        DraggedNodes.Clear();
        DraggedNodeString = StringU8.Empty;
    }

    public virtual void SetDragDrop(IFileSystemNode node)
    {
        DraggedNode = node;
        DraggedNodes.Clear();
        // TODO: Get selected
        DraggedNodes.Add(node);
        DraggedNodes.RemoveWhere(n => n.GetAncestors().Any(DraggedNodes.Contains));
        DraggedNodeString = DraggedNodes.Count is 1
            ? new StringU8($"Moving {DraggedNodes.First().FullPath}...")
            : new StringU8($"Moving ...\n\t - {StringU8.Join("\n\t"u8, DraggedNodes.Select(n => n.FullPath))}");
    }
}

/// <summary> The base class for file system drawer caches. </summary>
/// <typeparam name="TData"> The converted node to draw the tree. </typeparam>
/// <remarks> Does not use the filter cache base type because it behaves quite differently from regular filtered caches. </remarks>
public abstract class FileSystemCache<TData> : FileSystemCache
    where TData : IFileSystemNodeCache
{
    public new FileSystemDrawer<TData> Parent
        => (FileSystemDrawer<TData>)base.Parent;

    public IFilter<TData> Filter
        => Parent.Header.Filter;

    public FileSystemCache(FileSystemDrawer<TData> parent)
        : base(parent)
    {
        Filter.FilterChanged += OnFilterChanged;
        InitializeNodes();
    }

    public override void Draw()
    {
        UpdateTreeList();
        using var style = ImStyleDouble.FramePadding.PushX(Im.Style.GlobalScale)
            .PushY(ImStyleDouble.ItemSpacing, Im.Style.GlobalScale)
            .Push(ImStyleSingle.IndentSpacing, 14 * Im.Style.GlobalScale);
        TreeLine.Draw(VisibleNodes, LineColor);
    }

    protected virtual bool UpdateTreeList()
    {
        if (!FilterDirty)
            return false;

        VisibleNodes.Clear();
        foreach (var node in FileSystem.Root.GetChildren(Parent.SortMode))
            AddNode(node, -1, 0);

        FilterDirty = false;
        return true;

        void AddNode(IFileSystemNode node, int parentIndex, int currentDepth)
        {
            if (!AllNodes.TryGetValue(node, out var cache))
            {
                cache = ConvertNodeInternal(node);
                AllNodes.Add(node, cache);
            }

            var data = new FileSystemTreeNode(this, node, cache)
            {
                IndentationDepth = currentDepth,
                ParentIndex      = parentIndex,
            };
            if (node is not IFileSystemFolder { Expanded: true } folder)
            {
                data.StartsLineTo = -1;
                VisibleNodes.Add(data);
                return;
            }

            var index = VisibleNodes.Count;
            VisibleNodes.Add(data);
            foreach (var child in folder.GetChildren(Parent.SortMode))
                AddNode(child, index, currentDepth + 1);
            data.StartsLineTo = VisibleNodes.Count > index + 1 ? VisibleNodes.Count - 1 : -1;
        }
    }

    private void OnFilterChanged()
        => FilterDirty = true;

    protected override void ClearNodes()
    {
        if (typeof(TData).IsAssignableTo(typeof(IDisposable)))
            foreach (var node in AllNodes.Values)
                // ReSharper disable once SuspiciousTypeConversion.Global
                ((IDisposable)node).Dispose();

        AllNodes.Clear();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Filter.FilterChanged -= OnFilterChanged;
    }
}
