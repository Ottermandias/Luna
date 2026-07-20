namespace Luna;

/// <summary> A widget to draw a single separator line with a collapsible label and optionally a combo preview to the right of it. </summary>
public struct HeaderLine
{
    /// <summary> The delegate to draw a custom combo. </summary>
    /// <typeparam name="TCacheItem"> The type of the selection for the combo. </typeparam>
    /// <param name="preview"> The current preview text of the combo, as passed by the header line. </param>
    /// <param name="comboWidth"> The width for the combo, as passed by the header line. </param>
    /// <param name="selection"> When returning true, the new selection of the combo. </param>
    /// <returns> True when a new item is selected. </returns>
    public delegate bool DrawCombo<TCacheItem>(Utf8HintHandler preview, float comboWidth, out TCacheItem? selection);

    /// <summary> The color of the header line when the header is collapsed. </summary>
    public ColorParameter LineColorCollapsed;

    /// <summary> The color of the header line when the header is expanded. </summary>
    public ColorParameter LineColorExpanded;

    /// <summary> The color of the label text when the header is collapsed. </summary>
    public ColorParameter TextColorCollapsed;

    /// <summary> The color of the label text when the header is expanded. </summary>
    public ColorParameter TextColorExpanded;

    /// <summary> The color for the tooltip icon, if any. </summary>
    public ColorParameter TooltipIconColor;

    /// <summary> The background color of the label and expansion button when expanded. </summary>
    public ColorParameter ButtonBackgroundExpanded;

    /// <summary> The background color of the label and expansion button when collapsed. </summary>
    public ColorParameter ButtonBackgroundCollapsed;

    /// <summary> The background color of the label and expansion button when it is hovered. </summary>
    public ColorParameter ButtonHovered;

    /// <summary> The background color of the label and expansion button when it is clicked. </summary>
    public ColorParameter ButtonActive;

    /// <summary> The icon to use if the header has a tooltip, if any. </summary>
    public AwesomeIcon TooltipIcon;

    /// <summary> If this is 0 the label and expansion button auto-fits to the text and caret. Otherwise, this provides a minimum width. </summary>
    public float FixedButtonWidth;

    /// <summary> When drawing a combo, this provides a minimum width for the combo preview. </summary>
    public float FixedComboWidth;

    /// <summary> The length of the line to the left of the label and expansion button, which is shifted this far to the right. </summary>
    public float LeftDistance;

    /// <summary> When drawing a combo, the minimum distance between the label and expansion button and the combo preview, with the latter being shifted to the right. </summary>
    public float ComboDistance;

    /// <summary>
    ///   If this is non-negative, the length of the line to the right of the combo preview, with the center distance being expanded for any additional available space.
    ///   If this is negative, the combo preview is placed exactly <see cref="ComboDistance"/> to the right of the label and expansion button, and the right line is expanded for any additional available space.
    /// </summary>
    public float RightDistance;

    /// <summary> Whether the header should be collapsed by default if no interaction has taken place. Normally they start expanded. </summary>
    public bool DefaultClosed;

    /// <summary> Whether the header is collapsible at all. </summary>
    public bool Collapsible;

    /// <summary> Whether the combo preview should be drawn disabled. </summary>
    public bool ComboDisabled;

    /// <summary> When this is true, no header, label or collapse button will be drawn at all. The distances will be computed using FixedButtonWidth. </summary>
    /// <remarks> This is not respected for <see cref="Basic"/>, as that would just be a separator. </remarks>
    public bool NoLabel;

    /// <summary> Whether the rightmost part of the line is to be drawn or not. </summary>
    public bool HideRightLine;

    /// <summary> Draw a header line with a label and expansion button as well as a custom combo. </summary>
    /// <typeparam name="TCacheItem"> The combo item type. </typeparam>
    /// <param name="drawer"> The method to draw the combo preview. </param>
    /// <param name="label"> The label of the header. </param>
    /// <param name="tooltip"> A tooltip when hovering the header and expansion button. </param>
    /// <param name="preview"> The current preview text for the combo preview. </param>
    /// <returns> Whether the header is expanded or not, whether a selection has taken place, and the newly selected item if a selection has taken place. </returns>
    public (bool Expanded, bool Changed, TCacheItem? Selection) Combo<TCacheItem>(DrawCombo<TCacheItem> drawer, Utf8LabelHandler label,
        Utf8TextHandler tooltip, Utf8HintHandler preview)
    {
        var (expanded, changed, _, _, selection) = ComboInternal(drawer, 0, ref label, ref tooltip, ref preview);
        return (expanded, changed, selection);
    }

