namespace Luna;

/// <summary> A menu selector for folder-specific sort-modes. </summary>
/// <param name="drawer"> The parent file system drawer. </param>
public class SortModeSelector(FileSystemDrawer drawer) : BaseButton<IFileSystemFolder>
{
    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label(in IFileSystemFolder data)
        => "Sort Mode"u8;

    /// <inheritdoc/>
    public override bool DrawMenuItem(in IFileSystemFolder data)
    {
        if (!SortModeCombo.DrawCombo(drawer.ValidSortModes, "Individual Folder Sorting"u8, data.SortMode, out var newSortMode, true,
                180 * Im.Style.GlobalScale))
            return false;

        drawer.FileSystem.ChangeFolderSortMode(data, newSortMode);
        return true;
    }
}
