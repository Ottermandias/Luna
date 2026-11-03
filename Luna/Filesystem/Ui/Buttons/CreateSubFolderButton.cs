namespace Luna;

/// <summary> The button to move the current selection into the respective folder. </summary>
/// <param name="fileSystem"> The file system. </param>
public sealed class CreateSubFolderButton(BaseFileSystem fileSystem) : BaseButton<IFileSystemFolder>
{
    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label(in IFileSystemFolder _)
        => "Create Subfolder here"u8;

    /// <inheritdoc/>
    public override void OnClick(in IFileSystemFolder folder)
        => Im.Popup.Open($"CSF{folder.Identifier.Value}");

    /// <inheritdoc/>
    protected override bool MenuItem(in IFileSystemFolder data)
        => Im.Selectable(Label(data), false, SelectableFlags.NoAutoClosePopups);

    /// <inheritdoc/>
    public override bool HasTooltip
        => true;

    /// <inheritdoc/>
    public override void DrawTooltip(in IFileSystemFolder _)
        => Im.Text("Create a new empty folder that is a subfolder of this one. Can contain '/' to create multiple nested subfolders."u8);

    /// <inheritdoc/>
    protected override void PostDraw(in IFileSystemFolder parentFolder)
    {
        // Handle the actual popup.
        if (!InputPopup.Open($"CSF{parentFolder.Identifier.Value}", StringU8.Empty, out var newName, "Enter Subfolder Name..."u8))
            return;

        try
        {
            var fullPath = $"{parentFolder.FullPath}/{newName.AsSpan().Trim('/')}";
            var folder   = fileSystem.FindOrCreateAllFolders(fullPath);
            fileSystem.ExpandAllAncestors(folder);
        }
        catch
        {
            // Ignored
        }
    }
}
