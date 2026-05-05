namespace Luna;

public sealed class FileSystemSeparatorCache : IFileSystemNodeCache
{
    /// <inheritdoc/>
    public bool Dirty { get; set; } = true;

    /// <summary> The color for the separator. </summary>
    public ColorParameter Color { get; set; } = ColorParameter.Default;

    /// <summary> The path of the node. </summary>
    public StringPair FullPath { get; set; } = StringPair.Empty;

    /// <summary> The name of the node. </summary>
    public StringPair Name { get; set; } = StringPair.Empty;

    /// <inheritdoc/>
    public void Update(FileSystemCache cache, IFileSystemNode node)
    {
        FullPath = new StringPair(node.FullPath);
        Name     = new StringPair(node.Name.ToString());
        Color    = ((IFileSystemSeparator)node).Color;
    }

    /// <summary> Draw a separator line for the current node using the name as an ID. </summary>
    /// <param name="cache"> The cache drawing the node. </param>
    /// <param name="depth"> The depth of the node </param>
    /// <param name="color"> The color to draw the line in. </param>
    public static void DrawLine(FileSystemCache cache, int depth, ColorParameter color)
    {
        const float lengthGradientPixel = 20;
        var         start               = Im.Cursor.ScreenPosition;

        start.X += 1;
        start.Y += (Im.Style.TextHeight - 1) / 2;
        var end = start;
        end.X += Im.ContentRegion.Available.X;

        if (color.IsDefault)
        {
            Im.Window.DrawList.Shape.Line(start, end, cache.LineColor, 2 * Im.Style.GlobalScale);
        }
        else
        {
            if (depth > 0)
            {
                var shape      = Im.Window.DrawList.Shape;
                var pixels     = (int)(lengthGradientPixel * Im.Style.GlobalScale);
                var colorDiff  = (color.Color!.Value.ToVector() - cache.LineColor) / (pixels + 1);
                var localColor = cache.LineColor;
                for (var i = 0; i < pixels; ++i)
                {
                    var segmentEnd = start with { X = start.X + 1 };
                    localColor += colorDiff;
                    shape.Line(start, segmentEnd, color, 2 * Im.Style.GlobalScale);
                    start = segmentEnd;
                }
            }

            Im.Window.DrawList.Shape.Line(start, end, color.Color!.Value, 2 * Im.Style.GlobalScale);
        }
    }

    /// <inheritdoc/>
    public void Draw(FileSystemCache cache, IFileSystemNode node, bool startsLine)
    {
        DrawLine(cache, node.Depth, Color);
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
