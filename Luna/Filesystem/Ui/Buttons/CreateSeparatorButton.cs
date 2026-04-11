namespace Luna;

/// <summary> The button to create a new separator in a given file system. </summary>
/// <param name="fileSystem"> The file system. </param>
public sealed class CreateSeparatorButton(BaseFileSystem fileSystem) : BaseIconButton<AwesomeIcon>
{
    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label
        => "CS"u8;

    /// <inheritdoc/>
    public override AwesomeIcon Icon
        => LunaStyle.AddSeparatorIcon;

    /// <inheritdoc/>
    public override void DrawTooltip()
        => Im.Text("Create a new separator line. Can contain '/' to create a directory structure. The name will only be used for sorting."u8);

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

        var name       = newName.AsSpan();
        var folderPath = ReadOnlySpan<char>.Empty;
        var index      = newName.LastIndexOf('/');
        if (index >= 0)
        {
            name       = index == newName.Length - 1 ? string.Empty : newName.AsSpan(index + 1);
            folderPath = newName.AsSpan(0, index);
        }

        try
        {
            var folder = folderPath.Length is 0 ? fileSystem.Root : fileSystem.FindOrCreateAllFolders(folderPath);
            fileSystem.CreateSeparator(folder, name, ColorParameter.Default, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), false);
            fileSystem.ExpandAllAncestors(folder);
        }
        catch
        {
            // ignored
        }
    }
}
