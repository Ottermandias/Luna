using ImSharp.ImNodes;

namespace Luna;

public abstract partial class ConditionDrawerCache<TContext>
{
    /// <summary> Handle 'True'- and 'False' conditions. </summary>
    protected virtual OutputNode HandleConstantCondition(Action<ICondition<TContext>?> setter, bool value,
        ParentConditionType parent, byte depth)
    {
        var node    = new ConstantNode(IdCounter++, setter, IdCounter++, value, parent, depth);
        var ownSize = node.GetOwnSize(this);
        AddNode(node, ownSize.X, false);
        node.SubtreeHeight = ownSize.Y;

        return node;
    }

    /// <summary> Constant nodes should probably never exist after reduction. </summary>
    /// <inheritdoc/>
    protected sealed class ConstantNode(
        NodeId id,
        Action<ICondition<TContext>?> setter,
        AttributeId output,
        bool value,
        ParentConditionType parent,
        byte depth)
        : OutputNode(id, setter, output, parent, depth)
    {
        public readonly bool Value = value;

        /// <inheritdoc/>
        public override Rgba32 TitleColor(ConditionDrawerCache<TContext> drawerCache)
            => drawerCache.NodeColors.ConstantTitle;

        /// <inheritdoc/>
        public override Rgba32 BorderColor(ConditionDrawerCache<TContext> drawerCache)
            => drawerCache.NodeColors.ConstantBorder;

        public override bool DrawContent(ConditionDrawerCache<TContext> drawerCache, ImSharp.ImNodes.Node node)
        {
            DrawOutputConnector(node, drawerCache.ConnectorNodeSize, Output);
            using (node.TitleBar())
            {
                ImEx.TextFramed(Value ? "True"u8 : "False"u8, drawerCache.ConstantNodeSize with { Y = drawerCache.ButtonSize.Y },
                    Rgba32.Transparent);
            }

            var buttonSize = new Vector2(MathF.Round(drawerCache.ConstantNodeSize.X / 2) - ImNodes.Style.NodeBorderThickness, drawerCache.ButtonSize.Y - ImNodes.Style.NodeBorderThickness);
            Im.Cursor.X += ImNodes.Style.NodeBorderThickness;
            var ret        = DeleteConditionButton(drawerCache, false, buttonSize);
            Im.Line.NoSpacing();
            if (ImEx.Icon.ButtonCorners(Value ? LunaStyle.NegateIcon : LunaStyle.RemoveNegateIcon, Corners.BottomRight,
                    "Flip this constant condition."u8, true, buttonSize))
            {
                Setter(Value ? FalseCondition<TContext>.Instance : TrueCondition<TContext>.Instance);
                ret = true;
            }

            return ret;
        }
    }
}
