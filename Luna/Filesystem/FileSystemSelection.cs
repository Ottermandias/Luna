namespace Luna;

/// <summary> Data collection keeping more detailed track of selection state for a file system. </summary>
public sealed class FileSystemSelection : IDisposable
{
    private readonly BaseFileSystem _fileSystem;

    private readonly List<IFileSystemNode>   _orderedNodes = [];
    private readonly List<IFileSystemData>   _dataNodes    = [];
    private readonly List<IFileSystemFolder> _folders      = [];

    /// <summary> The current selection changed. </summary>
    public event Action? Changed;

    /// <summary> Whether this selection object allows multiple nodes to be selected at once. </summary>
    public readonly bool AllowsMultiSelection;

    /// <summary> The list of selected nodes in order of selection. </summary>
    /// <remarks> Re-selecting an already selected node without unselecting it before does not update the order. </remarks>
    public IReadOnlyList<IFileSystemNode> OrderedNodes
        => _orderedNodes;

    /// <summary> The list of all selected data nodes, excluding folders. </summary>
    public IReadOnlyList<IFileSystemData> DataNodes
        => _dataNodes;

    /// <summary> The list of all selected folders. </summary>
    public IReadOnlyList<IFileSystemFolder> Folders
        => _folders;

    /// <summary> A single selected data node if exactly one is selected; otherwise, null. </summary>
    public IFileSystemData? Selection { get; private set; }

    /// <summary> Create the selection data for a specific parent. </summary>
    /// <remarks> Since this is only called by the <see cref="BaseFileSystem"/> itself, and thus has at least the same lifetime as the event, it is not disposed by its parent. </remarks>
    internal FileSystemSelection(BaseFileSystem fileSystem, bool allowsMultiSelection)
    {
        _fileSystem          = fileSystem;
        AllowsMultiSelection = allowsMultiSelection;
        _fileSystem.Changed.Subscribe(OnFileSystemChanged, uint.MaxValue);
    }

    /// <summary> Unselect all nodes. </summary>
    public void UnselectAll()
    {
        while (OrderedNodes.Count > 0)
            _fileSystem.ChangeSelectedState(OrderedNodes[^1], false);
        Selection = null;
    }

    /// <summary> Select a single node and unselect all other nodes. </summary>
    /// <param name="node"> The node to select. </param>
    public void Select(IFileSystemData node)
    {
        UnselectAll();
        _fileSystem.ChangeSelectedState(node, true);
    }

    /// <summary> Toggle the selection state of a single node. </summary>
    /// <param name="node"> The node to toggle. </param>
    /// <remarks> Does nothing if <see cref="AllowsMultiSelection"/> is false and another node is already selected. </remarks>
    public void ToggleSelection(IFileSystemNode node)
    {
        if (!AllowsMultiSelection && _orderedNodes.Count > 0)
            return;

        _fileSystem.ChangeSelectedState(node, !node.Selected);
    }

    /// <summary> Add a single node to the selection. </summary>
    /// <param name="node"> The node to select. </param>
    /// <remarks> Does nothing if <see cref="AllowsMultiSelection"/> is false and another node is already selected. </remarks>
    public void AddToSelection(IFileSystemNode node)
    {
        if (!AllowsMultiSelection && _orderedNodes.Count > 0)
            return;

        _fileSystem.ChangeSelectedState(node, true);
    }

    /// <summary> Remove a single node from the selection. </summary>
    /// <param name="node"> The node to unselect. </param>
    public void RemoveFromSelection(IFileSystemNode node)
        => _fileSystem.ChangeSelectedState(node, false);


    /// <summary> Remove a node from the selection as consequence of a file system event. </summary>
    /// <param name="node"> The removed node. </param>
    private void RemoveNode(IFileSystemNode node)
    {
        if (!_orderedNodes.Remove(node))
            return;

        switch (node)
        {
            case IFileSystemData data:
                _dataNodes.Remove(data);
                break;
            case IFileSystemFolder folder:
                _folders.Remove(folder);
                break;
        }
        Selection = null;
        Changed?.Invoke();
    }

    /// <summary> Add a node to the selection as consequence of a file system event. </summary>
    /// <param name="node"> The selected node. </param>
    /// <remarks> If the node is already selected, this does not update the selection order. </remarks>
    private void AddNode(IFileSystemNode node)
    {
        if (!node.Selected)
            return;

        if (_orderedNodes.Contains(node))
            return;

        Selection = null;
        _orderedNodes.Add(node);
        switch (node)
        {
            case IFileSystemData data:
                _dataNodes.Add(data);
                if (_orderedNodes.Count is 1)
                    Selection = data;
                break;
            case IFileSystemFolder folder:
                _folders.Add(folder);
                break;
        }

        Changed?.Invoke();
    }

    /// <summary> Clear all selected nodes. </summary>
    private void Clear()
    {
        Selection = null;
        _orderedNodes.Clear();
        _dataNodes.Clear();
        _folders.Clear();
    }

    /// <summary> Update the selected nodes from the file system data. </summary>
    public void SetData()
    {
        foreach (var node in _fileSystem.Root.GetDescendants().Where(n => n.Selected))
            AddNode(node);
    }

    /// <summary> React to file system selection changes. </summary>
    private void OnFileSystemChanged(in FileSystemChanged.Arguments arguments)
    {
        switch (arguments.Type)
        {
            case FileSystemChangeType.ObjectRemoved:  RemoveNode(arguments.ChangedObject); break;
            case FileSystemChangeType.FolderAdded:    AddNode(arguments.ChangedObject); break;
            case FileSystemChangeType.DataAdded:      AddNode(arguments.ChangedObject); break;
            case FileSystemChangeType.FolderMerged:   RemoveNode(arguments.ChangedObject); break;
            case FileSystemChangeType.ReloadStarting: Clear(); break;
            case FileSystemChangeType.Reload:         SetData(); break;

            case FileSystemChangeType.SelectedChange when arguments.ChangedObject.Selected:  AddNode(arguments.ChangedObject); break;
            case FileSystemChangeType.SelectedChange when !arguments.ChangedObject.Selected: RemoveNode(arguments.ChangedObject); break;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Clear();
        _fileSystem.Changed.Unsubscribe(OnFileSystemChanged);
    }
}
