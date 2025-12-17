namespace Luna;

/// <summary> A cache for drawing a full, filtered file system. </summary>
public abstract class FileSystemCache : BasicCache
{
    /// <summary> Used to keep the dragged node alive for one frame longer so that moving things downwards works. </summary>
    internal static bool KeepDragAlive;

    /// <summary> Whether the list of <see cref="InternalNodes"/> needs to be rebuilt. </summary>
    public bool VisibleDirty { get; protected set; } = true;

    /// <summary> The file system drawer that created this cache. </summary>
    public readonly FileSystemDrawer Parent;

    /// <summary> The file system being drawn. </summary>
    public BaseFileSystem FileSystem
        => Parent.FileSystem;

    /// <inheritdoc cref="VisibleNodes"/>
    private protected readonly List<FileSystemTreeNode> InternalNodes = [];

    /// <summary> The cached data of all file system nodes, used to rebuild the visible list as needed. </summary>
    protected readonly Dictionary<IFileSystemNode, IFileSystemNodeCache> AllNodes = [];

    /// <summary> The list of currently visible nodes in the flattened tree. </summary>
    public IReadOnlyList<IFlattenedTreeNode> VisibleNodes
        => InternalNodes;

    /// <summary> A set of nodes currently being dragged by selection. </summary>
    internal readonly HashSet<IFileSystemNode> DraggedNodes = [];

    /// <summary> The text displayed while nodes are being dragged. </summary>
    internal StringU8 DraggedNodeString = StringU8.Empty;

    /// <summary> The actual node being dragged. </summary>
    internal IFileSystemNode? DraggedNode;

    /// <summary> The color to use for the tree lines. </summary>
    public Vector4 LineColor { get; set; } = Vector4.One;

    /// <summary> The last targeted node for shift-selection operations. </summary>
    protected readonly WeakReference<IFileSystemNode> LastTarget = new(null!);

    /// <summary> Create a new file system cache for the given drawer. </summary>
    /// <param name="drawer"> The parent drawer. </param>
    public FileSystemCache(FileSystemDrawer drawer)
    {
        Parent = drawer;
        FileSystem.Changed.Subscribe(OnFileSystemChange, 0);
        Parent.SortModeChanged += OnSortModeChanged;
    }

    /// <summary> Draw this cache. </summary>
    public abstract void Draw();

    /// <summary> Create a cache entry for the given node. </summary>
    /// <param name="node"> The actual file system node. </param>
    /// <returns> The cached data for the node. </returns>
    protected IFileSystemNodeCache ConvertNodeInternal(in IFileSystemNode node)
    {
        if (node is IFileSystemFolder)
            return new FileSystemFolderCache();

        return ConvertNode(node);
    }

