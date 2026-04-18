namespace Luna;

public sealed class FileSystemSeparatorCache : IFileSystemNodeCache
{
    /// <inheritdoc/>
    public bool Dirty { get; set; } = true;

    public ColorParameter Color    { get; set; } = ColorParameter.Default;
    public StringPair     FullPath { get; set; } = StringPair.Empty;
    public StringPair     Name     { get; set; } = StringPair.Empty;

    public void Update(FileSystemCache cache, IFileSystemNode node)
    {
        FullPath = new StringPair(node.FullPath);
        Name     = new StringPair(node.Name.ToString());
        Color    = ((IFileSystemSeparator)node).Color;
    }

    public void Draw(FileSystemCache cache, IFileSystemNode node, bool startsLine)
    {
        const float lengthGradientPixel = 20;
        var         start               = Im.Cursor.ScreenPosition;

        start.X += 1;
        start.Y += (Im.Style.TextHeight - 1) / 2;
        var end = start;
        end.X += Im.ContentRegion.Available.X;

        if (Color.IsDefault)
        {
            Im.Window.DrawList.Shape.Line(start, end, cache.LineColor, 2 * Im.Style.GlobalScale);
        }
        else
        {
            if (node.Depth > 0)
            {
                var shape     = Im.Window.DrawList.Shape;
                var pixels    = (int)(lengthGradientPixel * Im.Style.GlobalScale);
                var colorDiff = (Color.Color!.Value.ToVector() - cache.LineColor) / (pixels + 1);
                var color     = cache.LineColor;
                for (var i = 0; i < pixels; ++i)
                {
                    var segmentEnd = start with { X = start.X + 1 };
                    color += colorDiff;
                    shape.Line(start, segmentEnd, color, 2 * Im.Style.GlobalScale);
                    start = segmentEnd;
                }
            }

            Im.Window.DrawList.Shape.Line(start, end, Color.Color!.Value, 2 * Im.Style.GlobalScale);
        }

        Im.InvisibleButton(Name.Utf8, Im.ContentRegion.Available with { Y = Im.Style.TextHeight });
        if (cache.Parent.SeparatorContext.Count is 0)
            return;

        using var context = Im.Popup.BeginContextItem();
        if (!context)
            return;

        foreach (var button in cache.Parent.SeparatorContext)
            button.DrawMenuItem((IFileSystemSeparator)node);
    }
}
