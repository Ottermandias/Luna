namespace Luna;

/// <summary> A combo to display named colors. </summary>
public abstract class FilterComboColors : FilterComboBase<FilterComboColors.Item>
{
    /// <summary> A tracker variable for styles and colors pushed across function boundaries. </summary>
    protected readonly Im.ColorStyleDisposable Style = new();

    /// <summary> The size to draw each color in the combo in. </summary>
    protected Vector2 ButtonSize;

    /// <summary> No color is selected. </summary>
    public static readonly Item None = new(new StringU8("None"), Rgba32.Transparent, 0, false);

    /// <summary> The current selection of the combo. </summary>
    public Item CurrentSelection
    {
        get;
        set
        {
            if (field.Id == value.Id)
                return;

            field = value;
            SelectionChanged?.Invoke(field);
        }
    } = None;

    /// <summary> Invoked whenever the selection changes. </summary>
    public event Action<Item>? SelectionChanged;

    /// <summary> Additional space for the width of the combo. </summary>
    protected virtual float AdditionalSpace
        => 0;

    /// <summary> Set the base values for the color filter combo. </summary>
    public FilterComboColors()
    {
        ComputeWidth = true;
        Filter       = new NameFilter();
        Flags        = ComboFlags.NoArrowButton;
    }

    /// <inheritdoc/>
    protected internal override float ItemHeight
        => Im.Style.FrameHeight;

    /// <summary> Draw the combo updating its own current selection. </summary>
    /// <param name="label"> The label to use. </param>
    /// <returns> True if the selection was changed this frame. </returns>
    public bool Draw(Utf8LabelHandler label)
    {
        // Push the preview color.
        using var color = ImGuiColor.FrameBackground.Push(CurrentSelection.Color, !CurrentSelection.Color.IsTransparent);

        // Skip the named preview if it does not fit.
        var name = Im.Font.CalculateSize(CurrentSelection.Name).X <= Im.Style.FrameHeight ? CurrentSelection.Name : StringU8.Empty;
        var ret  = base.Draw(label, name, StringU8.Empty, Im.Style.FrameHeight, out var newStain);
        if (ret)
            CurrentSelection = newStain;

        return ret;
    }

    /// <inheritdoc/>
    protected internal override bool IsSelected(Item item, int globalIndex)
        => CurrentSelection.Id == item.Id;


    /// <inheritdoc/>
    protected internal override bool DrawItem(in Item item, int globalIndex, bool selected)
    {
        // Push the stain color to type and if it is too bright, turn the text color black.
        var contrastColor = item.Color.ContrastColor();
        Style.Push(ImGuiColor.Button, item.Color, !item.Color.IsTransparent)
            .Push(ImGuiColor.Text, contrastColor);
        Im.Cursor.X = 0;
        var ret = Im.Button(item.Name, Im.Scroll.MaximumY > 0 ? ButtonSize with { X = ButtonSize.X - Im.Style.ScrollbarSize } : ButtonSize);
        Style.PopColor(2);

        // Draw selection.
        var drawList   = Im.Window.DrawList.Shape;
        var upperLeft  = Im.Item.UpperLeftCorner;
        var lowerRight = Im.Item.LowerRightCorner;
        if (selected)
        {
            drawList.Rectangle(upperLeft, lowerRight, 0xFF2020D0, 0, ImDrawFlagsRectangle.None, Im.Style.GlobalScale);
            drawList.Rectangle(upperLeft + new Vector2(Im.Style.GlobalScale), lowerRight - new Vector2(Im.Style.GlobalScale), contrastColor, 0,
                ImDrawFlagsRectangle.None, Im.Style.GlobalScale);
        }

        // Draw gloss.
        if (item.Gloss)
            drawList.RectangleMulticolor(upperLeft, lowerRight, 0x50FFFFFF, 0x50000000, 0x50FFFFFF, 0x50000000);
        return ret;
    }

    /// <inheritdoc/>
    protected override void PostDrawCombo(float width)
    {
        if (!CurrentSelection.Gloss)
            return;

        var min = Im.Item.UpperLeftCorner;
        // This removes frame rounding but there is currently no easy way to obtain a multicolored rectangle with rounding.
        Im.Window.DrawList.Shape.RectangleMulticolor(min, Im.Item.LowerRightCorner with { X = min.X + width }, 0x50FFFFFF,
            0x50000000, 0x50FFFFFF, 0x50000000);
    }


    protected override bool DrawComboPopup(out Item ret)
    {
        Style.Push(ImStyleDouble.WindowPadding, Vector2.Zero);
        var r = base.DrawComboPopup(out ret);
        Style.PopStyle();
        return r;
    }

    /// <inheritdoc/>
    protected internal override void PreDrawList()
    {
        Style.Push(ImStyleSingle.FrameRounding, 0)
            .Push(ImStyleDouble.ItemSpacing, Vector2.Zero);
    }

    /// <inheritdoc/>
    protected override bool DrawFilter(float width, FilterComboBaseCache<Item> cache)
    {
        using var color = ImGuiColor.FrameBackground.PushDefault();
        return base.DrawFilter(width, cache);
    }

    /// <inheritdoc/>
    protected internal override void PostDrawList()
        => Style.Dispose();

    /// <summary> A color item. </summary>
    /// <param name="Name"> The name to display and filter for. </param>
    /// <param name="Color"> The color value. </param>
    /// <param name="Id"> An additional ID for the color. </param>
    /// <param name="Gloss"> Whether the color is glossy, in which case a shine effect will be applied. </param>
    public readonly record struct Item(StringU8 Name, Rgba32 Color, byte Id, bool Gloss);

    /// <summary> Specialized cache to compute the required width for the popup. </summary>
    protected class ColorsCache(FilterComboColors parent) : FilterComboBaseCache<Item>(parent)
    {
        protected override void ComputeWidth()
        {
            ComboWidth = 0;
            foreach (var item in AllItems)
                ComboWidth = Math.Max(ComboWidth, Im.Font.CalculateSize(item.Name).X);
            parent.ButtonSize =  new Vector2(ComboWidth += Im.Style.FramePadding.X * 2 + Im.Style.ScrollbarSize, parent.ItemHeight);
            ComboWidth        += parent.AdditionalSpace;
        }
    }

    /// <summary> The filter to use. </summary>
    protected sealed class NameFilter : Utf8FilterBase<Item>
    {
        /// <inheritdoc/>
        protected override ReadOnlySpan<byte> ToFilterString(in Item item, int globalIndex)
            => item.Name;
    }

    /// <inheritdoc/>
    protected override FilterComboBaseCache<Item> CreateCache()
        => new ColorsCache(this);
}
