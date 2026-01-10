namespace Luna;

/// <summary> The basic cache type for a file system node. </summary>
/// <typeparam name="TSelf"> The own type. </typeparam>
public abstract class BaseFileSystemNodeCache<TSelf> : IFileSystemNodeCache
    where TSelf : BaseFileSystemNodeCache<TSelf>
{
    /// <summary> Whether the drawer should call <see cref="Update"/> before drawing this node. </summary>
    public bool Dirty { get; set; } = true;

    /// <summary> The method to update the cache for the given node. </summary>
    /// <param name="cache"> The parent cache. </param>
    /// <param name="node"> The original file system node this is used for. </param>
    public virtual void Update(FileSystemCache cache, IFileSystemNode node)
    { }

    /// <inheritdoc/>
    public void Draw(FileSystemCache cache, IFileSystemNode node, bool _)
    {
        DrawInternal((FileSystemCache<TSelf>)cache, node);
        cache.HandleSelection(node, true);
        DrawContext(cache, (IFileSystemData)node);
        IFileSystemNodeCache.DragDrop(cache, node);
    }

    /// <summary> The internal draw method that handles the actual ImGui drawing of the node. </summary>
    /// <param name="cache"> The parent cache. </param>
    /// <param name="node"> The original file system node this is used for. </param>
    protected virtual void DrawInternal(FileSystemCache<TSelf> cache, IFileSystemNode node)
    {
        const TreeNodeFlags baseFlags = TreeNodeFlags.NoTreePushOnOpen;
        var                 flags     = node.Selected ? baseFlags | TreeNodeFlags.Selected : baseFlags;
        Im.Tree.Leaf(node.Name, flags);
    }

    /// <summary> Handle the context menu for a node based on the parent's data context. </summary>
    private static void DrawContext(FileSystemCache cache, IFileSystemData node)
    {
        if (cache.Parent.DataContext.Count is 0)
            return;

        using var popup = Im.Popup.BeginContextItem();
        if (!popup)
            return;

        foreach (var button in cache.Parent.DataContext)
            button.DrawMenuItem(node);
    }
}
