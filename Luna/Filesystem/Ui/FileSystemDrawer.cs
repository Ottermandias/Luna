namespace Luna;

/// <summary> The base class to draw a full file system UI. </summary>
public abstract class FileSystemDrawer : IPanel
{
    /// <inheritdoc/>>
    public abstract ReadOnlySpan<byte> Id { get; }

    /// <inheritdoc/>
    public abstract void Draw();

    /// <summary> A messager to inform users of failed operations. </summary>
    public readonly MessageService Messager;

    /// <summary> The parent file system that is drawn by this. </summary>
    public readonly BaseFileSystem FileSystem;

    /// <summary> A footer with buttons that can be added to a two-panel layout. </summary>
    public readonly ButtonFooter Footer;

    /// <summary> The buttons shown in the main right-click context menu without an associated node. </summary>
    public readonly ButtonList MainContext;

    /// <summary> The menu items shown in the context menu for folder nodes. </summary>
    public readonly ButtonList<IFileSystemFolder> FolderContext;

    /// <summary> The menu items shown in the context menu for separator nodes. </summary>
    public readonly ButtonList<IFileSystemSeparator> SeparatorContext = [];

    /// <summary> The menu items shown in the context menu for data nodes. </summary>
    public readonly ButtonList<IFileSystemData> DataContext = new();

    /// <summary> The base class to draw a full file system UI. </summary>
    /// <param name="messager"> A messager to inform users of failed operations. </param>
    /// <param name="fileSystem"> The parent file system to draw. </param>
    /// <param name="filter"> The filter used by the drawer to pass to the buttons. </param>
    protected FileSystemDrawer(MessageService messager, BaseFileSystem fileSystem, IFilter filter)
    {
        Messager         = messager;
        FileSystem       = fileSystem;
        Footer           = SetupBaseFooter(fileSystem);
        MainContext      = SetupBaseMainContext(fileSystem, filter);
        FolderContext    = SetupBaseFolderContext(this, filter);
        SeparatorContext = SetupBaseSeparatorContext(this);
    }

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

    /// <summary> The valid sort modes for this drawer. </summary>
    public virtual IEnumerable<ISortMode> ValidSortModes
        =>
        [
            ISortMode.FoldersFirst, ISortMode.Lexicographical, ISortMode.FoldersLast, ISortMode.InverseLexicographical,
            ISortMode.InverseFoldersFirst, ISortMode.InverseFoldersLast, ISortMode.InternalOrder, ISortMode.InverseInternalOrder,
        ];

    /// <summary> Get the color expanded folders without individual colors should use. </summary>
    public virtual Vector4 ExpandedFolderColor
        => Im.Style[ImGuiColor.Text];

    /// <summary> Get the color collapsed folders without individual colors should use. </summary>
    public virtual Vector4 CollapsedFolderColor
        => Im.Style[ImGuiColor.Text];

    /// <summary> Get the color the folder line should use. </summary>
    public virtual Vector4 FolderLineColor
        => Im.Style[ImGuiColor.TextDisabled];

    /// <summary> An event that is invoked when the <see cref="SortMode"/> changes. </summary>
    public event Action? SortModeChanged;

    /// <summary> Create the default buttons available in the footer. </summary>
    private static ButtonFooter SetupBaseFooter(BaseFileSystem fileSystem)
    {
        var ret = new ButtonFooter();
        ret.Buttons.AddButton(new CreateFolderButton(fileSystem),    0);
        ret.Buttons.AddButton(new CreateSeparatorButton(fileSystem), -1);
        return ret;
    }

    /// <summary> Create the default menu items available in the main context menu. </summary>
    private static ButtonList SetupBaseMainContext(BaseFileSystem fileSystem, IFilter filter)
    {
        var ret = new ButtonList();
        ret.AddButton(new ExpandAllButton(fileSystem, filter),   20);
        ret.AddButton(new CollapseAllButton(fileSystem, filter), 10);
        return ret;
    }

    /// <summary> Create the default menu items available in the folder context menu. </summary>
    private static ButtonList<IFileSystemFolder> SetupBaseFolderContext(FileSystemDrawer drawer, IFilter filter)
    {
        var ret = new ButtonList<IFileSystemFolder>();
        ret.AddButton(new ExpandDescendantsButton(drawer.FileSystem, filter),   100);
        ret.AddButton(new CollapseDescendantsButton(drawer.FileSystem, filter), 90);

        var editFolderButtons = new SubMenuButton<IFileSystemFolder>(new StringU8("Edit Folder"u8));
        editFolderButtons.Entries.AddButton(new LockFolderButton(drawer.FileSystem),     20);
        editFolderButtons.Entries.AddButton(new DissolveFolderButton(drawer.FileSystem), 15);
        editFolderButtons.Entries.AddButton(new MenuSeparator<IFileSystemFolder>(),      12);
        editFolderButtons.Entries.AddButton(new FolderColorEdits(drawer),                10);
        editFolderButtons.Entries.AddButton(new SortModeSelector(drawer),                0);
        ret.AddButton(editFolderButtons, 0);

        ret.AddButton(new RenameFolderInput(drawer.FileSystem), -100);
        return ret;
    }

    /// <summary> Create the default menu items available in the folder context menu. </summary>
    private static ButtonList<IFileSystemSeparator> SetupBaseSeparatorContext(FileSystemDrawer drawer)
    {
        var ret = new ButtonList<IFileSystemSeparator>();
        ret.AddButton(new SeparatorSortAsFolderButton(drawer.FileSystem), 100);
        ret.AddButton(new SeparatorDeleteButton(drawer.FileSystem),       90);
        ret.AddButton(new MenuSeparator<IFileSystemSeparator>(),          85);
        ret.AddButton(new SeparatorColorEdit(drawer),                     80);
        ret.AddButton(new MenuSeparator<IFileSystemSeparator>(),          70);
        ret.AddButton(new SeparatorPathEdit(drawer.FileSystem),           -100);
        ret.AddButton(new SeparatorTimestampEdit(drawer.FileSystem),      -110);
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
            FileSystemSeparatorCache     => IsEmpty,
            FileSystemFolderCache folder => WouldBeVisible(folder),
            TNodeCache dataNode          => WouldBeVisible(in dataNode, -1),
            _                            => false,
        };

    /// <inheritdoc cref="IFilter{TNodeCache}.WouldBeVisible(in TNodeCache,int)"/>
    public bool WouldBeVisible(in FileSystemFolderCache folder);
}

/// <summary> A file system drawer that uses a specific node cache type. </summary>
/// <typeparam name="TNodeCache"> The cache to use. </typeparam>
/// <param name="messager"> A messager to inform users of failed operations. </param>
/// <param name="fileSystem"> The parent file system to draw. </param>
/// <param name="filter"> The filter to use for the file system cache and a header above the panel in a 2-panel layout. </param>
public abstract class FileSystemDrawer<TNodeCache>(MessageService messager, BaseFileSystem fileSystem, IFileSystemFilter<TNodeCache>? filter)
    : FileSystemDrawer(messager, fileSystem, filter ?? NopFilter.Instance)
    where TNodeCache : IFileSystemNodeCache
{
    /// <summary> The header containing the filter for this file system drawer. </summary>
    public readonly FilterHeader<TNodeCache> Header = new(filter ?? NopFilter.Instance, new StringU8("Filter..."u8));

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
        public bool Clear()
            => false;

        /// <inheritdoc/>
        public bool IsEmpty
            => true;
    }
}
