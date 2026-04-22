namespace Luna;

/// <summary> Provides a color picker to specify an individual color for this separator. </summary>
/// <param name="drawer"> The parent drawer. </param>
public sealed class SeparatorColorEdit(FileSystemDrawer drawer) : BaseButton<IFileSystemSeparator>
{
    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label(in IFileSystemSeparator data)
        => "Color Edits"u8;

    /// <inheritdoc/>
    public override bool DrawMenuItem(in IFileSystemSeparator data)
    {
        var ret       = false;
        var lineColor = drawer.FolderLineColor;
        if (ImEx.IconCheckbox("##lineColor"u8, LunaStyle.LockedIcon, data.Color.IsDefault, out var isDefault))
        {
            drawer.FileSystem.ChangeSeparator(data, isDefault ? ColorParameter.Default : lineColor);
            ret = true;
        }

        Im.Tooltip.OnHover(isDefault ? "Use the custom color configured here."u8 : "Use the globally set color for the folder line."u8);

        Im.Line.SameInner();
        using (Im.Disabled(isDefault))
        {
            var color = data.Color.Color?.ToVector() ?? lineColor;
            if (Im.Color.Editor("Separator Color"u8, ref color, ColorEditorFlags.AlphaPreviewHalf | ColorEditorFlags.NoInputs))
            {
                ret = true;
                drawer.FileSystem.ChangeSeparator(data, color);
            }
        }

        return ret;
    }
}
