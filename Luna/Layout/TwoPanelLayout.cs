namespace Luna;

public class TwoPanelLayout : IUiService
{
    public bool    Resizable   { get; init; } = true;
    public IHeader LeftHeader  { get; init; } = EmptyHeader.Instance;
    public IHeader RightHeader { get; init; } = EmptyHeader.Instance;
    public IHeader LeftFooter  { get; init; } = EmptyHeader.Instance;
    public IHeader RightFooter { get; init; } = EmptyHeader.Instance;
    public IPanel  LeftPanel   { get; init; } = new BasePanel("l"u8);
    public IPanel  RightPanel  { get; init; } = new BasePanel("r"u8);

    private float? _currentWidth;

    protected virtual void SetSize(Vector2 newSize)
    {
        _currentWidth = newSize.X;
    }

    protected virtual Vector2 CurrentSize
    {
        get
        {
            if (_currentWidth.HasValue)
                return Im.ContentRegion.Available with { X = _currentWidth.Value };

            var ret = Im.ContentRegion.Available;
            ret.X         /= 2;
            _currentWidth =  ret.X;
            return ret;
        }
    }

    public void Draw()
    {
        DrawPopups();
        DrawLeftGroup();
        Im.Line.SameInner();
        DrawRightGroup();
    }

    protected virtual void DrawLeftGroup()
    {
        using var groupDisposable = Im.Group();
        using var style = Im.Style.PushY(ImStyleDouble.ItemSpacing, 0)
            .Push(ImStyleSingle.ChildRounding, 0)
            .Push(ImStyleDouble.WindowPadding, Vector2.Zero);

        using var test          = Im.Style.Push(ImStyleDouble.ButtonTextAlign, new Vector2());
        var       leftHeaderPos = Im.Cursor.Position;
        if (!LeftHeader.Collapsed)
            Im.Cursor.Y += Im.Style.FrameHeight;
        
        var size = CurrentSize;
        if (!LeftFooter.Collapsed)
            size.Y -= Im.Style.FrameHeight;
        
        using (var child = Resizable
                   ? ImEx.ResizableChild(LeftPanel.Id, size, out size, SetSize, size with { X = 0 }, size with { X = float.MaxValue })
                   : Im.Child.Begin(LeftPanel.Id, size, true))
        {
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
        
        var headerFooterSize = size with { Y = Im.Style.FrameHeight };
        if (!LeftFooter.Collapsed)
            LeftFooter.Draw(headerFooterSize);
        
        if (!LeftHeader.Collapsed)
        {
            Im.Cursor.Position = leftHeaderPos;
            LeftHeader.Draw(headerFooterSize);
        }
    }

    protected virtual void DrawRightGroup()
    {
        using var groupDisposable = Im.Group();
        using var style = Im.Style.PushY(ImStyleDouble.ItemSpacing, 0)
            .Push(ImStyleDouble.WindowPadding, Vector2.Zero)
            .Push(ImStyleSingle.FrameRounding, 0);
        var startCursor = Im.Cursor.Y;
        if (!RightHeader.Collapsed)
        {
            RightHeader.Draw(Im.ContentRegion.Available with { Y = Im.Style.FrameHeight });
            Im.Cursor.Y = startCursor + Im.Style.FrameHeight;
        }

        style.Pop().Push(ImStyleSingle.ChildRounding, 0);
        var childSize = Im.ContentRegion.Available;
        if (!RightFooter.Collapsed)
            childSize.Y -= Im.Style.FrameHeight;
        using (var child = Im.Child.Begin(RightPanel.Id, childSize, true))
        {
            if (child)
            {
                style.Pop(3);
                RightPanel.Draw();
                style.PushY(ImStyleDouble.ItemSpacing, 0)
                    .Push(ImStyleDouble.WindowPadding, Vector2.Zero)
                    .Push(ImStyleSingle.ChildRounding, 0);
            }
        }

        if (!RightFooter.Collapsed)
        {
            style.Pop()
                .Push(ImStyleSingle.FrameRounding, 0);
            RightFooter.Draw(Im.ContentRegion.Available);
        }
    }

    protected virtual void DrawPopups()
    { }
}
