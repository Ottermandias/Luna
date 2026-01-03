namespace Luna;

/// <summary> A basic footer that draws a list of buttons. </summary>
public class ButtonFooter : IFooter
{
    /// <summary> The buttons to draw. If this is empty, the footer is not drawn. </summary>
    public readonly ButtonList Buttons = new();

    /// <inheritdoc/>
    public bool Collapsed
        => Buttons.Count is 0;

    /// <inheritdoc/>
    public void Draw(Vector2 size)
    {
        var buttonWidth = size with { X = size.X / Buttons.Count };
        foreach (var button in Buttons.SkipLast(1))
        {
            button.DrawButton(buttonWidth);
            Im.Line.NoSpacing();
        }

        Buttons[^1].DrawButton(buttonWidth);
    }

    /// <inheritdoc/>
    /// <remarks> A general assumption that every button has to be at least as big as an icon button. </remarks>
    public float MinimumWidth
        => Buttons.Count * Im.Style.FrameHeight;
}
