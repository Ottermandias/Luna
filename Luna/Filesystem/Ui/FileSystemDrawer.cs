namespace Luna;

public abstract class FileSystemDrawer(BaseFileSystem fileSystem) : IPanel
{
    public abstract ReadOnlySpan<byte> Id { get; }
    public abstract void               Draw();

    public readonly BaseFileSystem                FileSystem    = fileSystem;
    public readonly ButtonFooter                  Footer        = SetupBaseFooter(fileSystem);
    public readonly ButtonList                    MainContext   = SetupBaseMainContext(fileSystem);
    public readonly ButtonList<IFileSystemFolder> FolderContext = SetupBaseFolderContext(fileSystem);
    public readonly ButtonList<IFileSystemData>   DataContext   = new();
    public          bool                          AllowDragAndDrop { get; set; } = true;

    public ISortMode SortMode
    {
        get;
        set
        {
            if (value == field)
                return;

            field = value;
            SortModeChanged?.Invoke();
        }
    } = ISortMode.FoldersFirst;

    public event Action? SortModeChanged;

    private static ButtonFooter SetupBaseFooter(BaseFileSystem fileSystem)
    {
        var ret = new ButtonFooter();
        ret.Buttons.AddButton(new CreateFolderButton(fileSystem), 0);
        return ret;
    }

    private static ButtonList SetupBaseMainContext(BaseFileSystem fileSystem)
    {
        var ret = new ButtonList();
        ret.AddButton(new ExpandAllButton(fileSystem),   20);
        ret.AddButton(new CollapseAllButton(fileSystem), 10);
        return ret;
    }

    private static ButtonList<IFileSystemFolder> SetupBaseFolderContext(BaseFileSystem fileSystem)
    {
        var ret = new ButtonList<IFileSystemFolder>();
        ret.AddButton(new ExpandDescendantsButton(fileSystem),   100);
        ret.AddButton(new CollapseDescendantsButton(fileSystem), 90);
        ret.AddButton(new DissolveFolderButton(fileSystem),      80);
        ret.AddButton(new RenameFolderInput(fileSystem),         -100);
        return ret;
    }

    protected virtual void RightClickMainContext()
    {
        if (MainContext.Count is 0)
            return;

        if (!Im.Item.AnyHovered && Im.Mouse.IsClicked(MouseButton.Right) && Im.Window.Hovered(HoveredFlags.ChildWindows))
        {
            if (!Im.Window.Focused(FocusedFlags.RootAndChildWindows))
                Im.Window.SetFocus(Id);
            Im.Popup.Open("MCTX"u8);
        }

        using var popup = Im.Popup.Begin("MCTX"u8);
        if (!popup)
            return;

        foreach (var button in MainContext)
            button.DrawMenuItem();
    }
}

public abstract class FileSystemDrawer<TNodeCache>(BaseFileSystem fileSystem, IFilter<TNodeCache>? filter)
    : FileSystemDrawer(fileSystem)
    where TNodeCache : IFileSystemNodeCache
{
    public readonly FilterHeader<TNodeCache> Header = new(filter ?? NopFilter<TNodeCache>.Instance, StringU8.Empty);

    public override void Draw()
    {
        RightClickMainContext();
        var cache = CacheManager.Instance.GetOrCreateCache(Im.Id.Current, CreateCache);
        cache.Draw();
    }

    protected abstract FileSystemCache<TNodeCache> CreateCache();
}
