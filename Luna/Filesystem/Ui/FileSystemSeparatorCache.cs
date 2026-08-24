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
    /// <param name="lineColor"> The actual color of the tree line connecting to this. </param>
    public static void DrawLine(FileSystemCache cache, int depth, ColorParameter color, ColorParameter lineColor)
    {
        const float lengthGradientPixel = 20;
        var         start               = Im.Cursor.ScreenPosition;

        start.X += 1;
        start.Y += (Im.Style.TextHeight - 1) / 2;
        var end = start;
        end.X += Im.ContentRegion.Available.X;

        if (color.IsDefault)
        {
            var parentColor = lineColor.CheckDefault(cache.GetLineColor(depth));
            Im.Window.DrawList.Shape.Line(start, end, parentColor, 2 * Im.Style.GlobalScale);
        }
        else
        {
            if (depth > 0)
            {
                var parentColor = lineColor.CheckDefault(cache.GetLineColor(depth));
                var shape       = Im.Window.DrawList.Shape;
                var pixels      = (int)(lengthGradientPixel * Im.Style.GlobalScale);
                var localColor  = parentColor.ToVector();
                var colorDiff   = (color.Color!.Value.ToVector() - localColor) / (pixels + 1);
                for (var i = 0; i < pixels; ++i)
                {
                    var segmentEnd = start with { X = start.X + 1 };
                    localColor += colorDiff;
                    shape.Line(start, segmentEnd, localColor, 2 * Im.Style.GlobalScale);
                    start = segmentEnd;
                }
            }

            Im.Window.DrawList.Shape.Line(start, end, color.Color!.Value, 2 * Im.Style.GlobalScale);
        }
    }

    /// <inheritdoc/>
    public void Draw(FileSystemCache cache, IFileSystemNode node, bool startsLine)
    {
        DrawLine(cache, node.Depth, Color, node.Parent?.LineColor ?? ColorParameter.Default);
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
