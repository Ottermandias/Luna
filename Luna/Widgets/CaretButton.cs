namespace Luna;

/// <summary> A widget to draw a button with a collapsed/expanded caret at the front, and optional tooltip indicator. </summary>
public struct CaretButton
{
    /// <summary> The color options for the button. </summary>
    public struct Colors
    {
        /// <summary> The background color. </summary>
        public Rgba32 Background;

        /// <summary> The text color. </summary>
        public Rgba32 Text;

        /// <summary> The color of the caret. Generally same as text. </summary>
        public Rgba32 Caret;

        /// <summary> The color of the border, if any. </summary>
        public Rgba32 Border;
    }

    /// <summary> The colors to use when the state is expanded. </summary>
    public Colors Expanded;

    /// <summary> The colors to use when the state is collapsed. </summary>
    public Colors Collapsed;

    /// <summary> The background color when the button is currently hovered. </summary>
    public ColorParameter ButtonHovered;

    /// <summary> The background color when the button is currently active. </summary>
    public ColorParameter ButtonActive;

    /// <summary> The color for the icon indicating an existing tooltip, if any. </summary>
    public ColorParameter TooltipIconColor;

    /// <summary> The icon to use to indicate an existing tooltip, if any. </summary>
    public AwesomeIcon TooltipIcon;

    /// <summary> The width of the button border. </summary>
    public float BorderWidth;

    /// <summary> The alignment of the text. The caret is always left-aligned, and the tooltip button is always right-aligned. </summary>
    public float TextAlignment;

    /// <summary> Draw the button. </summary>
    /// <param name="label"> The label to display, this follows usual ImGui conventions. </param>
    /// <param name="tooltip"> The tooltip to display when hovered. If this is not empty, the tooltip icon will be shown to the right of the label if it is set. </param>
    /// <param name="size"> The size to use for the button. If this is non-positive in Y, <see cref="Im.ImGuiStyle.FrameHeight"/> is used. If it is non-positive in X, the button is sized to fit. </param>
    /// <param name="expanded"> If this is null, the expanded state is tracked by the default storage. Otherwise, the passed value is used. </param>
    /// <returns> Whether the button was clicked this frame, and whether it should be expanded or collapsed. </returns>
    public (bool Clicked, bool Expanded) Draw(Utf8LabelHandler label, Utf8TextHandler tooltip, Vector2 size, bool? expanded = null)
    {
        if (!ImEx.SplitLabel(ref label, out var visible, out var id))
            return (false, expanded ?? true);

        using var _                = Im.Id.Push(id);
        var       open             = expanded ?? Im.State.Storage.GetBool(id);
        var       tooltipSpan      = tooltip.GetSpan(out var s) ? s : StringU8.Empty;
        var       tooltipIconWidth = !tooltipSpan.IsEmpty && !TooltipIcon.IsEmpty ? TooltipIcon.CalculateSize().X : 0;
        if (size.Y <= 0)
            size.Y = Im.Style.FrameHeight;
        if (size.X <= 0)
        {
            size.X = 2 * Im.Style.FramePadding.X + Im.Style.TextHeight + Im.Font.CalculateSize(visible, false).X + Im.Style.ItemInnerSpacing.X;
            if (tooltipIconWidth > 0)
                size.X += tooltipIconWidth + Im.Style.ItemInnerSpacing.X;
        }

        var (colors, caret) = open ? (Expanded, Direction.Down) : (Collapsed, Direction.Right);
        var  startPos = Im.Cursor.ScreenPosition;
        bool clicked;
        using (ImStyleBorder.Frame.Push(colors.Border, BorderWidth)
                   .Push(ImGuiColor.Button,        colors.Background)
                   .Push(ImGuiColor.ButtonHovered, ButtonHovered)
                   .Push(ImGuiColor.ButtonActive,  ButtonActive))
        {
            clicked = Im.Button("##b"u8, size);
            if (clicked && expanded is null)
                Im.State.Storage.SetBool(id, !open);
        }

        if (!tooltipSpan.IsEmpty && Im.Item.Hovered())
            Im.Tooltip.Set(tooltipSpan);

        var drawList = Im.Window.DrawList;
        var rect     = Rectangle.FromSize(startPos, Im.Item.Size);
        startPos += Im.Style.FramePadding;
        drawList.Render.Arrow(startPos, colors.Caret, caret, Im.Style.GlobalScale);
        var textEnd = tooltipIconWidth > 0
            ? rect.Maximum.X - tooltipIconWidth - Im.Style.FramePadding.X - Im.Style.ItemInnerSpacing.X
            : rect.Maximum.X - Im.Style.FramePadding.X;
        var textRect = new Rectangle(startPos.AddX(Im.Style.TextHeight + Im.Style.ItemInnerSpacing.X),
            new Vector2(textEnd, rect.Maximum.Y - Im.Style.FramePadding.Y));
        using (ImGuiColor.Text.Push(colors.Text))
        {
            drawList.TextClipped(textRect, visible, null, new Vector2(TextAlignment, 0.5f));
        }

        if (tooltipIconWidth > 0)
        {
            startPos.X = textRect.Maximum.X + Im.Style.ItemInnerSpacing.X;
            drawList.Text(AwesomeIcon.Font, AwesomeIcon.Font.Size, startPos, TooltipIconColor.CheckDefault(ImGuiColor.TextDisabled),
                TooltipIcon.Span);
        }

        return (clicked, clicked ? !open : open);
    }
}
