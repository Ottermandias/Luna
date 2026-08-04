namespace Luna;

/// <summary> The button to move the current selection into the respective folder. </summary>
/// <param name="fileSystem"> The file system. </param>
public sealed class MoveSelectionButton(BaseFileSystem fileSystem) : BaseButton<IFileSystemFolder>
{
    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label(in IFileSystemFolder _)
        => "Move Selection Here"u8;

    /// <inheritdoc/>
    public override bool Enabled(in IFileSystemFolder data)
        => LunaStyle.Modifier.Misclick;

    /// <inheritdoc/>
    public override void OnClick(in IFileSystemFolder folder)
    {
        foreach (var obj in fileSystem.Selection.OrderedNodes)
        {
            if (obj != folder && obj.Parent != folder && folder.GetAncestors().All(a => a != obj))
                fileSystem.Move(obj, folder);
        }
    }

    /// <inheritdoc/>
    public override bool HasTooltip
        => true;

    /// <inheritdoc/>
    public override void DrawTooltip(in IFileSystemFolder _)
    {
        Im.Text("Move your current selection into this folder if possible. Ancestors of this folder are ignored."u8);
        if (!LunaStyle.Modifier.Misclick)
            Im.Text($"\nHold {LunaStyle.Modifier.Misclick} while clicking to dissolve.");
    }
}
