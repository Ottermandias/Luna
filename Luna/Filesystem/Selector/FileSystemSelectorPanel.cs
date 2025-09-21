namespace Luna;

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
