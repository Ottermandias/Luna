namespace Luna;

/// <summary> A basic class for handling button behavior. </summary>
public abstract class BaseButton
{
    /// <summary> The label to display on the button or menu item, or the ID if no visible label is needed. </summary>
    public abstract ReadOnlySpan<byte> Label
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get;
    }

    /// <summary> Draw the interior part of a tooltip. This is invoked when the item is hovered, regardless of whether it is disabled, and is already inside a tooltip context. </summary>
    /// <remarks> If you use this, ensure that <see cref="HasTooltip"/> returns true. </remarks>
    public virtual void DrawTooltip()
    { }

    /// <summary> Whether the button is enabled. If false, the button is drawn in a disabled state and cannot be clicked. </summary>
    public virtual bool Enabled
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get => true;
    }

    /// <summary> Whether the button should draw a tooltip. Be sure to implement <see cref="DrawTooltip"/> if this returns true. </summary>
    public virtual bool HasTooltip
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get => false;
    }

    /// <summary> The action invoked when the button is clicked. </summary>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public virtual void OnClick()
    { }

    /// <summary> Invoked before the button is drawn but on the same ID stack level as the button. </summary>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public virtual void PreDraw()
    { }

    /// <summary> Invoked after the button and its tooltip (if any) have been drawn. </summary>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public virtual void PostDraw()
    { }

    /// <summary> The method to draw the button to a specific size. </summary>
    /// <param name="size"> The size for the button. If the size has a non-positive component, it is automatically calculated from the label text and style settings. </param>
    /// <returns> True if the button was clicked in this frame and <see cref="OnClick"/> was invoked. </returns>
    public virtual bool DrawButton(Vector2 size)
    {
        PreDraw();
        var ret = ImEx.Button(Label, disabled: !Enabled, size: size);
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

    /// <summary> The method to draw this button as a menu item. </summary>
    /// <returns> True if the menu item was clicked in this frame and <see cref="OnClick"/> was invoked. </returns>
    public virtual bool DrawMenuItem()
    {
        PreDraw();
        var ret = Im.Menu.Item(Label, enabled: Enabled);
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

/// <summary> A basic class for handling button behavior with arguments. </summary>
public abstract class BaseButton<T>
{
    /// <summary> The label to display on the button or menu item, or the ID if no visible label is needed. </summary>
    public abstract ReadOnlySpan<byte> Label(in T data);

    /// <summary> Draw the interior part of a tooltip. This is invoked when the item is hovered, regardless of whether it is disabled, and is already inside a tooltip context. </summary>
    /// <remarks> If you use this, ensure that <see cref="HasTooltip"/> returns true. </remarks>
    public virtual void DrawTooltip(in T data)
    { }

    /// <summary> Whether the button is enabled. If false, the button is drawn in a disabled state and cannot be clicked. </summary>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public virtual bool Enabled(in T data)
        => true;

    /// <summary> Whether the button should draw a tooltip. Be sure to implement <see cref="DrawTooltip"/> if this returns true. </summary>
    public virtual bool HasTooltip
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get => false;
    }

    /// <summary> The action invoked when the button is clicked. </summary>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public virtual void OnClick(in T data)
    { }

    /// <summary> Invoked before the button is drawn but on the same ID stack level as the button. </summary>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public virtual void PreDraw(in T data)
    { }

    /// <summary> Invoked after the button and its tooltip (if any) have been drawn. </summary>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public virtual void PostDraw(in T data)
    { }

    /// <summary> The method to draw the button to a specific size. </summary>
    /// <param name="size"> The size for the button. If the size has a non-positive component, it is automatically calculated from the label text and style settings. </param>
    /// <param name="data"> The arguments to be passed to the button. </param>
    /// <returns> True if the button was clicked in this frame and <see cref="OnClick"/> was invoked. </returns>
    public virtual bool DrawButton(Vector2 size, in T data)
    {
        PreDraw(data);
        var ret = ImEx.Button(Label(data), disabled: !Enabled(data), size: size);
        if (HasTooltip && Im.Item.Hovered(HoveredFlags.AllowWhenDisabled))
        {
            using var tt = Im.Tooltip.Begin();
            DrawTooltip(data);
        }

        PostDraw(data);
        if (ret)
            OnClick(data);

        return ret;
    }

    /// <summary> The method to draw this button as a menu item. </summary>
    /// <param name="data"> The arguments to be passed to the button. </param>
    /// <returns> True if the menu item was clicked in this frame and <see cref="OnClick"/> was invoked. </returns>
    public virtual bool DrawMenuItem(in T data)
    {
        PreDraw(data);
        var ret = Im.Menu.Item(Label(data), enabled: Enabled(data));
        if (HasTooltip && Im.Item.Hovered(HoveredFlags.AllowWhenDisabled))
        {
            using var tt = Im.Tooltip.Begin();
            DrawTooltip(data);
        }

        PostDraw(data);
        if (ret)
            OnClick(data);

        return ret;
    }
}
