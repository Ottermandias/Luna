namespace Luna;

public struct HeaderLine
{
    public delegate bool DrawCombo<TCacheItem>(Utf8HintHandler preview, float comboWidth, out TCacheItem? selection);

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
    public bool           ComboDisabled;

    public (bool Expanded, bool Changed, TCacheItem? Selection) Combo<TCacheItem>(DrawCombo<TCacheItem> drawer, Utf8LabelHandler label,
        Utf8TextHandler tooltip, Utf8HintHandler preview)
    {
        var (expanded, changed, _, _, selection) = ComboInternal(drawer, 0, ref label, ref tooltip, ref preview);
        return (expanded, changed, selection);
    }

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

    public (bool Expanded, ImGuiId ComboId, Rectangle ComboBoundingBox) Combo(Utf8LabelHandler label, Utf8TextHandler tooltip,
        Utf8HintHandler preview)
    {
        var (expanded, _, comboId, comboBoundingBox, _) = ComboInternal<object>(null, 0, ref label, ref tooltip, ref preview);
        return (expanded, comboId, comboBoundingBox);
    }

    [MethodImpl(ImSharpConfiguration.OptInl)]
    private (bool Expanded, bool Changed, ImGuiId ComboId, Rectangle ComboBoundingBox, TCacheItem? Selection) ComboInternal<TCacheItem>(
        DrawCombo<TCacheItem>? drawer, float width, ref Utf8LabelHandler label, ref Utf8TextHandler tooltip,
        ref Utf8HintHandler preview)
    {
        if (!ImEx.SplitLabel(ref label, out var visible, out var id))
            return (false, false, 0, default, default);

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
        if (Collapsible)
            textWidth += Im.Style.TextHeight + Im.Style.ItemInnerSpacing.X;
        if (FixedButtonWidth is not 0 && FixedButtonWidth > textWidth)
            textWidth = FixedButtonWidth;

        // Combo Size
        var comboWidth = width > 0 ? width : Im.Font.CalculateSize(ref preview, false).X + 2 * Im.Style.FramePadding.X + Im.Style.FrameHeight;
        if (FixedComboWidth is not 0 && FixedComboWidth > comboWidth)
            comboWidth = FixedComboWidth;
        var totalWidth     = textWidth + comboWidth + LeftDistance + Math.Max(RightDistance, 0) + ComboDistance;
        var centerDistance = totalWidth >= available.X ? ComboDistance : RightDistance >= 0 ? ComboDistance + available.X - totalWidth : ComboDistance;

        // Draw Lines.
        var startPos = Im.Window.Position with { Y = Im.Cursor.ScreenY + linePosition };
        var endPos   = startPos with { X = Im.Cursor.ScreenX + LeftDistance };
        shapes.Line(startPos, endPos, separatorColor, lineThickness);
        startPos.X = endPos.X + textWidth;
        endPos.X   = startPos.X + centerDistance;
        shapes.Line(startPos, endPos, separatorColor, lineThickness);
        startPos.X = endPos.X + comboWidth;
        endPos.X   = Im.Cursor.ScreenX + available.X;
        shapes.Line(startPos, endPos, separatorColor, lineThickness);

        ImGuiId     popupId;
        Rectangle   boundingBox;
        bool        change;
        TCacheItem? selection;
        using (var style = ImStyleBorder.Frame.Push(separatorColor, lineThickness)
                   .Push(ImGuiColor.Text, textColor)
                   .PushX(ImStyleDouble.ButtonTextAlign, 1)
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
                var caretPosition = Im.Item.UpperLeftCorner + Im.Style.FramePadding;
                Im.Window.DrawList.Render.Arrow(caretPosition, textColor, icon, 1f);
            }

            Im.Line.Same(0, centerDistance);

            style.PopColor(3).PopStyle();
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
        var frameHeight     = Im.Style.FrameHeight;
        var frameHeightEven = float.IsEvenInteger(frameHeight);
        var (lineThickness, linePosition) = frameHeightEven ? (2, frameHeight / 2) : (1, (frameHeight - 1) / 2);

        // Button Size
        var textWidth = Im.Font.CalculateSize(visible, false).X + 2 * Im.Style.FramePadding.X;
        if (Collapsible)
            textWidth += Im.Style.TextHeight + Im.Style.ItemInnerSpacing.X;
        if (FixedButtonWidth is not 0 && FixedButtonWidth > textWidth)
            textWidth = FixedButtonWidth;

        // Draw Lines.
        var startPos = Im.Window.Position with { Y = Im.Cursor.ScreenY + linePosition };
        var fullEnd  = Im.Cursor.ScreenX + Im.ContentRegion.Available.X;
        var endPos   = startPos with { X = Im.Cursor.ScreenX + LeftDistance };
        shapes.Line(startPos, endPos, separatorColor, lineThickness);
        startPos.X = endPos.X + textWidth;
        endPos.X   = fullEnd;
        shapes.Line(startPos, endPos, separatorColor, lineThickness);

        using (ImStyleBorder.Frame.Push(separatorColor, lineThickness)
                   .PushX(ImStyleDouble.ButtonTextAlign, 1)
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
                var caretPosition = Im.Item.UpperLeftCorner + Im.Style.FramePadding;
                Im.Window.DrawList.Render.Arrow(caretPosition, textColor, icon, 1f);
            }
        }

        Im.Tooltip.OnHover(tooltip);

        return expanded;
    }
}
