namespace Luna;

/// <summary> A basic button that displays an icon instead of text. It still uses the <see cref="BaseButton.Label"/> as a pushed ID. </summary>
/// <typeparam name="TIcon"> The type of the icon to draw. </typeparam>
public abstract class BaseIconButton<TIcon> : BaseButton
    where TIcon : IIconStandIn
{
    /// <summary> Get the icon to display on the button. </summary>
    public abstract TIcon Icon
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get;
    }

    /// <summary> The method to draw the icon button to a specific size. </summary>
    /// <param name="size"> The size for the button. If the size has a non-positive component, it is automatically calculated as <see cref="Im.ImGuiStyle.FrameHeight"/>. </param>
    /// <returns> True if the button was clicked in this frame and <see cref="BaseButton.OnClick"/> was invoked. </returns>
    public override bool DrawButton(Vector2 size)
    {
        using var id = Im.Id.Push(Label);
        PreDraw();
        var ret = ImEx.Icon.Button(Icon, !Enabled, size);
        if (HasTooltip && Im.Item.Hovered(HoveredFlags.AllowWhenDisabled))
        {
            using var tt = Im.Tooltip.Begin();
            DrawTooltip();
        }

        PostDraw();
        if (ret)
            OnClick();

        return ret;
    }
}
