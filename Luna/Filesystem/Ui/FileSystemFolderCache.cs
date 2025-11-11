namespace Luna;

/// <summary> The cached object for folder nodes. </summary>
public sealed class FileSystemFolderCache : IFileSystemNodeCache
{
    /// <inheritdoc/>
    public bool Dirty { get; set; } = true;

    /// <summary> The folder name as a UTF8 string. </summary>
    public StringU8 Label { get; set; } = StringU8.Empty;

    /// <inheritdoc/>
    public void Update(FileSystemCache _, IFileSystemNode node)
        => Label = new StringU8(node.Name);

    /// <inheritdoc/>
    public bool Draw(FileSystemCache cache, IFileSystemNode node)
    {
        var folder = (IFileSystemFolder)node;
        Im.Tree.SetNextOpen(folder.Expanded);
        Im.Tree.Node(Label, TreeNodeFlags.NoTreePushOnOpen).Dispose();
        var ret = Im.Tree.ToggledOpen();
        if (ret)
            cache.FileSystem.ChangeExpandedState(folder, !folder.Expanded);
        DrawContext(cache, folder);
        IFileSystemNodeCache.DragDrop(cache, node);

        return ret;
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
}
