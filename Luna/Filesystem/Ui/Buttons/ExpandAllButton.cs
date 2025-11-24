namespace Luna;

/// <summary> The menu item to expand all folders in the file system. </summary>
/// <param name="fileSystem"> The file system. </param>
public sealed class ExpandAllButton(BaseFileSystem fileSystem) : BaseButton
{
    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label
        => "Expand All Folders"u8;

    /// <inheritdoc/>
    public override void OnClick()
        => fileSystem.ExpandAllDescendants(fileSystem.Root);
}
