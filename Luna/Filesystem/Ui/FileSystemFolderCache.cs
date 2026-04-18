namespace Luna;

/// <summary> The cached object for folder nodes. </summary>
public sealed class FileSystemFolderCache : IFileSystemNodeCache
{
    public int FlattenedAncestors
    {
        get;
        set => Dirty |= LunaHelpers.SetDifferent(ref field, value);
    }

    /// <inheritdoc/>
    public bool Dirty { get; set; } = true;

    /// <summary> The folder name as a UTF8 string. </summary>
    public StringU8 Label { get; set; } = StringU8.Empty;

    /// <summary> The full path of the folder as a UTF16 string. </summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary> The folder name as a UTF16 string. </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary> The color to use for this folder when it is expanded. </summary>
    public Vector4 ExpandedColor { get; set; }

    /// <summary> The color to use for this folder when it is collapsed. </summary>
    public Vector4 CollapsedColor { get; set; }

    /// <inheritdoc/>
    public void Update(FileSystemCache cache, IFileSystemNode node)
    {
        FullPath = node.FullPath;
        string name;
        ExpandedColor  = ((IFileSystemFolder)node).ExpandedColor.Color?.ToVector() ?? cache.ExpandedFolderColor;
        CollapsedColor = ((IFileSystemFolder)node).CollapsedColor.Color?.ToVector() ?? cache.CollapsedFolderColor;
        if (FlattenedAncestors is 0)
        {
            name = node.Name.ToString();
        }
        else
        {
            var builder = new StringBuilder(256);
            AppendFlattenedPath(builder, node.Parent, FlattenedAncestors - 1);
            builder.Append(node.Name);
            name = builder.ToString();
        }

        Label = new StringU8(name);
        Name  = name;

        return;

        static void AppendFlattenedPath(StringBuilder builder, IFileSystemNode? node, int flattenedAncestors)
        {
            if (node is null)
                return;

            if (flattenedAncestors > 0)
                AppendFlattenedPath(builder, node.Parent, flattenedAncestors - 1);

            builder.Append(node.Name);
            builder.Append('/');
        }
    }

    /// <inheritdoc/>
    public void Draw(FileSystemCache cache, IFileSystemNode node, bool temporaryExpansion)
    {
        var folder   = (IFileSystemFolder)node;
        var expanded = temporaryExpansion ? folder.FilterExpanded : folder.Expanded;
        Im.Tree.SetNextOpen(expanded);
        bool ret;
        var  flags = node.Selected ? TreeNodeFlags.NoTreePushOnOpen | TreeNodeFlags.Selected : TreeNodeFlags.NoTreePushOnOpen;
        using (ImGuiColor.Text.Push(expanded ? ExpandedColor : CollapsedColor))
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
