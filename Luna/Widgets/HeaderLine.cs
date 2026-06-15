namespace Luna;

public struct HeaderLine
{
    public ColorParameter LineColorCollapsed;
    public ColorParameter LineColorExpanded;
    public ColorParameter TextColorCollapsed;
    public ColorParameter TextColorExpanded;
    public ColorParameter ButtonBackground;
    public ColorParameter ButtonHovered;
    public ColorParameter ButtonActive;
    public float          FixedButtonWidth;
    public float          FixedComboWidth;
    public float          LeftDistance;
    public float          ComboDistance;
    public float          RightDistance;
    public bool           DefaultClosed;
    public bool           Collapsible;

    public (bool Expanded, ImGuiId ComboId, Rectangle ComboBoundingBox) Combo(Utf8LabelHandler label, Utf8TextHandler tooltip,
        Utf8HintHandler preview)
    {
        if (!ImEx.SplitLabel(ref label, out var visible, out var id))
            return (false, 0, default);

        using var _        = Im.Id.Push(id);
        var       expanded = !Collapsible || Im.State.Storage.GetBool(id, !DefaultClosed);
        var       shapes   = Im.Window.DrawList.Shape;

        // Colors
        var (separatorColor, textColor, icon) = expanded
            ? (LineColorExpanded.CheckDefault(ImGuiColor.Separator), TextColorExpanded.CheckDefault(ImGuiColor.Text),
                Direction.Down)
            : (LineColorCollapsed.CheckDefault(ImGuiColor.Separator), TextColorCollapsed.CheckDefault(ImGuiColor.Text),
                Direction.Right);
        var buttonBackground = ButtonBackground.CheckDefault(ImGuiColor.FrameBackground);

        // Frame
        var available       = Im.ContentRegion.Available;
        var frameHeight     = Im.Style.FrameHeight;
        var frameHeightEven = float.IsEvenInteger(frameHeight);
        var (lineThickness, linePosition) = frameHeightEven ? (2, frameHeight / 2) : (1, (frameHeight - 1) / 2);

        // Button Size
        var textWidth = Im.Font.CalculateSize(visible, false).X + 2 * Im.Style.FramePadding.X;
        var caretSize = 0f;
        if (Collapsible)
            textWidth += (caretSize = Im.Style.TextHeight) + Im.Style.ItemSpacing.X;
        if (FixedButtonWidth is not 0 && FixedButtonWidth > textWidth)
            textWidth = FixedButtonWidth;

        // Combo Size
        var comboWidth = Im.Font.CalculateSize(ref preview, false).X + 2 * Im.Style.FramePadding.X + Im.Style.FrameHeight;
        if (FixedComboWidth is not 0 && FixedComboWidth > comboWidth)
            comboWidth = FixedButtonWidth;
        var totalWidth     = textWidth + comboWidth + LeftDistance + RightDistance + ComboDistance;
        var centerDistance = totalWidth >= available.X ? ComboDistance : ComboDistance + available.X - totalWidth;

        // Draw Lines.
        var startPos = Im.Cursor.ScreenPosition;
        startPos.Y += linePosition;
        var endPos = startPos with { X = startPos.X + LeftDistance };
        shapes.Line(startPos, endPos, separatorColor, lineThickness);
        startPos.X = endPos.X + textWidth;
        endPos.X   = startPos.X + centerDistance;
        shapes.Line(startPos, endPos, separatorColor, lineThickness);
        startPos.X = endPos.X + comboWidth;
        endPos.X   = available.X;
        shapes.Line(startPos, endPos, separatorColor, lineThickness);

        ImGuiId   popupId;
        Rectangle boundingBox;
        using (var style = ImStyleBorder.Frame.Push(separatorColor, lineThickness)
                   .Push(ImGuiColor.Text, textColor)
                   .PushX(ImStyleDouble.ButtonTextAlign, 0)
                   .Push(ImGuiColor.Button,        buttonBackground)
                   .Push(ImGuiColor.ButtonHovered, ButtonHovered.CheckDefault(ImGuiColor.FrameBackgroundHovered))
                   .Push(ImGuiColor.ButtonActive,  ButtonActive.CheckDefault(ImGuiColor.FrameBackgroundActive)))
        {
            using var group = Im.Group();
            Im.Cursor.X += LeftDistance;
            if (Im.Button(visible, new Vector2(textWidth, frameHeight)) && Collapsible)
                Im.State.Storage.SetBool(id, !expanded);

            if (Collapsible)
            {
                var caretPosition = Im.Item.LowerRightCorner
                  - new Vector2(caretSize + Im.Style.FramePadding.X, frameHeight - Im.Style.FramePadding.Y);
                Im.Window.DrawList.Render.Arrow(caretPosition, textColor, icon, 1f);
            }

            Im.Line.Same(0, centerDistance);
            Im.Item.SetNextWidth(comboWidth);
            style.PopColor(3).PopStyle();
            Im.Combo.DrawPreview("##combo"u8, preview, out popupId, out boundingBox);
        }

        Im.Tooltip.OnHover(tooltip);

        return (expanded, popupId, boundingBox);
    }

