namespace Luna;

/// <summary> The button to set a folder locked. </summary>
/// <param name="fileSystem"> The file system. </param>
/// <param name="lockString"> The string displayed when this button locks the node on click. </param>
/// <param name="unlockString"> The string displayed when this button unlocks the node on click. </param>
public sealed class LockNodeButton(BaseFileSystem fileSystem, ReadOnlySpan<byte> lockString, ReadOnlySpan<byte> unlockString)
    : BaseButton<IFileSystemData>
{
    public readonly StringU8 LockString   = new(lockString);
    public readonly StringU8 UnlockString = new(unlockString);

    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label(in IFileSystemData node)
        => node.Locked ? UnlockString : LockString;

    /// <inheritdoc/>
    public override void OnClick(in IFileSystemData node)
        => fileSystem.ChangeLockState(node, !node.Locked);

    /// <inheritdoc/>
    public override bool HasTooltip
        => true;

    /// <inheritdoc/>
    public override void DrawTooltip(in IFileSystemData _)
        => Im.Text(
            "Locking an item prevents this item from being dragged to other positions. It does not prevent any other manipulations of the item."u8);
}
