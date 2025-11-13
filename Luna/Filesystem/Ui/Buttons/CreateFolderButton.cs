namespace Luna;

/// <summary> The button to create a new folder in a given file system. </summary>
/// <param name="fileSystem"> The file system. </param>
public sealed class CreateFolderButton(BaseFileSystem fileSystem) : BaseIconButton<AwesomeIcon>
{
    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label
        => "CF"u8;

    /// <inheritdoc/>
    public override AwesomeIcon Icon
        => LunaStyle.AddFolderIcon;

    /// <inheritdoc/>
    public override void DrawTooltip()
        => Im.Text("Create a new, empty folder. Can contain '/' to create a directory structure."u8);

    /// <inheritdoc/>
    public override bool HasTooltip
        => true;

    /// <inheritdoc/>
    public override void OnClick()
        => Im.Popup.Open(Label);

    /// <inheritdoc/>
    protected override void PostDraw()
    {
        // Handle the actual popup.
        if (!InputPopup.OpenName(Label, out var newName))
            return;

        try
        {
            var folder = fileSystem.FindOrCreateAllFolders(newName);
            fileSystem.ExpandAllAncestors(folder);
        }
        catch
        {
            // Ignored
        }
    }
}
