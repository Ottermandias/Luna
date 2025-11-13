namespace Luna;

/// <summary> The button to collapse the descendants of a specific folder </summary>
/// <param name="fileSystem"> The file system. </param>
public sealed class CollapseDescendantsButton(BaseFileSystem fileSystem) : BaseButton<IFileSystemFolder>
{
    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label(in IFileSystemFolder _)
        => "Collapse All Descendants"u8;

    /// <inheritdoc/>
    public override void OnClick(in IFileSystemFolder folder)
        => fileSystem.CollapseAllDescendants(folder);

    /// <inheritdoc/>
    public override bool HasTooltip
        => true;

    /// <inheritdoc/>
    public override void DrawTooltip(in IFileSystemFolder _)
        => Im.Text("Successively collapse all folders that descend from this folder, including itself."u8);
}
