namespace Luna;

/// <summary> A text input to rename folders in the context menu. </summary>
/// <param name="fileSystem"> The file system. </param>
public sealed class RenameFolderInput(BaseFileSystem fileSystem) : BaseButton<IFileSystemFolder>
{
    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label(in IFileSystemFolder _)
        => "##Rename"u8;

    /// <summary> Replaces the normal menu item handling for a text input, so the other fields are not used. </summary>
    /// <inheritdoc/>
    public override bool DrawMenuItem(in IFileSystemFolder folder)
    {
        MenuSeparator.DrawSeparator();

        var currentPath = folder.FullPath;
        var ret         = false;
        if (Im.Window.Appearing)
            Im.Keyboard.SetFocusHere();

        if (Im.Input.Text(Label(folder), ref currentPath, flags: InputTextFlags.EnterReturnsTrue))
        {
            fileSystem.RenameAndMove(folder, currentPath);
            fileSystem.ExpandAllAncestors(folder);
            ret = true;
        }

        Im.Tooltip.OnHover("Enter a full path here to move or rename the folder. Creates all required parent directories, if possible."u8);
        return ret;
    }
}
