namespace Luna;

/// <summary> A basic button that displays an icon instead of text. It still uses the <see cref="BaseButton.Label"/> as a pushed ID. </summary>
/// <typeparam name="TIcon"> The type of the icon to draw. </typeparam>
public abstract class BaseIconButton<TIcon> : BaseButton
    where TIcon : IIconStandIn
{
    /// <summary> The label used as ID for the button. Leave empty when no ID is required. </summary>
    public override ReadOnlySpan<byte> Label
        => StringU8.Empty;

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
            using var style = Im.Style.PushDefault();
            using var tt    = Im.Tooltip.Begin();
            DrawTooltip();
        }

        PostDraw();
        if (ret)
            OnClick();

        return ret;
    }
}

/// <summary> A basic button that displays an icon instead of text. It still uses the <see cref="BaseButton.Label"/> as a pushed ID. </summary>
/// <typeparam name="TIcon"> The type of the icon to draw. </typeparam>
/// <typeparam name="TData"> The type of the data passed to the button methods. </typeparam>
public abstract class BaseIconButton<TIcon, TData> : BaseButton<TData>
    where TIcon : IIconStandIn
{
    /// <summary> The label used as ID for the button. Leave empty when no ID is required. </summary>
    public override ReadOnlySpan<byte> Label(in TData _)
        => StringU8.Empty;

    /// <summary> Get the icon to display on the button. </summary>
    public abstract TIcon Icon
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get;
    }

    /// <summary> The method to draw the icon button to a specific size. </summary>
    /// <param name="size"> The size for the button. If the size has a non-positive component, it is automatically calculated as <see cref="Im.ImGuiStyle.FrameHeight"/>. </param>
    /// <param name="data"> The arguments to be passed to the button. </param>
    /// <returns> True if the button was clicked in this frame and <see cref="BaseButton.OnClick"/> was invoked. </returns>
    public override bool DrawButton(Vector2 size, in TData data)
    {
        using var id = Im.Id.Push(Label(data));
        PreDraw(data);
        var ret = ImEx.Icon.Button(Icon, !Enabled(data), size);
        if (HasTooltip && Im.Item.Hovered(HoveredFlags.AllowWhenDisabled))
        {
            using var style = Im.Style.PushDefault();
            using var tt    = Im.Tooltip.Begin();
            DrawTooltip(data);
        }

        PostDraw(data);
        if (ret)
            OnClick(data);

        return ret;
    }
}
