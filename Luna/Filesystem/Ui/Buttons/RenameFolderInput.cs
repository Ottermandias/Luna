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
    public override bool DrawMenuItem(in IFileSystemFolder data)
    {
        var       currentPath = data.FullPath;
        var       ret         = false;
        using var style       = Im.Style.PushDefault(ImStyleDouble.FramePadding);

        MenuSeparator.DrawSeparator();

        if (Im.Window.Appearing)
            Im.Keyboard.SetFocusHere();
        Im.Text("Rename Folder (Display Only):"u8);
        if (ImEx.InputOnDeactivation.Text("##Display"u8, data.DisplayName ?? string.Empty, out string newName, "Display Name..."u8))
        {
            fileSystem.ChangeFolderDisplayName(data, newName);
            ret = true;
        }

        Im.Tooltip.OnHover("Enter a display name for this folder here. The folder will be sorted according to its path, but display this text as its name.\n"u8
          + "An empty display name means it uses the path as its name.\n\n"u8
          + "Keep in mind that display names do not have to be unique and paths need to be used for referencing a folder."u8);

        MenuSeparator.DrawSeparator();
        Im.Text("Move Folder:"u8);
        if (Im.Input.Text(Label(data), ref currentPath, flags: InputTextFlags.EnterReturnsTrue) && currentPath.Length > 0)
        {
            fileSystem.RenameAndMove(data, currentPath);
            fileSystem.ExpandAllAncestors(data);
            ret = true;
        }

        Im.Tooltip.OnHover("Enter a full path here to move or rename the folder. Creates all required parent directories, if possible."u8);
        return ret;
    }
}