    /// <summary> Draw a header line with a label and expansion button as well as a custom combo. </summary>
    /// <param name="drawer"> The action to draw the custom combo given its desired width. </param>
    /// <param name="width"> The intended width of the drawn combo. If no <see cref="FixedComboWidth"/> is set, this is passed to the action. Otherwise, it may be replaced by the fixed width. </param>
    /// <param name="label"> The label of the header. </param>
    /// <param name="tooltip"> A tooltip when hovering the header and expansion button. </param>
    /// <returns> Whether the header is expanded or not. The combo action is expected to handle all interactivity itself. </returns>
    public bool Combo(Action<float> drawer, float width, Utf8LabelHandler label, Utf8TextHandler tooltip)
    {
        Utf8HintHandler hint = StringU8.Empty;
        var (expanded, _, _, _, _) = ComboInternal<object>(Drawer, width, ref label, ref tooltip, ref hint);
        return expanded;

        bool Drawer(Utf8HintHandler _, float comboWidth, out object? selection)
        {
            selection = null;
            drawer(comboWidth);
            return false;
        }
    }

    /// <summary> Draw a header line with a label and expansion button as well as a default combo preview. </summary>
    /// <param name="label"> The label of the header. </param>
    /// <param name="tooltip"> A tooltip when hovering the header and expansion button. </param>
    /// <param name="preview"> The current preview text for the combo preview. </param>
    /// <returns> Whether the header is expanded or not, the ID for the popup opened by the combo preview and the bounding box of the combo to pass to the combo popup. </returns>
    public (bool Expanded, ImGuiId ComboId, Rectangle ComboBoundingBox) Combo(Utf8LabelHandler label, Utf8TextHandler tooltip,
        Utf8HintHandler preview)
    {
        var (expanded, _, comboId, comboBoundingBox, _) = ComboInternal<object>(null, 0, ref label, ref tooltip, ref preview);
        return (expanded, comboId, comboBoundingBox);
    }

    /// <summary> Draw a header line with a label and expansion button. </summary>
    /// <param name="label"> The label of the header. </param>
    /// <param name="tooltip"> A tooltip when hovering the header and expansion button. </param>
    /// <returns> Whether the header is expanded or not. </returns>
    public bool Basic(Utf8LabelHandler label, Utf8TextHandler tooltip)
    {
        if (!ImEx.SplitLabel(ref label, out var visible, out var id))
            return false;

        using var _        = Im.Id.Push(id);
        var       expanded = !Collapsible || Im.State.Storage.GetBool(id, !DefaultClosed);
        var (separatorColor, textColor, buttonBackground, caret) = GetColors(expanded);
        var (frameHeight, lineThickness, linePosition)           = GetLineData();
        var tooltipSpan = tooltip.GetSpan(out var s) ? s : StringU8.Empty;
        var (textWidth, tooltipIconWidth, buttonWidth) = GetButtonWidth(visible, tooltipSpan);
        DrawTwoLines(separatorColor, lineThickness, linePosition, buttonWidth);

        using (PushData(separatorColor, lineThickness, buttonBackground))
        {
            DrawLabel(visible, tooltipSpan, id, tooltipIconWidth, textWidth, buttonWidth, 0, frameHeight, textColor, buttonBackground, caret,
                false);
        }

        Im.Tooltip.OnHover(tooltip);

        return expanded;
    }

    private Im.ColorStyleDisposable PushData(Rgba32 separator, float lineThickness, Rgba32 buttonBackground)
        => ImStyleBorder.Frame.Push(separator, lineThickness)
            .Push(ImGuiColor.Button,        buttonBackground)
            .Push(ImGuiColor.ButtonHovered, ButtonHovered.CheckDefault(ImGuiColor.FrameBackgroundHovered))
            .Push(ImGuiColor.ButtonActive,  ButtonHovered.CheckDefault(ImGuiColor.FrameBackgroundActive));