    public bool Basic(Utf8LabelHandler label, Utf8TextHandler tooltip)
    {
        if (!ImEx.SplitLabel(ref label, out var visible, out var id))
            return false;

        var expanded = !Collapsible || Im.State.Storage.GetBool(id, !DefaultClosed);
        var shapes   = Im.Window.DrawList.Shape;

        // Colors
        var (separatorColor, textColor, icon) = expanded
            ? (LineColorExpanded.CheckDefault(ImGuiColor.Separator), TextColorExpanded.CheckDefault(ImGuiColor.Text),
                Direction.Down)
            : (LineColorCollapsed.CheckDefault(ImGuiColor.Separator), TextColorCollapsed.CheckDefault(ImGuiColor.Text),
                Direction.Right);
        var buttonBackground = ButtonBackground.CheckDefault(ImGuiColor.FrameBackground);

        // Frame
        var available       = Im.ContentRegion.Available;
        var frameHeight     = Im.Style.FrameHeight;
        var frameHeightEven = float.IsEvenInteger(frameHeight);
        var (lineThickness, linePosition) = frameHeightEven ? (2, frameHeight / 2) : (1, (frameHeight - 1) / 2);

        // Button Size
        var textWidth = Im.Font.CalculateSize(visible, false).X + 2 * Im.Style.FramePadding.X;
        var caretSize = 0f;
        if (Collapsible)
            textWidth += (caretSize = Im.Style.TextHeight) + Im.Style.ItemSpacing.X;
        if (FixedButtonWidth is not 0 && FixedButtonWidth > textWidth)
            textWidth = FixedButtonWidth;

        // Draw Lines.
        var startPos = Im.Cursor.ScreenPosition;
        startPos.Y += linePosition;
        var endPos = startPos with { X = startPos.X + LeftDistance };
        shapes.Line(startPos, endPos, separatorColor, lineThickness);
        startPos.X = endPos.X + textWidth;
        endPos.X   = available.X;
        shapes.Line(startPos, endPos, separatorColor, lineThickness);

        using (ImStyleBorder.Frame.Push(separatorColor, lineThickness)
                   .PushX(ImStyleDouble.ButtonTextAlign, 0)
                   .Push(ImGuiColor.Button,        buttonBackground)
                   .Push(ImGuiColor.ButtonHovered, ButtonHovered.CheckDefault(ImGuiColor.FrameBackgroundHovered))
                   .Push(ImGuiColor.ButtonActive,  ButtonActive.CheckDefault(ImGuiColor.FrameBackgroundActive))
                   .Push(ImGuiColor.Text,          textColor))
        {
            Im.Cursor.X += LeftDistance;
            if (Im.Button(visible, new Vector2(textWidth, frameHeight)) && Collapsible)
                Im.State.Storage.SetBool(id, !expanded);

            if (Collapsible)
            {
                var caretPosition = Im.Item.LowerRightCorner
                  - new Vector2(caretSize + Im.Style.FramePadding.X, frameHeight - Im.Style.FramePadding.Y);
                Im.Window.DrawList.Render.Arrow(caretPosition, textColor, icon, 1f);
            }
        }

        Im.Tooltip.OnHover(tooltip);

        return expanded;
    }
}