    /// <summary> Reactions to file system changes. </summary>
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
                    // ReSharper disable once SuspiciousTypeConversion.Global
                    (value as IDisposable)?.Dispose();
                break;
            case FileSystemChangeType.ReloadStarting: ClearNodes(); break;
            case FileSystemChangeType.Reload:         InitializeNodes(); break;
        }

        VisibleDirty = true;
    }

    /// <summary> The actual method to convert a file system node into its cached representation. </summary>
    /// <param name="node"> The file system node. </param>
    /// <returns> The cached representation. </returns>
    protected abstract IFileSystemNodeCache ConvertNode(in IFileSystemNode node);

    /// <summary> Clear all currently stored node data. </summary>
    protected virtual void ClearNodes()
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        foreach (var node in AllNodes.Values.OfType<IDisposable>())
            node.Dispose();

        AllNodes.Clear();
    }

    /// <summary> Setup new node caches for all nodes in the file system. </summary>
    protected void InitializeNodes()
    {
        foreach (var node in FileSystem.Root.GetDescendants())
            AllNodes[node] = ConvertNodeInternal(node);
    }

    /// <summary> Changing the sort mode means reconstructing the tree. </summary>
    private void OnSortModeChanged()
        => VisibleDirty = true;

    /// <summary> Reset the stored drag and drop data. </summary>
    public void ClearDragDrop()
    {
        DraggedNode = null;
        DraggedNodes.Clear();
        DraggedNodeString = StringU8.Empty;
    }

    /// <summary> Set the drag and drop data for the given node and current selection. </summary>
    /// <param name="node"> The node being dragged. </param>
    public virtual void SetDragDrop(IFileSystemNode node)
    {
        DraggedNode = node;
        DraggedNodes.Clear();
        DraggedNodes.UnionWith(Parent.FileSystem.Selection.OrderedNodes);
        DraggedNodes.Add(node);
        DraggedNodes.RemoveWhere(n => n.GetAncestors().Any(DraggedNodes.Contains));
        DraggedNodeString = DraggedNodes.Count is 1
            ? new StringU8($"Moving {DraggedNodes.First().FullPath}...")
            : new StringU8($"Moving ...\n\t - {StringU8.Join("\n\t"u8, DraggedNodes.Select(n => n.FullPath))}");
    }

    #region Selection Handling

    /// <summary> Handle selecting the given node, considering modifier keys. </summary>
    /// <param name="node"> The node to check for clicks. </param>
    /// <param name="selectFolders"> Whether folders should be selected for shift-based multi selections or only data nodes. </param>
    public virtual void HandleSelection(IFileSystemNode node, bool selectFolders)
    {
        // We do not want to change selection when dropping items onto some node.
        if (KeepDragAlive || !Im.Mouse.IsReleased(MouseButton.Left) || Im.DragDrop.PeekPayload().Valid || !Im.Item.Hovered())
            return;

        // If no multi selection is allowed, just treat every selection event as a 'Select'.
        // We do not allow selecting folders in that case.
        if (!FileSystem.Selection.AllowsMultiSelection)
        {
            if (node is IFileSystemData data)
                Parent.FileSystem.Selection.Select(data);
            return;
        }

        // Control triggers additive/subtractive selection.
        if (Im.Io.KeyControl)
        {
            // With shift, we either add all nodes between this node and the last selected node or remove them,
            // depending on whether the clicked node is already selected (remove) or not (add).
            if (Im.Io.KeyShift)
            {
                var (startIndex, targetIndex) = GetNodeIndices(node);
                if (node.Selected)
                    foreach (var n in IterateFromIndices(startIndex, targetIndex, selectFolders))
                        Parent.FileSystem.Selection.RemoveFromSelection(n.ParentNode);
                else
                    foreach (var n in IterateFromIndices(startIndex, targetIndex, selectFolders))
                        Parent.FileSystem.Selection.AddToSelection(n.ParentNode);
            }
            // Pure Control means toggling the selection state of the node.
            else
            {
                Parent.FileSystem.Selection.ToggleSelection(node);
            }
        }
        // Shift without Control means removing the prior selection and then selecting
        // all nodes between this node and the last selected node.
        else if (Im.Io.KeyShift)
        {
            var (startIndex, targetIndex) = GetNodeIndices(node);
            if (targetIndex is not -1)
            {
                Parent.FileSystem.Selection.UnselectAll();
                foreach (var n in IterateFromIndices(startIndex, targetIndex, selectFolders))
                    Parent.FileSystem.Selection.AddToSelection(n.ParentNode);
            }
        }
        // Without modifiers, we just remove the prior selection and then select the new node.
        else if (node is IFileSystemData data)
        {
            Parent.FileSystem.Selection.Select(data);
        }

        // Set the last selected node for further shift-modified operations.
        LastTarget.SetTarget(node);
    }

    /// <summary> Iterate over all nodes between the two given indices, in correct order. </summary>
    /// <param name="startIndex"> The first node to iterate from. </param>
    /// <param name="targetIndex"> The last node to iterate to. </param>
    /// <param name="selectFolders"> Whether folders should be selected for shift-based multi selections or only data nodes. </param>
    /// <returns> An enumeration of all nodes between the two indices in correct order. </returns>
    private IEnumerable<FileSystemTreeNode> IterateFromIndices(int startIndex, int targetIndex, bool selectFolders)
    {
        if (targetIndex is -1)
            yield break;

        // Iterate from start to end in correct order since selection order is relevant.
        // Only return items on the same level as target and end, and optionally ignore folders.
        var parentIndex = InternalNodes[targetIndex].ParentIndex;
        if (startIndex <= targetIndex)
            for (var i = startIndex; i <= targetIndex; ++i)
            {
                var node = InternalNodes[i];
                if (node.ParentIndex != parentIndex)
                    continue;

                if (selectFolders || node.ParentNode is not IFileSystemFolder)
                    yield return node;
            }
        else
            for (var i = startIndex; i >= targetIndex; --i)
            {
                var node = InternalNodes[i];
                if (node.ParentIndex != parentIndex)
                    continue;

                if (selectFolders || node.ParentNode is not IFileSystemFolder)
                    yield return node;
            }
    }

    /// <summary> Get the start and target indices for shift-based selection operations for the given node. </summary>
    /// <param name="node"> The node that was clicked, i.e. the target node. </param>
    /// <returns> The indices of the start and target nodes if both are valid for shift-based selection, a negative target index if they are not. </returns>
    private (int StartIndex, int TargetIndex) GetNodeIndices(IFileSystemNode node)
    {
        // We use the last selected node as stored in the cache, or the only currently selected node,
        // or the start of the visible nodes as start point, and the clicked node as target point.
        if (!LastTarget.TryGetTarget(out var start))
            start = Parent.FileSystem.Selection.Selection ?? InternalNodes.FirstOrDefault()?.ParentNode;

        // We currently only support both target and start to have the same parent.
        if (start?.Parent != node.Parent)
            return (-1, -1);

        // Find the indices of the nodes in the visible node list.
        var startIndex  = -1;
        var targetIndex = -1;
        foreach (var (index, visible) in InternalNodes.Index())
        {
            if (visible.ParentNode == start)
                startIndex = index;
            if (visible.ParentNode == node)
                targetIndex = index;

            if (startIndex is not -1 && targetIndex is not -1)
                break;
        }

        // If no starting point was found, treat the target as the start for a single selection.
        if (startIndex is -1)
            startIndex = targetIndex;

        return (startIndex, targetIndex);
    }

    #endregion

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        ClearNodes();
        FileSystem.Changed.Unsubscribe(OnFileSystemChange);
        Parent.SortModeChanged -= OnSortModeChanged;
    }

    /// <summary> The wrapper type that combines a file system node and its cached data for drawing in a flattened tree. </summary>
    /// <param name="cache"> we need to capture the parent cache for updating and drawing. </param>
    /// <param name="parentNode"> The actual file system node. </param>
    /// <param name="nodeData"> The cached data for the node. </param>
    private protected sealed class FileSystemTreeNode(FileSystemCache cache, IFileSystemNode parentNode, IFileSystemNodeCache nodeData)
        : IFlattenedTreeNode
    {
        /// <summary> The parent cache for updating and drawing. </summary>
        public readonly FileSystemCache Parent = cache;

        /// <summary> The actual file system node. </summary>
        public readonly IFileSystemNode ParentNode = parentNode;

        /// <summary> The cached data for the node. </summary>
        public readonly IFileSystemNodeCache NodeData = nodeData;

        /// <inheritdoc/>
        public int ParentIndex { get; set; }

        /// <inheritdoc/>
        public int StartsLineTo { get; set; }

        /// <inheritdoc/>
        public int IndentationDepth { get; set; }

        /// <inheritdoc/>
        public void Draw(int _)
        {
            // Update if the node cache is dirty.
            if (NodeData.Dirty)
            {
                NodeData.Update(Parent, ParentNode);
                NodeData.Dirty = false;
            }

            // Draw the node.
            NodeData.Draw(Parent, ParentNode);
        }
    }
}

