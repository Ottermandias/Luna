namespace Luna;

public abstract class FilterComboColors : FilterComboBase<FilterComboColors.Item>
{
    protected short   StainId;
    protected Vector2 ButtonSize;

    protected virtual float AdditionalSpace
        => 0;

    public FilterComboColors()
        => ComputeWidth = true;


    public sealed class Item
    {
        public StringPair Name;
        public Rgba32     Color;
        public byte       Id;
        public bool       Gloss;
    }

    protected internal override float ItemHeight
        => Im.Style.FrameHeightWithSpacing;

    protected internal override bool DrawItem(in Item item, int globalIndex, bool selected)
    {
        // Push the stain color to type and if it is too bright, turn the text color black.
        var contrastColor = item.Color.ContrastColor();
        using var colors = ImGuiColor.Button.Push(item.Color, !item.Color.IsTransparent)
            .Push(ImGuiColor.Text, contrastColor);
        var ret        = Im.Button(item.Name.Utf8, ButtonSize);
        var drawList   = Im.Window.DrawList.Shape;
        var upperLeft  = Im.Item.UpperLeftCorner;
        var lowerRight = Im.Item.LowerRightCorner;
        if (selected)
        {
            drawList.Rectangle(upperLeft, lowerRight, 0xFF2020D0, 0, ImDrawFlagsRectangle.None, Im.Style.GlobalScale);
            drawList.Rectangle(upperLeft + new Vector2(Im.Style.GlobalScale), lowerRight - new Vector2(Im.Style.GlobalScale), contrastColor, 0,
                ImDrawFlagsRectangle.None, Im.Style.GlobalScale);
        }

        if (item.Gloss)
            drawList.RectangleMulticolor(upperLeft, lowerRight, 0x50FFFFFF, 0x50000000, 0x50FFFFFF, 0x50000000);
        return ret;
    }

    protected internal override bool IsSelected(Item item, int globalIndex)
        => StainId == item.Id;

    protected class ColorsCache(FilterComboColors parent) : FilterComboBaseCache<Item>(parent)
    {
        protected override void ComputeWidth()
        {
            ComboWidth = 0;
            foreach (var item in AllItems)
                ComboWidth = Math.Max(ComboWidth, Im.Font.CalculateSize(item.Name.Utf8).X);
            parent.ButtonSize =  new Vector2(ComboWidth += Im.Style.FramePadding.X * 2, parent.ItemHeight);
            ComboWidth        += parent.AdditionalSpace;
        }
    }

    protected override FilterComboBaseCache<Item> CreateCache()
        => new ColorsCache(this);
}
