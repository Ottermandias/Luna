namespace Luna;

public class FileSystemSelectorCache<TCacheItem>(BaseFileSystem fileSystem, IFilter<TCacheItem> filter) : FilterCache<TCacheItem>, IDisposable
{
    public readonly IFilter<TCacheItem> Filter = filter;

    public virtual ISortMode SortMode
        => ISortMode.FoldersFirst;

    protected override bool WouldBeVisible(in TCacheItem item, int globalIndex)
        => Filter.WouldBeVisible(item, globalIndex);

    protected override IEnumerable<TCacheItem> GetItems()
        => throw new NotImplementedException();
}

public class FileSystemSelectorPanel : IPanel, IDisposable
{
    public          StringU8                      Label { get; init; } = new("Selector"u8);
    public readonly FileSystemSelectorContextMenu ContextMenu;

    public ReadOnlySpan<byte> Id
        => Label;

    public FileSystemSelectorPanel(BaseFileSystem fileSystem)
    {
        ContextMenu = new FileSystemSelectorContextMenu(this, fileSystem);
    }

    public void Dispose()
    { }

    public void Draw()
    {
        ContextMenu.DrawMainContext();
        CacheManager.Instance.GetOrCreateCache();
    }
}
