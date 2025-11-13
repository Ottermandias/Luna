namespace Luna;

/// <summary> The button to set a folder locked. </summary>
/// <param name="fileSystem"> The file system. </param>
public sealed class LockFolderButton(BaseFileSystem fileSystem) : BaseButton<IFileSystemFolder>
{
    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label(in IFileSystemFolder folder)
        => folder.Locked ? "Unlock Folder"u8 : "Lock Folder"u8;

    /// <inheritdoc/>
    public override void OnClick(in IFileSystemFolder folder)
        => fileSystem.ChangeLockState(folder, !folder.Locked);

    /// <inheritdoc/>
    public override bool HasTooltip
        => true;

    /// <inheritdoc/>
    public override void DrawTooltip(in IFileSystemFolder _)
        => Im.Text(
            "Locking an item prevents this item from being dragged to other positions. It does not prevent any other manipulations of the item."u8);
}
