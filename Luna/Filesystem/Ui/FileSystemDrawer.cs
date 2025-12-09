namespace Luna;

/// <summary> The base class to draw a full file system UI. </summary>
/// <param name="fileSystem"> The parent file system to draw. </param>
public abstract class FileSystemDrawer(BaseFileSystem fileSystem) : IPanel
{
    /// <inheritdoc/>>
    public abstract ReadOnlySpan<byte> Id { get; }

    /// <inheritdoc/>
    public abstract void Draw();

    /// <summary> The parent file system that is drawn by this. </summary>
    public readonly BaseFileSystem FileSystem = fileSystem;

    /// <summary> A footer with buttons that can be added to a two-panel layout. </summary>
    public readonly ButtonFooter Footer = SetupBaseFooter(fileSystem);

    /// <summary> The buttons shown in the main right-click context menu without an associated node. </summary>
    public readonly ButtonList MainContext = SetupBaseMainContext(fileSystem);

    /// <summary> The menu items shown in the context menu for folder nodes. </summary>
    public readonly ButtonList<IFileSystemFolder> FolderContext = SetupBaseFolderContext(fileSystem);

    /// <summary> The menu items shown in the context menu for data nodes. </summary>
    public readonly ButtonList<IFileSystemData> DataContext = new();

    /// <summary> Whether this file system drawer allows drag and drop operations. </summary>
    public bool AllowDragAndDrop { get; set; } = true;

    /// <summary> The sort mode used to order nodes in the drawer. </summary>
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

    /// <summary> An event that is invoked when the <see cref="SortMode"/> changes. </summary>
    public event Action? SortModeChanged;

    /// <summary> Create the default buttons available in the footer. </summary>
    private static ButtonFooter SetupBaseFooter(BaseFileSystem fileSystem)
    {
        var ret = new ButtonFooter();
        ret.Buttons.AddButton(new CreateFolderButton(fileSystem), 0);
        return ret;
    }

    /// <summary> Create the default menu items available in the main context menu. </summary>
    private static ButtonList SetupBaseMainContext(BaseFileSystem fileSystem)
    {
        var ret = new ButtonList();
        ret.AddButton(new ExpandAllButton(fileSystem),   20);
        ret.AddButton(new CollapseAllButton(fileSystem), 10);
        return ret;
    }

    /// <summary> Create the default menu items available in the folder context menu. </summary>
    private static ButtonList<IFileSystemFolder> SetupBaseFolderContext(BaseFileSystem fileSystem)
    {
        var ret = new ButtonList<IFileSystemFolder>();
        ret.AddButton(new ExpandDescendantsButton(fileSystem),   100);
        ret.AddButton(new CollapseDescendantsButton(fileSystem), 90);
        ret.AddButton(new LockFolderButton(fileSystem),          80);
        ret.AddButton(new DissolveFolderButton(fileSystem),      70);
        ret.AddButton(new RenameFolderInput(fileSystem),         -100);
        return ret;
    }

    /// <summary> Open the main context menu on a right-click in the current child window that does not hit any other item. </summary>
    protected virtual void RightClickMainContext()
    {
        // Do not open a main context menu without any items.
        if (MainContext.Count is 0)
            return;

        // Open the menu when a right-click occurs in the window but not on any item.
        if (!Im.Item.AnyHovered && Im.Mouse.IsClicked(MouseButton.Right) && Im.Window.Hovered(HoveredFlags.ChildWindows))
        {
            // Focus this panel if it is not already focused.
            if (!Im.Window.Focused(FocusedFlags.RootAndChildWindows))
                Im.Window.SetFocus(Id);
            Im.Popup.Open("MCTX"u8);
        }

        // Open the context menu popup.
        using var popup = Im.Popup.Begin("MCTX"u8);
        if (!popup)
            return;

        // Draw the menu items.
        foreach (var button in MainContext)
            button.DrawMenuItem();
    }
}

/// <summary> A filter that can apply to a given node cache type and to folder caches. </summary>
/// <typeparam name="TNodeCache"> The data node cache type. </typeparam>
public interface IFileSystemFilter<TNodeCache> : IFilter<TNodeCache>
    where TNodeCache : IFileSystemNodeCache
{
    /// <inheritdoc cref="IFilter{TNodeCache}.WouldBeVisible(in TNodeCache,int)"/>
    public bool WouldBeVisible(in IFileSystemNodeCache node)
        => node switch
        {
            FileSystemFolderCache folder => WouldBeVisible(folder),
            TNodeCache dataNode          => WouldBeVisible(in dataNode, -1),
            _                            => false,
        };

    /// <inheritdoc cref="IFilter{TNodeCache}.WouldBeVisible(in TNodeCache,int)"/>
    public bool WouldBeVisible(in FileSystemFolderCache folder);
}

/// <summary> A file system drawer that uses a specific node cache type. </summary>
/// <typeparam name="TNodeCache"> The cache to use. </typeparam>
/// <param name="fileSystem"> The parent file system to draw. </param>
/// <param name="filter"> The filter to use for the file system cache and a header above the panel in a 2-panel layout. </param>
public abstract class FileSystemDrawer<TNodeCache>(BaseFileSystem fileSystem, IFileSystemFilter<TNodeCache>? filter)
    : FileSystemDrawer(fileSystem)
    where TNodeCache : IFileSystemNodeCache
{
    /// <summary> The header containing the filter for this file system drawer. </summary>
    public readonly FilterHeader<TNodeCache> Header = new(filter ?? NopFilter.Instance, StringU8.Empty);

    /// <summary> Draw the panel. </summary>
    public override void Draw()
    {
        RightClickMainContext();
        var cache = CacheManager.Instance.GetOrCreateCache(Im.Id.Current, CreateCache);
        cache.Draw();
    }

    /// <summary> Create a new cache to draw from scratch. </summary>
    /// <returns> The cache. </returns>
    protected abstract FileSystemCache<TNodeCache> CreateCache();

    /// <inheritdoc cref="ImSharp.NopFilter{TNodeCache}"/>
    private sealed class NopFilter : IFileSystemFilter<TNodeCache>
    {
        /// <inheritdoc cref="ImSharp.NopFilter{TNodeCache}.Instance"/>
        public static readonly NopFilter Instance = new();

        /// <inheritdoc/>
        public bool WouldBeVisible(in TNodeCache node, int depth)
            => true;

        /// <inheritdoc/>
        public bool WouldBeVisible(in FileSystemFolderCache folder)
            => true;

        /// <inheritdoc/>
        public event Action? FilterChanged
        {
            add { }
            remove { }
        }

        /// <inheritdoc/>
        public bool IsVisible
            => false;

        /// <inheritdoc/>
        public bool DrawFilter(ReadOnlySpan<byte> label, Vector2 availableRegion)
            => false;

        /// <inheritdoc/>
        public void Clear()
        { }
    }
}