    [MethodImpl(ImSharpConfiguration.OptInl)]
    private (bool Expanded, bool Changed, ImGuiId ComboId, Rectangle ComboBoundingBox, TCacheItem? Selection) ComboInternal<TCacheItem>(
        DrawCombo<TCacheItem>? drawer, float width, ref Utf8LabelHandler label, ref Utf8TextHandler tooltip, ref Utf8HintHandler preview)
    {
        if (!ImEx.SplitLabel(ref label, out var visible, out var id))
            return (false, false, 0, default, default);

        using var _        = Im.Id.Push(id);
        var       expanded = !Collapsible || Im.State.Storage.GetBool(id, !DefaultClosed);
        var (separatorColor, textColor, buttonBackground, caret) = GetColors(expanded);
        var (frameHeight, lineThickness, linePosition)           = GetLineData();
        var tooltipSpan = tooltip.GetSpan(out var s) ? s : StringU8.Empty;
        var (textWidth, tooltipIconWidth, buttonWidth) = GetButtonWidth(visible, tooltipSpan);
        var (comboWidth, centerDistance)               = GetComboWidth(frameHeight, width, buttonWidth, ref preview);
        DrawThreeLines(separatorColor, lineThickness, linePosition, buttonWidth, comboWidth, centerDistance);

        ImGuiId     popupId;
        Rectangle   boundingBox;
        bool        change;
        TCacheItem? selection;
        using (var style = PushData(separatorColor, lineThickness, buttonBackground))
        {
            using var group = Im.Group();
            DrawLabel(visible,    tooltipSpan, id, tooltipIconWidth, textWidth, buttonWidth, centerDistance, frameHeight, textColor,
                buttonBackground, caret,       NoLabel);
            style.PopColor(3);
            if (!NoLabel)
                Im.Line.Same(0, centerDistance);
            using var disabled = Im.Disabled(ComboDisabled);
            if (drawer is null)
            {
                Im.Item.SetNextWidth(comboWidth);
                Im.Combo.DrawPreview("##combo"u8, preview, out popupId, out boundingBox);
                change    = false;
                selection = default;
            }
            else
            {
                change      = drawer(preview, comboWidth, out selection);
                popupId     = ImGuiId.Invalid;
                boundingBox = Rectangle.Zero;
            }
        }

        Im.Tooltip.OnHover(tooltip);

        return (expanded, change, popupId, boundingBox, selection);
    }

    [MethodImpl(ImSharpConfiguration.OptInl)]
    private (float Text, float Tooltip, float Button) GetButtonWidth(ReadOnlySpan<byte> text, ReadOnlySpan<byte> tooltip)
    {
        var textWidth    = Im.Font.CalculateSize(text, false).X;
        var buttonWidth  = Im.Font.CalculateSize(text, false).X + 2 * Im.Style.FramePadding.X;
        var tooltipWidth = 0f;
        if (Collapsible)
            buttonWidth += Im.Style.TextHeight + Im.Style.ItemInnerSpacing.X;
        if (!tooltip.IsEmpty && !TooltipIcon.IsEmpty)
            buttonWidth += (tooltipWidth = TooltipIcon.CalculateSize().X) + Im.Style.ItemInnerSpacing.X;
        if (FixedButtonWidth is not 0 && FixedButtonWidth > textWidth)
            buttonWidth = FixedButtonWidth;
        return (textWidth, tooltipWidth, buttonWidth);
    }

    [MethodImpl(ImSharpConfiguration.OptInl)]
    private (Rgba32 Separator, Rgba32 Text, Rgba32 Button, Direction Caret) GetColors(bool expanded)
        => expanded
            ? (LineColorExpanded.CheckDefault(ImGuiColor.Separator), TextColorExpanded.CheckDefault(ImGuiColor.Text),
                ButtonBackgroundExpanded.CheckDefault(ImGuiColor.FrameBackground),
                Direction.Down)
            : (LineColorCollapsed.CheckDefault(ImGuiColor.Separator), TextColorCollapsed.CheckDefault(ImGuiColor.Text),
                ButtonBackgroundCollapsed.CheckDefault(ImGuiColor.FrameBackground),
                Direction.Right);

    [MethodImpl(ImSharpConfiguration.OptInl)]
    private (float FrameHeight, float Thickness, float Position) GetLineData()
    {
        var frameHeight = Im.Style.FrameHeight;
        return float.IsEvenInteger(frameHeight) ? (frameHeight, 2, frameHeight / 2) : (frameHeight, 1, (frameHeight - 1) / 2);
    }

