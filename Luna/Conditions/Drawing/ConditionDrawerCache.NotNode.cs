using ImSharp.ImNodes;

namespace Luna;

public abstract partial class ConditionDrawerCache<TContext>
{
    /// <summary> Handle 'Not'-conditions and their children. </summary>
    protected virtual OutputNode HandleNotCondition(Action<ICondition<TContext>?> setter,
        NotCondition<TContext> not, ParentConditionType parent, byte depth)
    {
        // Create the new node.
        var node    = new NotNode(IdCounter++, not, setter, IdCounter++, IdCounter++, parent, depth);
        var ownSize = node.GetOwnSize(this);
        AddNode(node, ownSize.X, true);

        var output = HandleCondition(c =>
        {
            if (c is null)
                setter(null);
            else
                not.Condition = c;
        }, not.Condition, ParentConditionType.Not, (byte)(depth + 1));
        node.SubtreeHeight = MathF.Max(output!.SubtreeHeight, ownSize.Y);

        Links.Add(new Link(IdCounter++, output.Output, node.Input));
        return node;
    }

    /// <summary> A node representing a 'Not'-condition. </summary>
    /// <inheritdoc/>
    protected sealed class NotNode(
        NodeId id,
        NotCondition<TContext> condition,
        Action<ICondition<TContext>?> setter,
        AttributeId input,
        AttributeId output,
        ParentConditionType parent,
        byte depth)
        : ConnectorNode(id, condition, setter, input, output, parent, depth)
    {
        /// <inheritdoc/>
        protected override ReadOnlySpan<byte> Text
            => "Not"u8;

        /// <inheritdoc/>
        public override Rgba32 TitleColor(ConditionDrawerCache<TContext> drawerCache)
            => drawerCache.NodeColors.NotTitle;

        /// <inheritdoc/>
        public override Rgba32 BorderColor(ConditionDrawerCache<TContext> drawerCache)
            => drawerCache.NodeColors.NotBorder;

        public override Vector2 GetOwnSize(ConditionDrawerCache<TContext> drawerCache)
            => drawerCache.NotNodeSize;

        protected override bool DrawButtons(ConditionDrawerCache<TContext> drawerCache, ImSharp.ImNodes.Node node, Vector2 actualSize)
        {
            var buttonSize = new Vector2(actualSize.X / 2 - ImNodes.Style.NodeBorderThickness,
                drawerCache.ButtonSize.Y - ImNodes.Style.NodeBorderThickness);
            Im.Cursor.X += ImNodes.Style.NodeBorderThickness;
            var ret = DeleteConditionButton(drawerCache, true, buttonSize);
            // Un-negate button
            Im.Line.NoSpacing();
            if (ImEx.Icon.ButtonCorners(LunaStyle.RemoveNegateIcon, Corners.BottomRight, "Remove the negation from this child's condition."u8,
                    false, buttonSize))
            {
                Setter(((NotCondition<TContext>)Condition).Condition.Reduce());
                ret = true;
            }

            return ret;
        }
    }
}
