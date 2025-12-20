namespace Luna;

public static partial class LunaStyle
{
    /// <summary> Draw a help marker. </summary>
    /// <param name="color"> The color to use. If null, <see cref="ImGuiColor.TextDisabled"/> will be used.</param>
    /// <returns> True if the help marker is hovered by the mouse cursor in this frame. </returns>
    public static bool DrawHelpMarker(ColorParameter color = default)
    {
        ImEx.Icon.Draw(HelpMarker, color.CheckDefault(ImGuiColor.TextDisabled));
        return Im.Item.Hovered(HoveredFlags.AllowWhenDisabled);
    }

    /// <summary> Draw a help marker aligned to frame padding. </summary>
    /// <inheritdoc cref="DrawHelpMarker(ColorParameter)"/>
    public static bool DrawAlignedHelpMarker(ColorParameter color = default)
    {
        Im.Cursor.FrameAlign();
        ImEx.Icon.Draw(HelpMarker, color.CheckDefault(ImGuiColor.TextDisabled));
        return Im.Item.Hovered(HoveredFlags.AllowWhenDisabled);
    }

    /// <summary> Draw a help marker with a following label, and a tooltip if either of them is hovered. </summary>
    /// <param name="label"> The label to draw as text. Does not have to null-terminated. </param>
    /// <param name="tooltip"> The tooltip to draw when the help marker or the label are hovered.</param>
    /// <param name="color"> The color for the help marker. </param>
    public static void DrawHelpMarker(Utf8LabelHandler label, Utf8TextHandler tooltip, ColorParameter color = default)
    {
        var hovered = DrawHelpMarker(color);
        Im.Line.SameInner();
        Im.Text(ref label);
        if (hovered || Im.Item.Hovered())
        {
            using var tt = Im.Tooltip.Begin();
            Im.Text(ref tooltip);
        }
    }

    /// <summary> Draw a help marker with a following label, both aligned to frame padding, and a tooltip if either of them is hovered. </summary>
    /// <inheritdoc cref="DrawHelpMarker(Utf8LabelHandler,Utf8TextHandler,ColorParameter)"/>
    public static void DrawAlignedHelpMarker(Utf8LabelHandler label, Utf8TextHandler tooltip, ColorParameter color = default)
    {
        var hovered = DrawAlignedHelpMarker(color);
        Im.Line.SameInner();
        ImEx.TextFrameAligned(ref label);
        if (hovered || Im.Item.Hovered())
        {
            using var tt = Im.Tooltip.Begin();
            Im.Text(ref tooltip);
        }
    }

    /// <summary> Draw a help marker with a tooltip if it is hovered. </summary>
    /// <param name="tooltip"> The tooltip to draw when the help marker is hovered.</param>
    /// <param name="color"> The color for the help marker. </param>
    /// <param name="treatAsHovered"> Pass this if the tooltip should be shown regardless of hover state of the help marker. </param>
    public static void DrawHelpMarker(Utf8TextHandler tooltip, ColorParameter color = default, bool treatAsHovered = false)
    {
        if (DrawHelpMarker(color) || treatAsHovered)
        {
            using var tt = Im.Tooltip.Begin();
            Im.Text(ref tooltip);
        }
    }

    /// <summary> Draw a help marker aligned to frame padding with a tooltip if it is hovered. </summary>
    /// <inheritdoc cref="DrawHelpMarker(Utf8TextHandler,ColorParameter,bool)"/>
    public static void DrawAlignedHelpMarker(Utf8TextHandler tooltip, ColorParameter color = default, bool treatAsHovered = false)
    {
        if (DrawAlignedHelpMarker(color) || treatAsHovered)
        {
            using var tt = Im.Tooltip.Begin();
            Im.Text(ref tooltip);
        }
    }

    /// <summary> Draw a help marker on the current line with a following label, and a tooltip if either of them or the last drawn item is hovered. </summary>
    /// <param name="label"> The label to draw as text. Does not have to null-terminated. </param>
    /// <param name="tooltip"> The tooltip to draw when the help marker or the label are hovered.</param>
    /// <param name="color"> The color for the help marker. </param>
    public static void DrawHelpMarkerLabel(Utf8LabelHandler label, Utf8TextHandler tooltip, ColorParameter color = default)
    {
        Im.Line.SameInner();
        var hovered = Im.Item.Hovered(HoveredFlags.AllowWhenDisabled) | DrawHelpMarker(color);
        Im.Line.SameInner();
        Im.Text(ref label);
        if (hovered || Im.Item.Hovered())
        {
            using var tt = Im.Tooltip.Begin();
            Im.Text(ref tooltip);
        }
    }

    /// <summary> Draw a help marker on the current line with a following label, both aligned to frame padding, and a tooltip if either of them or the last drawn item is hovered. </summary>
    /// <inheritdoc cref="DrawHelpMarkerLabel"/>
    public static void DrawAlignedHelpMarkerLabel(Utf8LabelHandler label, Utf8TextHandler tooltip, ColorParameter color = default)
    {
        Im.Line.SameInner();
        var hovered = Im.Item.Hovered(HoveredFlags.AllowWhenDisabled) | DrawAlignedHelpMarker(color);
        Im.Line.SameInner();
        ImEx.TextFrameAligned(ref label);
        if (hovered || Im.Item.Hovered())
        {
            using var tt = Im.Tooltip.Begin();
            Im.Text(ref tooltip);
        }
    }

    /// <summary> Draw a right-aligned help marker on the current line, and a tooltip if the last item or the help marker are hovered. </summary>
    /// <param name="tooltip"> The tooltip to draw. </param>
    /// <param name="color"> The color for the help marker. </param>
    public static void DrawRightAlignedHelpMarker(Utf8TextHandler tooltip, ColorParameter color = default)
    {
        var hovered = Im.Item.Hovered(HoveredFlags.AllowWhenDisabled);
        Im.Line.Same();
        using (AwesomeIcon.Font.Push())
        {
            ImEx.Icon.Draw(HelpMarker, color.CheckDefault(ImGuiColor.TextDisabled));
        }

        if (hovered || Im.Item.Hovered())
        {
            using var tt = Im.Tooltip.Begin();
            Im.Text(ref tooltip);
        }
    }
}
