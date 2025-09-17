namespace Luna;

/// <summary> A flattened tree node that can be drawn in a linear list. </summary>
public interface IFlattenedTreeNode
{
    /// <summary> The local index of the parent of this node, -1 if no parent exists. </summary>
    public int ParentIndex { get; }

    /// <summary> The index of the last direct child node of this node. -1 if this has no child nodes. </summary>
    public int StartsLineTo { get; }

    /// <summary> The indentation depth, or depth inside the tree, of this node. Or the number of ancestors. </summary>
    public int IndentationDepth { get; }

    /// <summary> Draw this node. Should not indent itself. </summary>
    public void Draw();
}

public static class TreeLine
{
    /// <inheritdoc cref="Draw(IReadOnlyList{IFlattenedTreeNode},Rgba32,float,float)"/>
    public static void Draw(IReadOnlyList<IFlattenedTreeNode> list, Rgba32 lineColor)
        => Draw(list, lineColor, Im.Style.TextHeight, 2 * Im.Style.GlobalScale);

    /// <summary> Draw a tree structure with a child-associating line from a flattened tree node list. </summary>
    /// <param name="list"> The list of flattened nodes, expected to have a valid and sensible structure. </param>
    /// <param name="lineColor"> The color to draw the associative line in. </param>
    /// <param name="itemHeight"> The height of each node in the tree WITHOUT spacing. For regular tree nodes, this should be <see cref="Im.ImGuiStyle.TextHeight"/>. </param>
    /// <param name="lineWidth"> The width of the line to draw in pixels. Should generally be 1, 2, or 3 at most. Possibly scaled by the <see cref="Im.ImGuiStyle.GlobalScale"/>. Default is 2 scaled.  </param>
    public static void Draw(IReadOnlyList<IFlattenedTreeNode> list, Rgba32 lineColor, float itemHeight, float lineWidth)
    {
        if (list.Count is 0)
            return;

        // Note that lines may be aliased in different ways depending on the renderer.
        var itemHeightWithSpacing = itemHeight + Im.Style.ItemSpacing.Y;
        // The indentation width is specified by the style.
        var indentationWidth = Im.Style.IndentSpacing;
        // Since we assume regular tree nodes, we use the spacing of the label to offset the line.
        var spacing = Im.Style.TreeNodeToLabelSpacing / 2;
        // The general offset for the vertical line from the cursor point. The -0.5 is for optimizing DX line rendering.
        var lineOffset = new Vector2(spacing - 0.5f, itemHeight);
        // The general offset for the horizontal line from the cursor point. The -0.5 is for optimizing DX line rendering.
        var horizontalOffset = new Vector2(spacing - lineOffset.X, itemHeight / 2 - 0.5f);
        // Magic number for good looks on average.
        var lineSize = MathF.Max(0, indentationWidth - 9 * Im.Style.GlobalScale) - 0.5f;

        // Clip the list, despite the continuous lines.
        using var clipper    = new Im.ListClipper(list.Count, itemHeightWithSpacing);
        using var enumerator = clipper.GetEnumerator();
        // If we have no items, return. This should not be able to happen due to the early return before.
        if (!enumerator.MoveNext())
            return;

        var drawList = Im.Window.DrawList.Shape;

        // Keep track of the first index to differentiate potential parents clipped away,
        // and keep track of how many lines we are still missing.
        var firstIndex        = enumerator.Current;
        var hasMissingParents = list[firstIndex].IndentationDepth;

        var       currentDepth = 0;
        using var indent       = new Im.IndentDisposable();
        do
        {
            var currentItem = list[enumerator.Current];

            // Handle new indentation due to changed depth.
            if (currentItem.IndentationDepth != currentDepth)
            {
                indent.Indent((currentItem.IndentationDepth - currentDepth) * indentationWidth);
                currentDepth = currentItem.IndentationDepth;
            }

            // For any item that has a non-zero depth, we need to draw a horizontal line.
            if (currentDepth is not 0)
            {
                // Start point is the right-most point of the line,
                // and we go back according to the indentation difference, which ~should~ always be 1.
                var start = Im.Cursor.ScreenPosition + horizontalOffset;
                var diff  = currentDepth - list[currentItem.ParentIndex].IndentationDepth;
                var end   = start with { X = start.X - diff * indentationWidth + lineSize };
                drawList.Line(start, end, lineColor, lineWidth);
            }

            // If this node starts a line, draw it. 
            StartLine(drawList, currentItem, enumerator.Current, Im.Cursor.ScreenPosition);

            // Handle missing parents due to clipping.
            if (hasMissingParents > 0 && currentItem.ParentIndex >= 0 && currentItem.ParentIndex < firstIndex)
            {
                var parent = list[currentItem.ParentIndex];
                // We only draw the omitted lines for the last node of the parent for uniqueness.
                if (parent.StartsLineTo == enumerator.Current)
                {
                    var cursorOffset = new Vector2(indentationWidth, (enumerator.Current - currentItem.ParentIndex) * itemHeightWithSpacing);
                    StartLine(drawList, parent, currentItem.ParentIndex, Im.Cursor.ScreenPosition - cursorOffset);
                    --hasMissingParents;
                }
            }

            // Draw the item itself under the ID of the current index.
            using var id = Im.Id.Push(enumerator.Current);
            currentItem.Draw();
        } while (enumerator.MoveNext());

        return;

        void StartLine(in Im.DrawList.DrawListShapes drawList, in IFlattenedTreeNode node, int nodeIndex, Vector2 startPosition)
        {
            // If this node does not start a line, skip.
            if (node.StartsLineTo < 0)
                return;

            // Start is straight below the downward triangle for a tree node, immediately outside the selectable area.
            var start = startPosition + lineOffset;
            // End is at the center of the last horizontal line.
            var end = start with { Y = start.Y + (node.StartsLineTo - nodeIndex) * itemHeightWithSpacing - lineOffset.Y / 2 };
            drawList.Line(start, end, lineColor, lineWidth);
        }
    }
}
