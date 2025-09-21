namespace Luna;

/// <summary> The different context menu entries for a file system selector. Contains a main context menu, a folder context menu and a data context menu. </summary>
/// <param name="fileSystem"> The file system used. </param>
public sealed class FileSystemSelectorContextMenu(FileSystemSelectorPanel parent, BaseFileSystem fileSystem)
{
    public ReadOnlySpan<byte> WindowId
        => parent.Id;

    /// <summary> Arguments passed to the main context menu buttons. </summary>
    /// <param name="FileSystem"> The file system used. </param>
    public readonly record struct MainButtonData(BaseFileSystem FileSystem);

    /// <summary> Arguments passed to the folder context menu buttons. </summary>
    /// <param name="FileSystem"> The file system used. </param>
    /// <param name="Folder"> The folder for which the context menu was opened. </param>
    public readonly record struct FolderButtonData(BaseFileSystem FileSystem, IFileSystemFolder Folder);

    /// <summary> Arguments passed to the data context menu buttons. </summary>
    /// <param name="FileSystem"> The file system used. </param>
    /// <param name="Data"> The data node for which the context menu was opened. </param>
    public readonly record struct DataButtonData(BaseFileSystem FileSystem, IFileSystemData Data);

    /// <summary> The menu items for the main context menu. </summary>
    public readonly ButtonList<MainButtonData> MainContext = new();

    /// <summary> The menu items for the folder context menu. </summary>
    public readonly ButtonList<FolderButtonData> FolderContext = new();

    /// <summary> The menu items for the data context menu. </summary>
    public readonly ButtonList<DataButtonData> DataContext = new();

    /// <summary> Initialize a default context menu with common operations. </summary>
    /// <param name="fileSystem"> The file system used. </param>
    /// <returns> A context menu with default menu items. </returns>
    /// <remarks>
    ///   Main: Expand all folders, Collapse all folders. <br/>
    ///   Folder: Expand all descendants, Collapse all descendants, Lock/Unlock, Dissolve folder, Rename/move folder. <br/>
    ///   Data: Lock/Unlock, Rename/move search path.
    /// </remarks>
    public static FileSystemSelectorContextMenu InitializeDefault(BaseFileSystem fileSystem)
    {
        var ret = new FileSystemSelectorContextMenu(fileSystem);
        ret.MainContext.AddButton(new ExpandAllFolders(),   1);
        ret.MainContext.AddButton(new CollapseAllFolders(), 2);

        ret.FolderContext.AddButton(new ExpandAllDescendants(),   100);
        ret.FolderContext.AddButton(new CollapseAllDescendants(), 101);
        ret.FolderContext.AddButton(new SetFolderLocked(),        900);
        ret.FolderContext.AddButton(new DissolveFolder(),         999);
        ret.FolderContext.AddButton(new RenameFolder(),           1000);

        ret.DataContext.AddButton(new SetDataLocked(), 900);
        ret.DataContext.AddButton(new RenameData(),    1000);
        return ret;
    }

    /// <summary> Draw the main context menu. Call this at the start of drawing the panel without any style changes. </summary>
    public void DrawMainContext()
    {
        if (MainContext.Count is 0)
            return;

        if (Im.Item.AnyHovered && Im.Mouse.IsClicked(MouseButton.Right) && Im.Window.Hovered(HoveredFlags.ChildWindows))
        {
            if (!Im.Window.Focused(FocusedFlags.RootAndChildWindows))
                Im.Window.SetFocus(WindowId);
            Im.Popup.Open("MainContext"u8);
        }

        using var popup = Im.Popup.Begin("MainContext"u8);
        if (!popup)
            return;

        var arguments = new MainButtonData(fileSystem);
        foreach (var button in MainContext)
            button.DrawMenuItem(arguments);
    }

    /// <summary> Draw a folder context menu. Call this directly after drawing the folder, as it checks for a right-click on the last item. </summary>
    public void DrawFolderContext(in IFileSystemFolder folder)
    {
        if (FolderContext.Count is 0)
            return;

        if (Im.Item.Clicked(MouseButton.Right))
            Im.Popup.Open($"{folder.Identifier}");

        using var popup = Im.Popup.Begin($"{folder.Identifier}");
        if (!popup)
            return;

        var arguments = new FolderButtonData(fileSystem, folder);
        foreach (var button in FolderContext)
            button.DrawMenuItem(arguments);
    }

    /// <summary> Draw a data context menu. Call this directly after drawing the data node, as it checks for a right-click on the last item. </summary>
    public void DrawDataContext(in IFileSystemData data)
    {
        if (DataContext.Count is 0)
            return;

        if (Im.Item.Clicked(MouseButton.Right))
            Im.Popup.Open($"{data.Identifier}");

        using var popup = Im.Popup.Begin($"{data.Identifier}");
        if (!popup)
            return;

        var arguments = new DataButtonData(fileSystem, data);
        foreach (var button in DataContext)
            button.DrawMenuItem(arguments);
    }

    public sealed class ExpandAllFolders : BaseButton<MainButtonData>
    {
        public override ReadOnlySpan<byte> Label(in MainButtonData _)
            => "Expand All Directories"u8;

