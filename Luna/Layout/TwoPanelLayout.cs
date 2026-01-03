namespace Luna;

/// <summary> The scaling mode for a panel. </summary>
public enum ScalingMode : byte
{
    /// <summary> The current desired size for the left panel is given in absolute pixels and does not change when resizing the window, except to comply with boundaries. </summary>
    Absolute,

    /// <summary> The current desired size for the left panel is given as a percentage of the total available content region, except for complying with boundaries. </summary>
    Percentage,
}

/// <summary> The desired width of the left panel. </summary>
/// <param name="Width">
///   The width of the left panel,
///   <list type="bullet">
///     <item>in unscaled pixels if <paramref name="Mode"/> is <see cref="ScalingMode.Absolute"/> (this will get multiplied by <see cref="Im.ImGuiStyle.GlobalScale"/>),</item>
///     <item>or as a percentage of the <see cref="Im.ContentRegion.Available"/>.X if it is <see cref="ScalingMode.Percentage"/>.</item>
///   </list>
/// </param>
/// <param name="Mode"> The mode that describes how <paramref name="Width"/> is to be interpreted. </param>
public readonly record struct TwoPanelWidth(float Width, ScalingMode Mode)
{
    /// <summary> Compute the actual width for the current panel. </summary>
    public Vector2 ComputeWidth()
    {
        var available = Im.ContentRegion.Available;
        if (Mode is ScalingMode.Percentage)
        {
            if (float.IsNaN(Width))
                return available with { X = 0.5f * available.X };

            return available with { X = Width * available.X };
        }

        if (float.IsNaN(Width))
            return available with { X = 0.5f * available.X };

        return available with { X = Width * Im.Style.GlobalScale };
    }

    /// <summary> Indeterminate value for an absolute selector that will resolve to half of the available content region but return absolute values. </summary>
    public static readonly TwoPanelWidth IndeterminateAbsolute = new(float.NaN, ScalingMode.Absolute);

    /// <summary> Indeterminate value for a percentage selector that will resolve to half of the available content region. </summary>
    public static readonly TwoPanelWidth IndeterminateRelative = new(0.5f, ScalingMode.Percentage);
}

/// <summary> A basic layout that splits the screen into a left and right panel that can be resized by dragging the splitter. </summary>
public class TwoPanelLayout : IUiService
{
    /// <summary> Whether the panel splitter should be resizable or not. </summary>
    public bool Resizable { get; init; } = true;

    /// <summary> An optional left header of <see cref="Im.ImGuiStyle.FrameHeight"/>, which is drawn to a specific width after the panel may have been resized. </summary>
    public IHeader LeftHeader { get; init; } = EmptyHeaderFooter.Instance;

    /// <summary> An optional right header of <see cref="Im.ImGuiStyle.FrameHeight"/>, which is drawn to the remainder of the available content region. </summary>
    public IHeader RightHeader { get; init; } = EmptyHeaderFooter.Instance;

    /// <summary> An optional left footer of <see cref="Im.ImGuiStyle.FrameHeight"/>, which is drawn to a specific width after the panel may have been resized. </summary>
    public IFooter LeftFooter { get; init; } = EmptyHeaderFooter.Instance;

    /// <summary> An optional right footer of <see cref="Im.ImGuiStyle.FrameHeight"/>, which is drawn to the remainder of the available content region. </summary>
    public IFooter RightFooter { get; init; } = EmptyHeaderFooter.Instance;

    /// <summary> The left panel drawn within a child managed by this layout using the panel's <see cref="IPanel.Id"/>. </summary>
    public IPanel LeftPanel { get; init; } = new BasePanel("l"u8);

    /// <summary> The right panel drawn within a child managed by this layout using the panel's <see cref="IPanel.Id"/>. </summary>
    public IPanel RightPanel { get; init; } = new BasePanel("r"u8);

    /// <summary> The label that informs the ID of the entire panel layout. </summary>
    public virtual ReadOnlySpan<byte> Label
        => "##Layout"u8;

    /// <summary> The ID of the main layout during the current Draw call. Undefined outside Draw. </summary>
    protected ImGuiId LayoutId { get; private set; }

    /// <summary> The mode stored for the SetWidth call. </summary>
    private ScalingMode _mode = ScalingMode.Absolute;

    /// <summary> The available content region width before drawing anything. </summary>
    private float _contentRegionStart;

    /// <summary> Invoked whenever the panels are resized with the new size of the left panel. </summary>
    /// <param name="width"> The new width of the left panel. </param>
    /// <param name="mode"> The mode this panel was resized in. </param>
    /// <remarks> This is not invoked when the panel is resized due to the available content region, <see cref="MinimumWidth"/>, or <see cref="MaximumWidth"/> changing. </remarks>
    protected virtual void SetWidth(float width, ScalingMode mode)
    { }

