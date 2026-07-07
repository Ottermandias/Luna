using ImSharp.ImNodes;

namespace Luna;

public abstract partial class ConditionDrawerCache<TContext>
{
    /// <summary> A node representing an 'Or'-condition. </summary>
    /// <inheritdoc/>
    protected sealed class OrNode(
        NodeId id,
        OrCondition<TContext> condition,
        Action<ICondition<TContext>?> setter,
        AttributeId input,
        AttributeId output,
        ParentConditionType parent,
        byte depth)
        : ConnectorNode(id, condition, setter, input, output, parent, depth)
    {
        /// <inheritdoc/>
        protected override ReadOnlySpan<byte> Text
            => "Or"u8;

        /// <inheritdoc/>
        public override Rgba32 TitleColor(ConditionDrawerCache<TContext> drawerCache)
            => drawerCache.NodeColors.OrTitle;

        /// <inheritdoc/>
        public override Rgba32 BorderColor(ConditionDrawerCache<TContext> drawerCache)
            => drawerCache.NodeColors.OrBorder;

        /// <inheritdoc/>
        protected override bool DrawButtons(ConditionDrawerCache<TContext> drawerCache, ImSharp.ImNodes.Node node, Vector2 actualSize)
        {
            var buttonSize = new Vector2(actualSize.X / (Parent is ParentConditionType.Not ? 3 : 4),
                drawerCache.ButtonSize.Y - ImNodes.Style.NodeBorderThickness);
            var reducedButton = buttonSize with { X = buttonSize.X - ImNodes.Style.NodeBorderThickness };
            Im.Cursor.X += ImNodes.Style.NodeBorderThickness;
            var ret = DeleteConditionButton(drawerCache, true, reducedButton);
            Im.Line.NoSpacing();
            ret |= AddConditionButton(drawerCache, buttonSize);

            // Flip And/Or button.
            Im.Line.NoSpacing();
            if (ImEx.Icon.ButtonCorners(LunaStyle.SwitchIcon, Parent is ParentConditionType.Not ? Corners.BottomRight : Corners.None,
                    "Turn this entire Or-Condition into an And-Condition while keeping its input. This may merge it with its parent Or-Condition."u8,
                    !LunaStyle.Modifier.Misclick, Parent is ParentConditionType.Not ? reducedButton : buttonSize))
            {
                Setter(new AndCondition<TContext>((IReadOnlyList<ICondition<TContext>>)Condition).Reduce());
                ret = true;
            }

            LunaStyle.Modifier.Misclick.TooltipLineBreak("flip"u8);
            ret |= NegateButton(drawerCache, Condition, reducedButton);
            return ret;
        }
    }
}
