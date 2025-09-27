namespace Luna;

public abstract class FileSystemSelectorPanel<TCache, TCacheNode> : IPanel, IDisposable
    where TCache : FileSystemSelectorCache<TCacheNode>
    where TCacheNode : FileSystemCacheNodeBase<TCacheNode>
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
        var cache = CacheManager.Instance.GetOrCreateCache(Im.Id.Current, CreateCache);
        var color = 0xFFFFFFFF;
        TreeLine.Draw(cache.FlatList, color);
    }

    protected abstract TCache CreateCache();
}