    private void SetWidth(Vector2 width)
    {
        if (_mode is ScalingMode.Absolute)
            SetWidth(width.X, _mode);
        else
            SetWidth(width.X / _contentRegionStart, _mode);
    }

    /// <summary> Get the minimum width for the left panel. </summary>
    protected virtual float MinimumWidth
        => 0;

    /// <summary> Get the maximum width for the left panel. </summary>
    protected virtual float MaximumWidth
        => float.MaxValue;

    /// <summary> Draw the full layout. </summary>
    public void Draw(in TwoPanelWidth width)
    {
        using var id = Im.Id.Push(Label);
        LayoutId = Im.Id.Current;
        DrawPopups();

        _mode               = width.Mode;
        _contentRegionStart = Im.ContentRegion.Available.X;
        DrawLeftGroup(width);
        Im.Line.SameInner();
        DrawRightGroup();
    }

    /// <summary> Draw the left header, panel and footer, while also handling the resizing of the panel. </summary>
    protected virtual void DrawLeftGroup(in TwoPanelWidth width)
    {
        using var groupDisposable = Im.Group();
        using var style = Im.Style.PushY(ImStyleDouble.ItemSpacing, 0)
            .Push(ImStyleSingle.ChildRounding, 0)
            .Push(ImStyleDouble.WindowPadding, Vector2.Zero);

        // We need to draw the header after the child so we can draw it scaled with the child.
        var leftHeaderPos = Im.Cursor.Position;
        if (!LeftHeader.Collapsed)
            Im.Cursor.Y += Im.Style.FrameHeight;

        // If we have a footer, we have to reduce the height of the child.
        var size = width.ComputeWidth();
        if (!LeftFooter.Collapsed)
            size.Y -= Im.Style.FrameHeight;


        using (var child = Resizable
                   ? ImEx.ResizableChild(LeftPanel.Id, size, out size, SetWidth, size with { X = MinimumWidth }, size with { X = MaximumWidth })
                   : Im.Child.Begin(LeftPanel.Id, size, true))
        {
            // Draw the child without style variables pushed and restore them afterward.
            if (child)
            {
                style.Pop(3);
                LeftPanel.Draw();
                style.PushY(ImStyleDouble.ItemSpacing, 0)
                    .Push(ImStyleSingle.ChildRounding, 0)
                    .Push(ImStyleDouble.WindowPadding, Vector2.Zero);
            }
        }

        style.Pop(2)
            .Push(ImStyleSingle.FrameRounding, 0);

        // Draw the footer
        var headerFooterSize = size with { Y = Im.Style.FrameHeight };
        if (!LeftFooter.Collapsed)
            LeftFooter.Draw(headerFooterSize);

        // Draw the header by moving back the cursor.
        if (!LeftHeader.Collapsed)
        {
            Im.Cursor.Position = leftHeaderPos;
            LeftHeader.Draw(headerFooterSize);
        }
    }

    /// <summary> Draw the right header, panel and footer using the remaining available width. </summary>
    protected virtual void DrawRightGroup()
    {
        using var groupDisposable = Im.Group();
        using var style = Im.Style.PushY(ImStyleDouble.ItemSpacing, 0)
            .Push(ImStyleDouble.WindowPadding, Vector2.Zero)
            .Push(ImStyleSingle.FrameRounding, 0);

        // Draw the header, which is not restricted to frame height.
        if (!RightHeader.Collapsed)
            RightHeader.Draw(Im.ContentRegion.Available);

        style.Pop(2).Push(ImStyleSingle.ChildRounding, 0);

        // Reduce the height of the child if we have a footer.
        var childSize = Im.ContentRegion.Available;
        if (!RightFooter.Collapsed)
            childSize.Y -= Im.Style.FrameHeight;

        using (var child = Im.Child.Begin(RightPanel.Id, childSize, true))
        {
            if (child)
            {
                // Draw the child without style variables pushed and restore them afterward.
                style.Pop(2);
                RightPanel.Draw();
                style.PushY(ImStyleDouble.ItemSpacing, 0)
                    .Push(ImStyleSingle.ChildRounding, 0);
            }
        }

        // Draw the footer
        if (!RightFooter.Collapsed)
        {
            style.Pop()
                .Push(ImStyleDouble.WindowPadding, Vector2.Zero)
                .Push(ImStyleSingle.FrameRounding, 0);
            RightFooter.Draw(Im.ContentRegion.Available);
        }
    }

    /// <summary> Draw any optional popups of the layout regardless of what is visible or not. </summary>
    protected virtual void DrawPopups()
    { }
}
