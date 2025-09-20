namespace Luna;

public class FileSystemSelectorCache<TCacheItem> : FilterCache<TCacheItem>, IDisposable
{
    public readonly  IFilter<TCacheItem> Filter;
    private readonly BaseFileSystem      _fileSystem;
    private readonly FileSystemSelection _selection;

    public FileSystemSelectorCache(BaseFileSystem fileSystem, FileSystemSelection selection, IFilter<TCacheItem> filter)
    {
        _fileSystem = fileSystem;
        _selection  = selection;
        Filter      = filter;
        _fileSystem.Changed.Subscribe(OnFileSystemChange, uint.MinValue);
        _selection.Changed.Subscribe(OnSelectionChange, uint.MaxValue);
    }

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

    protected override bool WouldBeVisible(in TCacheItem item, int globalIndex)
        => Filter.WouldBeVisible(item, globalIndex);

    protected override IEnumerable<TCacheItem> GetItems()
    {
        return null;
    }

    protected override void Dispose(bool disposing)
    {
        _fileSystem.Changed.Unsubscribe(OnFileSystemChange);
        _selection.Changed.Unsubscribe(OnSelectionChange);
    }
}
