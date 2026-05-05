namespace Luna;

/// <summary> Provides a checkbox to toggle the separator state of a folder. </summary>
/// <param name="drawer"> The parent drawer. </param>
public sealed class FolderAsSeparatorCheckbox(FileSystemDrawer drawer) : BaseButton<IFileSystemFolder>
{
    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label(in IFileSystemFolder data)
        => "IsSeparator"u8;

    /// <inheritdoc/>
    public override bool DrawMenuItem(in IFileSystemFolder data)
    {
        if (Im.Checkbox("Draw Folder as Separator"u8, data.DrawAsSeparator))
            drawer.FileSystem.ChangeFolderSeparatorState(data, !data.DrawAsSeparator);

        Im.Tooltip.OnHover(
            "When this is enabled, a folder will instead be displayed as a separator line using the expanded color. It will always be expanded and all its children will be shown under the separator but not indented."u8);
        return false;
    }
}
