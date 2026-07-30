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

/// <summary> A menu selector for the global sort-mode. </summary>
/// <param name="drawer"> The parent file system drawer. </param>
/// <param name="configSetter"> An additional method to invoke when the sort mode is changed, e.g. to update the configuration. </param>
public class GlobalSortModeSelector(FileSystemDrawer drawer, Action<ISortMode>? configSetter) : BaseButton
{
    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label
        => "Global Sort Mode"u8;

    /// <inheritdoc/>
    public override bool DrawMenuItem()
    {
        LunaStyle.DrawSeparator();
        Im.Text("Global Sorting:"u8);
        if (!SortModeCombo.DrawCombo(drawer.ValidSortModes, "##sortCombo"u8, drawer.SortMode, out var newSortMode, false,
                180 * Im.Style.GlobalScale))
            return false;

        drawer.SortMode = newSortMode!;
        configSetter?.Invoke(newSortMode!);
        return true;
    }
}
