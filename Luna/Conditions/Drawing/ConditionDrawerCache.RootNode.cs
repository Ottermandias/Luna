using ImSharp.ImNodes;

namespace Luna;

public abstract partial class ConditionDrawerCache<TContext>
{
    /// <summary> The root node that always exists even if no condition is set. </summary>
    /// <param name="id"><inheritdoc/></param>
    /// <param name="setter"><inheritdoc/></param>
    /// <param name="input"> The unique ID of the input pin. </param>
    /// <param name="isEmpty"> Whether there is currently not any condition set. </param>
    protected sealed class RootNode(NodeId id, Action<ICondition<TContext>?> setter, AttributeId input, bool isEmpty)
        : Node(id, setter, ParentConditionType.Root, 0)
    {
        public readonly AttributeId Input = input;

        /// <inheritdoc/>
        public override Rgba32 TitleColor(ConditionDrawerCache<TContext> drawerCache)
            => drawerCache.NodeColors.RootTitle;

        /// <inheritdoc/>
        public override Rgba32 BorderColor(ConditionDrawerCache<TContext> drawerCache)
            => drawerCache.NodeColors.RootBorder;

        public override Vector2 GetOwnSize(ConditionDrawerCache<TContext> drawerCache)
            => drawerCache.RootNodeSize;

        public override bool DrawContent(ConditionDrawerCache<TContext> drawerCache, ImSharp.ImNodes.Node node)
        {
            DrawInputConnector(node, drawerCache.RootNodeSize, Input);
            // Draw The root title.
            using (node.TitleBar())
            {
                ImEx.TextFramed("Condition"u8, drawerCache.RootNodeSize with { Y = drawerCache.ButtonSize.Y }, Rgba32.Transparent);
            }

            // Draw either the 'Add Condition'-button or the 'Delete all conditions'-button.
            var ret = false;
            var buttonSize = new Vector2(drawerCache.RootNodeSize.X - 2 * ImNodes.Style.NodeBorderThickness,
                drawerCache.ButtonSize.Y - ImNodes.Style.NodeBorderThickness);
            Im.Cursor.X += ImNodes.Style.NodeBorderThickness;
            if (isEmpty)
            {
                ret = drawerCache.DrawAddRootConditionButton(this, drawerCache, node, buttonSize);
            }
            else
            {
                if (ImEx.Icon.ButtonCorners(LunaStyle.DeleteIcon, Corners.Bottom, "Delete the entire condition for this object."u8,
                        !LunaStyle.Modifier.Destructive, buttonSize))
                {
                    Setter(null);
                    ret = true;
                }

                LunaStyle.Modifier.Destructive.TooltipLineBreak("delete"u8);
            }

            return ret;
        }
    }
}
