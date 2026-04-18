namespace Luna;

public static class SortModeCombo
{
    /// <summary> Different supported sort modes as a combo. </summary>
    /// <param name="modes"> The supported modes. </param>
    /// <param name="label"> The label for the combo as text. If this is UTF8, HAS to be null-terminated. </param>
    /// <param name="currentSortMode"> The currently selected sort mode, if any. </param>
    /// <param name="newSortMode"> The newly selected sort mode if this returns true. This can only return null if <paramref name="withUseGlobal"/> is enabled and selected. </param>
    /// <param name="withUseGlobal"> Whether to provide an entry that corresponds to no value, 'Use Global Sorting'. </param>
    /// <param name="width"> The width to use for this combo. </param>
    /// <returns> True if a different sort mode was selected in this frame. </returns>
    public static bool DrawCombo(IEnumerable<ISortMode> modes, Utf8LabelHandler label, ISortMode? currentSortMode, out ISortMode? newSortMode,
        bool withUseGlobal,
        float width)
    {
        Im.Item.SetNextWidth(width);
        var       name  = currentSortMode is null ? "Use Global Sorting"u8 : currentSortMode.Name;
        var       ret   = false;
        using var combo = Im.Combo.Begin(label, name);
        newSortMode = null;
        if (withUseGlobal && currentSortMode is not null)
        {
            Im.Tooltip.OnHover("Control + Right-Click to remove individual sort mode and use global sorting.");
            if (Im.Item.RightClicked() && Im.Io.KeyControl)
                ret = true;
        }

        if (!combo)
            return ret;

        if (withUseGlobal)

        {
            if (Im.Selectable("Use Global Sorting"u8, currentSortMode is null) && currentSortMode is not null)
                ret = true;

            Im.Tooltip.OnHover("Use the sort mode defined for the entire file system and no custom sort mode for this folder."u8);
        }

        foreach (var val in modes)
        {
            if (Im.Selectable(val.Name, val.Equals(currentSortMode)) && !val.Equals(currentSortMode))
            {
                newSortMode = val;
                ret         = true;
            }

            Im.Tooltip.OnHover(val.Description);
        }

        return ret;
    }
}
