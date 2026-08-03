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

    /// <inheritdoc/>
    public override bool Enabled(in IFileSystemFolder data)
        => LunaStyle.Modifier.Destructive;

    /// <inheritdoc/>
    public override void OnClick(in IFileSystemFolder folder)
    {
        if (!folder.IsRoot)
            fileSystem.Merge(folder, folder.Parent!);
    }
}

/// <summary> The button to dissolve all descendant folders of a folder and move their flattened content into the parent folder, if possible. </summary>
/// <param name="fileSystem"> The file system. </param>
public sealed class DissolveAllFoldersButton(BaseFileSystem fileSystem) : BaseButton<IFileSystemFolder>
{
    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label(in IFileSystemFolder _)
        => "Dissolve All Descendant Folders"u8;

    /// <inheritdoc/>
    public override void DrawTooltip(in IFileSystemFolder _)
    {
        Im.Text("Remove all descendant folders of this folder and move their flattened children into this folder, if possible."u8);
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
        // By iterating in reverse, deeper descendants will be merged into this folder first.
        foreach(var descendant in folder.GetDescendants().OfType<IFileSystemFolder>().Reverse())
            fileSystem.Merge(descendant, folder);
    }
}
