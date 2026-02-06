namespace Luna;

/// <summary> A header with equally sized buttons split to the left and right and a text in the middle. </summary>
public class SplitButtonHeader : IHeader
{
    /// <summary> The buttons on the left side. Higher priority means further left. </summary>
    public readonly ButtonList LeftButtons = new();

    /// <summary> The buttons on the right side. Higher priority means further left. </summary>
    public readonly ButtonList RightButtons = new();

    /// <summary> An optional color for the text in the center. </summary>
    public virtual ColorParameter TextColor
        => ColorParameter.Default;

    /// <summary> The text in the center. </summary>
    public virtual ReadOnlySpan<byte> Text
        => StringU8.Empty;

    public virtual void Draw(Vector2 size)
    {
        var width = Im.Style.FrameHeightWithSpacing;
        using var style = new Im.ColorStyleDisposable()
            .Push(ImStyleDouble.ItemSpacing,          Vector2.Zero)
            .Push(ImStyleSingle.FrameRounding,        0)
            .Push(ImStyleSingle.FrameBorderThickness, Im.Style.GlobalScale);

        // Draw the left buttons and keep track of their total size to align the text.
        var buttonSize     = size with { X = width };
        var leftButtonSize = 0f;
        foreach (var button in LeftButtons.Where(b => b.IsVisible))
        {
            button.DrawButton(buttonSize);
            Im.Line.Same();
            leftButtonSize += width;
        }

        // Count the visible buttons on the right to calculate the required size.
        var rightButtonSize = RightButtons.Count(b => b.IsVisible) * width;
        var midSize         = Im.ContentRegion.Available.X - rightButtonSize - Im.Style.GlobalScale;

        // Align the text so that it is centered on the header, regardless of the buttons.
        style.PopStyle()
            .Push(ImStyleDouble.ButtonTextAlign, new Vector2(0.5f + (rightButtonSize - leftButtonSize) / midSize / 1.5f, 0.5f));
        var text  = Text;
        var color = TextColor;
        if (color.IsDefault)
            style.PushDefault(ImGuiColor.Text);
        else
            style.Push(ImGuiColor.Text, color);
        ImEx.TextFramed(text, size with { X = midSize }, ColorParameter.Default, ColorParameter.Default, Rgba32.Transparent);
        style.PopColor()
            .PopStyle()
            .Push(ImStyleSingle.FrameBorderThickness, Im.Style.GlobalScale);

        // Draw the right buttons.
        foreach (var button in RightButtons.Where(b => b.IsVisible))
        {
            Im.Line.Same();
            button.DrawButton(buttonSize);
        }
    }

    public bool Collapsed
        => false;
}
