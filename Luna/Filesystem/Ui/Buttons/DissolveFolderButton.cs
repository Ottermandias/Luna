namespace Luna;

/// <summary> The button to dissolve a folder and move all its content into the parent folder. </summary>
/// <param name="fileSystem"> The file system. </param>
public sealed class DissolveFolderButton(BaseFileSystem fileSystem) : BaseButton<IFileSystemFolder>
{
    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label(in IFileSystemFolder _)
        => "Dissolve Folder"u8;

    /// <inheritdoc/>
    public override void DrawTooltip(in IFileSystemFolder _)
    {
        Im.Text("Remove this folder and move all its children to its parent-folder, if possible."u8);
        if (!LunaStyle.Modifier.Destructive)
            Im.Text($"\nHold {LunaStyle.Modifier.Destructive} while clicking to dissolve.");
    }

    /// <inheritdoc/>
    public override bool HasTooltip
        => true;

    public override bool Enabled(in IFileSystemFolder data)
        => LunaStyle.Modifier.Destructive;

    /// <inheritdoc/>
    public override void OnClick(in IFileSystemFolder folder)
    {
        if (!folder.IsRoot)
            fileSystem.Merge(folder, folder.Parent!);
    }
}
