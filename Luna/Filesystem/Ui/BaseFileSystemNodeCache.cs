namespace Luna;

public abstract class BaseFileSystemNodeCache<TSelf> : IFileSystemNodeCache
    where TSelf : BaseFileSystemNodeCache<TSelf>
{
    public bool Dirty { get; set; } = true;

    public virtual void Update(FileSystemCache cache, IFileSystemNode node)
    { }

    public bool Draw(FileSystemCache cache, IFileSystemNode node)
    {
        var ret = DrawInternal((FileSystemCache<TSelf>)cache, node);
        DrawContext(cache, (IFileSystemData)node);
        IFileSystemNodeCache.DragDrop(cache, node);
        return ret;
    }

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

    protected virtual bool DrawInternal(FileSystemCache<TSelf> _, IFileSystemNode node)
    {
        Im.Tree.Node(node.Name, TreeNodeFlags.NoTreePushOnOpen | TreeNodeFlags.Leaf | TreeNodeFlags.Bullet).Dispose();
        return Im.Item.Clicked();
    }
}
