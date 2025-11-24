namespace Luna;

/// <summary> The menu item to collapse all folders in the file system. </summary>
/// <param name="fileSystem"> The file system. </param>
public sealed class CollapseAllButton(BaseFileSystem fileSystem) : BaseButton
{
    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label
        => "Collapse All Folders"u8;

    /// <inheritdoc/>
    public override void OnClick()
        => fileSystem.CollapseAllDescendants(fileSystem.Root);
}