        public override void OnClick(in MainButtonData data)
        {
            // TODO
        }
    }

    public sealed class CollapseAllFolders : BaseButton<MainButtonData>
    {
        public override ReadOnlySpan<byte> Label(in MainButtonData _)
            => "Collapse All Directories"u8;

        public override void OnClick(in MainButtonData data)
        {
            // TODO
        }
    }

    public sealed class ExpandAllDescendants : BaseButton<FolderButtonData>
    {
        public override ReadOnlySpan<byte> Label(in FolderButtonData _)
            => "Expand All Descendants"u8;

        public override bool HasTooltip
            => true;

        public override void DrawTooltip(in FolderButtonData data)
            => Im.Text("Successively expand all folders that descend from this folder, including itself."u8);


        public override void OnClick(in FolderButtonData data)
        {
            // TODO
        }
    }

    public sealed class CollapseAllDescendants : BaseButton<FolderButtonData>
    {
        public override ReadOnlySpan<byte> Label(in FolderButtonData _)
            => "Collapse All Descendants"u8;

        public override bool HasTooltip
            => true;

        public override void DrawTooltip(in FolderButtonData data)
            => Im.Text("Successively collapse all folders that descend from this folder, including itself."u8);


        public override void OnClick(in FolderButtonData data)
        {
            // TODO
        }
    }

    public sealed class DissolveFolder : BaseButton<FolderButtonData>
    {
        public override ReadOnlySpan<byte> Label(in FolderButtonData _)
            => "Dissolve Folder"u8;

        public override void DrawTooltip(in FolderButtonData _)
            => Im.Text("Remove this folder and move all its children to its parent-folder, if possible."u8);

        public override bool HasTooltip
            => true;

        public override void OnClick(in FolderButtonData data)
        {
            if (data.Folder.IsRoot)
                return;

            // TODO: do in actions outside of iteration.
            data.FileSystem.Merge(data.Folder, data.Folder.Parent!);
        }
    }

    public sealed class RenameFolder : BaseButton<FolderButtonData>
    {
        public override ReadOnlySpan<byte> Label(in FolderButtonData data)
            => "##Rename"u8;

        public override bool DrawMenuItem(in FolderButtonData data)
        {
            var currentPath = data.Folder.FullPath;
            var ret         = Im.Input.Text("##Rename"u8, ref currentPath, "Folder Path..."u8, InputTextFlags.EnterReturnsTrue);
            if (ret)
            {
                // TODO: do in actions outside of iteration.
                data.FileSystem.RenameAndMove(data.Folder, currentPath);
                // Todo expand ancestors.
                Im.Popup.CloseCurrent();
            }


            Im.Tooltip.OnHover("Enter a full path here to move or rename the folder. Creates all required parent directories, if possible."u8);
            return ret;
        }
    }

    public sealed class SetFolderLocked : BaseButton<FolderButtonData>
    {
        public override ReadOnlySpan<byte> Label(in FolderButtonData data)
            => data.Folder.Locked ? "Unlock"u8 : "Lock"u8;

        public override void DrawTooltip(in FolderButtonData _)
            => Im.Text(
                "Locking an item prevents this item from being dragged to other positions. It does not prevent any other manipulations of the item."u8);

        public override bool HasTooltip
            => true;

        public override void OnClick(in FolderButtonData data)
            => data.FileSystem.ChangeLockState(data.Folder, !data.Folder.Locked);
    }

    public sealed class SetDataLocked : BaseButton<DataButtonData>
    {
        public override ReadOnlySpan<byte> Label(in DataButtonData data)
            => data.Data.Locked ? "Unlock"u8 : "Lock"u8;

        public override void DrawTooltip(in DataButtonData _)
            => Im.Text(
                "Locking an item prevents this item from being dragged to other positions. It does not prevent any other manipulations of the item."u8);

        public override bool HasTooltip
            => true;

        public override void OnClick(in DataButtonData data)
            => data.FileSystem.ChangeLockState(data.Data, !data.Data.Locked);
    }

    public sealed class RenameData : BaseButton<DataButtonData>
    {
        public override ReadOnlySpan<byte> Label(in DataButtonData data)
            => "##Rename"u8;

        public override bool DrawMenuItem(in DataButtonData data)
        {
            var currentPath = data.Data.FullPath;
            if (Im.Window.Appearing)
                Im.Keyboard.SetFocusHere();
            Im.Text("Rename Search Path or Move:"u8);
            var ret = Im.Input.Text("##RenameSearch"u8, ref currentPath, "Search Path..."u8, InputTextFlags.EnterReturnsTrue);
            if (ret)
            {
                // TODO: do in actions outside of iteration.
                data.FileSystem.RenameAndMove(data.Data, currentPath);
                // Todo expand ancestors.
                Im.Popup.CloseCurrent();
            }

            Im.Tooltip.OnHover(
                "Enter a full path here to move or rename the search path of the leaf. Creates all required parent directories, if possible.\n\nDoes NOT rename the actual data!"u8);
            return ret;
        }
    }
}
