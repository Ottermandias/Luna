namespace Luna;

/// <summary> The button to create a new folder in a given file system. </summary>
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