/// <summary> The base class for file system drawer caches. </summary>
/// <typeparam name="TData"> The converted node to draw the tree. </typeparam>
/// <remarks> Does not use the filter cache base type because it behaves quite differently from regular filtered caches. </remarks>
public abstract class FileSystemCache<TData> : FileSystemCache
    where TData : IFileSystemNodeCache
{
    /// <inheritdoc cref="FileSystemCache.Parent"/>
    public new FileSystemDrawer<TData> Parent
        => (FileSystemDrawer<TData>)base.Parent;

    /// <summary> Get the filter used by the parent drawer. </summary>
    public IFileSystemFilter<TData> Filter
        => (IFileSystemFilter<TData>)Parent.Header.Filter;

    /// <summary> Create a new file system cache for the given typed drawer. </summary>
    /// <param name="parent"> The typed file system drawer. </param>
    public FileSystemCache(FileSystemDrawer<TData> parent)
        : base(parent)
    {
        Filter.FilterChanged += OnFilterChanged;
        InitializeNodes();
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        UpdateTreeList();
        using var style = ImStyleDouble.FramePadding.PushX(Im.Style.GlobalScale)
            .PushY(ImStyleDouble.ItemSpacing, Im.Style.GlobalScale)
            .Push(ImStyleSingle.IndentSpacing, 14 * Im.Style.GlobalScale);
        TreeLine.Draw(VisibleNodes, LineColor);
    }

    /// <summary> Update the linearized tree list if needed. </summary>
    /// <returns> True if the list was updated. </returns>
    protected virtual bool UpdateTreeList()
    {
        if (!VisibleDirty)
            return false;

        // Recursively add nodes in the correct order, starting from the children of root.
        InternalNodes.Clear();
        foreach (var node in FileSystem.Root.GetChildren(Parent.SortMode))
            AddNode(node, -1, 0);

        VisibleDirty = false;
        return true;

        void AddNode(IFileSystemNode node, int parentIndex, int currentDepth)
        {
            // Create the cache if necessary, this should not happen.
            if (!AllNodes.TryGetValue(node, out var cache))
            {
                cache = ConvertNodeInternal(node);
                AllNodes.Add(node, cache);
            }

            var visible = Filter.WouldBeVisible(cache);
            // Skip filtered out nodes.
            if (!visible && node is not IFileSystemFolder)
                return;

            // Create a new flattened node entry with the node and its cache,
            // and the current tree data.
            var data = new FileSystemTreeNode(this, node, cache)
            {
                IndentationDepth = currentDepth,
                ParentIndex      = parentIndex,
            };
            if (node is not IFileSystemFolder { Expanded: true } folder)
            {
                // Only add folders that have any children that fulfill the filter if they are collapsed.
                // If the node is not a folder, we already checked visibility above.
                if (!visible && ((IFileSystemFolder)node).GetDescendants().All(d => !AllNodes.TryGetValue(d, out var c) || !Filter.WouldBeVisible(c)))
                    return;

                // Add the visible data node or collapsed folder.
                data.StartsLineTo = -1;
                InternalNodes.Add(data);
                return;
            }

            var index = VisibleNodes.Count;
            // The folder is added regardless of visibility.
            InternalNodes.Add(data);
            // Add all visible children.
            foreach (var child in folder.GetChildren(Parent.SortMode))
                AddNode(child, index, currentDepth + 1);

            // We have visible children, so the folder is also visible either way.
            if (VisibleNodes.Count > index + 1)
                data.StartsLineTo = VisibleNodes.Count - 1;
            // The folder is visible by itself through the filter, but has no visible children.
            else if (visible)
                data.StartsLineTo = -1;
            // The folder has neither visible children, nor is visible by itself. Remove it again.
            else
                InternalNodes.RemoveAt(index);
        }
    }

    /// <summary> The filter changed, so we need to rebuild the visible list. </summary>
    private void OnFilterChanged()
        => VisibleDirty = true;

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Filter.FilterChanged -= OnFilterChanged;
    }
}
