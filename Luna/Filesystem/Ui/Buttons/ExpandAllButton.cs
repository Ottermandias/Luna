namespace Luna;

/// <summary> The menu item to expand all folders in the file system. </summary>
/// <param name="fileSystem"> The file system. </param>
/// <param name="filter"> The filter used by the file system drawer. </param>
public sealed class ExpandAllButton(BaseFileSystem fileSystem, IFilter filter) : BaseButton
{
    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label
        => "Expand All Folders"u8;

    /// <inheritdoc/>
    public override void OnClick()
        => fileSystem.ExpandAllDescendants(fileSystem.Root, !filter.IsEmpty);
}
