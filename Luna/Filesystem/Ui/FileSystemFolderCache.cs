namespace Luna;

/// <summary> The cached object for folder nodes. </summary>
public sealed class FileSystemFolderCache : IFileSystemNodeCache
{
    /// <inheritdoc/>
    public bool Dirty { get; set; } = true;

    /// <summary> The folder name as a UTF8 string. </summary>
    public StringU8 Label { get; set; } = StringU8.Empty;

    /// <summary> The full path of the folder as a UTF16 string. </summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary> The folder name as a UTF16 string. </summary>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    public void Update(FileSystemCache _, IFileSystemNode node)
    {
        Label    = new StringU8(node.Name);
        FullPath = node.FullPath;
        Name     = node.Name.ToString();
    }

    /// <inheritdoc/>
    public void Draw(FileSystemCache cache, IFileSystemNode node, bool temporaryExpansion)
    {
        var folder   = (IFileSystemFolder)node;
        var expanded = temporaryExpansion ? folder.FilterExpanded : folder.Expanded;
        Im.Tree.SetNextOpen(expanded);
        bool ret;
        var  flags = node.Selected ? TreeNodeFlags.NoTreePushOnOpen | TreeNodeFlags.Selected : TreeNodeFlags.NoTreePushOnOpen;
        using (ImGuiColor.Text.Push(expanded ? cache.ExpandedFolderColor : cache.CollapsedFolderColor))
        {
            ImEx.IconTreeNode(Label, flags, node, out ret, new LockedIcon(cache.FileSystem)).Dispose();
        }

        if (ret)
        {
            if (temporaryExpansion)
                cache.FileSystem.ChangeTemporaryExpandedState(folder, !folder.FilterExpanded);
            else
                cache.FileSystem.ChangeExpandedState(folder, !folder.Expanded);
        }

        cache.HandleSelection(node, true);
        ApplyMiddleClick(cache, folder);
        DrawContext(cache, folder);
        IFileSystemNodeCache.DragDrop(cache, node);
    }

    /// <summary> Draw the context menu based on the cache's folder context. </summary>
    private static void DrawContext(FileSystemCache cache, IFileSystemFolder folder)
    {
        if (cache.Parent.FolderContext.Count <= 0)
            return;

        using var popup = Im.Popup.BeginContextItem();
        if (!popup)
            return;

        foreach (var button in cache.Parent.FolderContext)
            button.DrawMenuItem(folder);
    }

    /// <summary> Select or unselect all data node descendants of the folder on middle-click. </summary>
    private static void ApplyMiddleClick(FileSystemCache cache, IFileSystemFolder folder)
    {
        if (!Im.Item.MiddleClicked())
            return;

        bool? isSelected = null;
        foreach (var child in folder.GetDescendants().OfType<IFileSystemData>())
        {
            isSelected ??= child.Selected;
            cache.Parent.FileSystem.ChangeSelectedState(child, !isSelected.Value);
        }
    }
}
