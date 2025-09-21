namespace Luna;

/// <summary> A class containing selected nodes of a filesystem and managing selection state. </summary>
public class FileSystemSelection : IDisposable, IUiService
{
    /// <summary> The parent file system. </summary>
    protected readonly BaseFileSystem FileSystem;

    /// <summary> Whether the file system allows multi selection or only single selection. </summary>
    public virtual bool AllowsMultiSelection
        => true;

    /// <summary> All selected nodes in a quick lookup set. </summary>
    protected readonly HashSet<IFileSystemNode> Selected = [];

    /// <summary> The selected nodes in the order they were selected and with random access. </summary>
    protected readonly List<IFileSystemNode> OrderedSelection = [];

    /// <summary> The selected nodes filtered to folders only. </summary>
    protected readonly List<IFileSystemFolder> SelectedFolders = [];

    /// <summary> The selected nodes filtered to data nodes only. </summary>
    protected readonly List<IFileSystemData> SelectedData = [];

    /// <summary> Temporary value during file system reloads to restore selection of folders by path. </summary>
    protected string[]? SelectedPaths;

    /// <summary> Temporary value during file system reloads to restore selection of data nodes by values. </summary>
    protected IFileSystemValue[]? SelectedValues;

    /// <summary> Event invoked whenever the selection changes. </summary>
    public readonly SelectionChangedEvent Changed;

    /// <summary> Get the current selection in selection order. </summary>
    public IReadOnlyList<IFileSystemNode> Selection
        => OrderedSelection;

    /// <summary> Create a new selection manager for the given file system and selection changed event. </summary>
    /// <param name="fileSystem"> The parent file system. </param>
    /// <param name="event"> The event to invoke when the selection changes. </param>
    public FileSystemSelection(BaseFileSystem fileSystem, SelectionChangedEvent @event)
    {
        FileSystem = fileSystem;
        Changed    = @event;
        FileSystem.Changed.Subscribe(OnFileSystemChanged, uint.MaxValue);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~FileSystemSelection()
        => Dispose(false);

    /// <summary> Disposal method to unsubscribe from events. </summary>
    /// <param name="_"> Whether this is called from Dispose or a finalizer. </param>
    protected virtual void Dispose(bool _)
        => FileSystem.Changed.Unsubscribe(OnFileSystemChanged);

    /// <summary> Whether we currently have multiple nodes selected.  </summary>
    public bool MultiSelection
        => Selected.Count > 1;

    /// <summary> The selected data node if we have exactly one selected data node and nothing else. </summary>
    public IFileSystemData? SingleSelection
        => SelectedData.Count is 1 && Selected.Count is 1
            ? SelectedData[0]
            : null;

    /// <summary> Check whether the given node is currently selected. </summary>
    /// <param name="node"> The node to check. </param>
    /// <returns> True if the node is selected. </returns>
    public bool IsSelected(IFileSystemNode node)
        => Selected.Contains(node);

    /// <summary> Select the given node. </summary>
    /// <param name="node"> The node to mark as selected. </param>
    /// <returns> True if the node was newly selected, false if it was already selected. </returns>
    public bool Select(IFileSystemNode node)
    {
        if (AllowsMultiSelection)
        {
            if (!Selected.Add(node))
                return false;

            OrderedSelection.Add(node);
            switch (node)
            {
                case IFileSystemFolder f: SelectedFolders.Add(f); break;
                case IFileSystemData d:   SelectedData.Add(d); break;
            }

            Changed.Invoke(new SelectionChangedEvent.Arguments(node, null));
            return true;
        }

        if (node is not IFileSystemData data)
            return false;

        if (Selected.Count == 1 && SelectedData.Count is 1 && SelectedData[0] == data)
            return false;

        UnselectAll();
        Selected.Add(data);
        OrderedSelection.Add(data);
        SelectedData.Add(data);
        Changed.Invoke(new SelectionChangedEvent.Arguments(node, null));
        return true;
    }

    /// <summary> Clear the current selection. </summary>
    public void UnselectAll()
    {
        while (OrderedSelection.Count > 0)
            Unselect(OrderedSelection[0]);
    }

    /// <summary> Unselect the given node. </summary>
    /// <param name="node"> The node to remove the selected mark from. </param>
    /// <returns> True if the node was selected before, false if it was not. </returns>
    public bool Unselect(IFileSystemNode node)
    {
        if (!Selected.Remove(node))
            return false;

        OrderedSelection.Remove(node);
        switch (node)
        {
            case IFileSystemFolder f: SelectedFolders.Remove(f); break;
            case IFileSystemData d:   SelectedData.Remove(d); break;
        }

        Changed.Invoke(new SelectionChangedEvent.Arguments(null, node));
        return true;
    }

    /// <summary> Handle changes in the parent file system. </summary>
    /// <param name="arguments"> The arguments for the change. </param>
    private void OnFileSystemChanged(in FileSystemChanged.Arguments arguments)
    {
        switch (arguments.Type)
        {
            // If an object was removed or a folder was merged, we need to unselect to get rid of the reference.
            case FileSystemChangeType.ObjectRemoved:
            case FileSystemChangeType.FolderMerged:
                Unselect(arguments.ChangedObject);
                break;
            // If we are reloading, we need to store the selection and restore it afterward.
            case FileSystemChangeType.ReloadStarting:
                SelectedPaths  = SelectedFolders.Select(f => f.FullPath).ToArray();
                SelectedValues = SelectedData.Select(d => d.Value).ToArray();
                UnselectAll();
                break;

            // After a reload, we need to restore as much of the selection. as possible.
            case FileSystemChangeType.Reload:
                if (SelectedPaths is { Length: > 0 })
                    foreach (var path in SelectedPaths)
                    {
                        if (FileSystem.Find(path, out var node) && node is IFileSystemFolder folder)
                            Select(folder);
                    }

                if (SelectedValues is { Length: > 0 })
                    foreach (var value in SelectedValues.Where(v => v.Node is not null))
                        Select(value.Node!);

                SelectedPaths  = null;
                SelectedValues = null;
                break;
        }
    }

    /// <summary> Invoked whenever the selection of the associated file system changes. </summary>
    /// <param name="name"> The name of the event. </param>
    /// <param name="log"> A logger. </param>
    public sealed class SelectionChangedEvent(string name, Logger log) : EventBase<SelectionChangedEvent.Arguments, uint>(name, log)
    {
        /// <summary> The arguments for a selection changed event. </summary>
        /// <param name="Added"> The node that was added to the selection, if any. </param>
        /// <param name="Removed"> The node that was removed from the selection, if any. </param>
        public readonly record struct Arguments(IFileSystemNode? Added, IFileSystemNode? Removed);
    }
}
