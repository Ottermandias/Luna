namespace Luna;

/// <summary> A basic cache node for the flattened file system drawer. </summary>
public interface IFileSystemNodeCache
{
    /// <summary> Whether this node needs to be updated. </summary>
    /// <remarks> Should be set by the cache that is using it on events that change cached data. </remarks>
    public bool Dirty { get; set; }

    /// <summary> Update the cached data of this node. </summary>
    /// <param name="cache"> The cache that is drawing this node. </param>
    /// <param name="node"> The original file system node that is drawn. </param>
    /// <remarks> Called by the cache before drawing if <see cref="Dirty"/> is true, in which case the cache sets <see cref="Dirty"/> to false afterward. </remarks>
    public void Update(FileSystemCache cache, IFileSystemNode node);

    /// <summary> Draw this node. </summary>
    /// <param name="cache"> The cache that is drawing this node. </param>
    /// <param name="node"> The original file system node that is drawn. </param>
    /// <remarks> Called inside a <see cref="TreeLine"/>. The drawn object should be a single item for ImGui. </remarks>
    public void Draw(FileSystemCache cache, IFileSystemNode node);

    /// <summary> Draw the drag and drop functionality for file system tree nodes. </summary>
    /// <param name="cache"> The cache that is drawing this node. </param>
    /// <param name="node"> The original file system node that is drawn. </param>
    public static void DragDrop(FileSystemCache cache, IFileSystemNode node)
    {
        if (!cache.Parent.AllowDragAndDrop)
            return;

        DragDropTarget(cache, node);
        DragDropSource(cache, node);
    }

    /// <summary> Draw the drag and drop target for a node. </summary>
    /// <param name="cache"> The cache that is drawing this node. </param>
    /// <param name="node"> The original file system node that is drawn. </param>
    private static void DragDropTarget(FileSystemCache cache, IFileSystemNode node)
    {
        // We manually handle dragging state, so check that first.
        if (cache.DraggedNode is null)
            return;

        // Draw the actual target and check it via ImGui functionality.
        using var target = Im.DragDrop.Target();
        if (!target.IsDropping("dd"u8))
            return;

        // Apply the drop to the folder or the parent of the data node.
        var newParent = node as IFileSystemFolder ?? node.Parent!;
        foreach (var drag in cache.DraggedNodes)
            cache.FileSystem.Move(drag, newParent);
        cache.ClearDragDrop();
        FileSystemCache.KeepDragAlive = false;
    }

    /// <summary> Draw the drag and drop source for a node. </summary>
    /// <param name="cache"> The cache that is drawing this node. </param>
    /// <param name="node"> The original file system node that is drawn. </param>
    private static void DragDropSource(FileSystemCache cache, IFileSystemNode node)
    {
        // Ignore locked nodes.
        if (node.Locked)
            return;

        // Draw the actual source. If this fails, reset the state.
        using var source = Im.DragDrop.Source();
        if (!source.Success)
        {
            if (cache.DraggedNode != node)
                return;

            if (FileSystemCache.KeepDragAlive)
                FileSystemCache.KeepDragAlive = false;
            else
                cache.ClearDragDrop();
            return;
        }

        // Only update the dragging for different nodes.
        if (cache.DraggedNode != node)
        {
            source.SetPayload("dd"u8);
            cache.SetDragDrop(node);
        }

        FileSystemCache.KeepDragAlive = true;
        if (cache.DraggedNodes.Count is 1)
        {
            Im.Text($"Moving {cache.DraggedNodes.First().FullPath}...");
        }
        else
        {
            Im.Text("Moving ..."u8);
            foreach (var n in cache.DraggedNodes)
                Im.BulletText(n.FullPath);
        }
    }
}