    [MethodImpl(ImSharpConfiguration.OptInl)]
    public (float Combo, float Center) GetComboWidth(float frameHeight, float width, float buttonWidth, ref Utf8HintHandler preview)
    {
        var available  = Im.ContentRegion.Available;
        var comboWidth = width > 0 ? width : Im.Font.CalculateSize(ref preview, false).X + 2 * Im.Style.FramePadding.X + Im.Style.FrameHeight;
        if (FixedComboWidth is not 0 && FixedComboWidth > comboWidth)
            comboWidth = FixedComboWidth;
        var totalWidth = buttonWidth + comboWidth + LeftDistance + Math.Max(RightDistance, 0) + ComboDistance;
        var centerDistance = totalWidth >= available.X ? ComboDistance :
            RightDistance >= 0                         ? ComboDistance + available.X - totalWidth : ComboDistance;
        return (comboWidth, centerDistance);
    }

    [MethodImpl(ImSharpConfiguration.OptInl)]
    private void DrawTwoLines(Rgba32 separatorColor, float lineThickness, float linePosition, float buttonWidth)
    {
        var drawList = Im.Window.DrawList.Shape;
        var startPos = Im.Window.Position with { Y = Im.Cursor.ScreenY + linePosition };
        var fullEnd  = Im.Cursor.ScreenX + Im.ContentRegion.Available.X;
        var endPos   = startPos with { X = Im.Cursor.ScreenX + LeftDistance };
        drawList.Line(startPos, endPos, separatorColor, lineThickness);
        if (HideRightLine)
            return;

        startPos.X = endPos.X + buttonWidth;
        endPos.X   = fullEnd;
        drawList.Line(startPos, endPos, separatorColor, lineThickness);
    }

    [MethodImpl(ImSharpConfiguration.OptInl)]
    private void DrawThreeLines(Rgba32 separatorColor, float lineThickness, float linePosition, float buttonWidth, float comboWidth,
        float centerDistance)
    {
        var drawList  = Im.Window.DrawList.Shape;
        var available = Im.ContentRegion.Available;
        var startPos  = Im.Window.Position with { Y = Im.Cursor.ScreenY + linePosition };
        var endPos    = startPos with { X = Im.Cursor.ScreenX + LeftDistance };
        if (NoLabel)
        {
            endPos.X = endPos.X + buttonWidth + centerDistance;
        }
        else
        {
            drawList.Line(startPos, endPos, separatorColor, lineThickness);
            startPos.X = endPos.X + buttonWidth;
            endPos.X   = startPos.X + centerDistance;
        }

        drawList.Line(startPos, endPos, separatorColor, lineThickness);
        if (HideRightLine)
            return;
        startPos.X = endPos.X + comboWidth;
        endPos.X   = Im.Cursor.ScreenX + available.X;
        drawList.Line(startPos, endPos, separatorColor, lineThickness);
    }

    [MethodImpl(ImSharpConfiguration.OptInl)]
    private void DrawLabel(ReadOnlySpan<byte> text, ReadOnlySpan<byte> tooltip, ImGuiId id, float tooltipIconWidth, float textWidth,
        float buttonWidth,
        float centerDistance, float frameHeight, Rgba32 textColor, Rgba32 buttonBackground, Direction caret, bool noLabel)
    {
        Im.Cursor.X += LeftDistance;
        if (!noLabel)
        {
            if (Collapsible)
            {
                if (Im.Button("##b"u8, new Vector2(buttonWidth, frameHeight)))
                    Im.State.Storage.SetBool(id, caret is Direction.Right);
                var caretPosition = Im.Item.UpperLeftCorner + Im.Style.FramePadding;
                Im.Window.DrawList.Render.Arrow(caretPosition, textColor, caret, 1f);
            }
            else
            {
                ImEx.TextFramed(StringU8.Empty, new Vector2(buttonWidth, frameHeight), buttonBackground);
            }

            var pos = Im.Item.LowerRightCorner - Im.Style.FramePadding;
            pos.Y -= Im.Style.TextHeight;
            if (!tooltip.IsEmpty && !TooltipIcon.IsEmpty)
            {
                pos.X -= tooltipIconWidth;
                Im.Window.DrawList.Text(AwesomeIcon.Font, AwesomeIcon.Font.Size, pos, TooltipIconColor.CheckDefault(ImGuiColor.TextDisabled),
                    TooltipIcon.Span);
                pos.X -= Im.Style.ItemInnerSpacing.X;
            }

            pos.X -= textWidth;
            Im.Window.DrawList.Text(pos, textColor, text);
        }
        else
        {
            Im.Cursor.X += centerDistance + buttonWidth;
        }
    }
}
