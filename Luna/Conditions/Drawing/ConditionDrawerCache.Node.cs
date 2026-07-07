using ImSharp.ImNodes;

namespace Luna;

public abstract partial class ConditionDrawerCache<TContext>
{
    /// <summary> Add a node to the cache and update the minimum width for its depth level accordingly. </summary>
    /// <param name="node"> The node to add. </param>
    /// <param name="width"> The minimum width required for this node. </param>
    /// <param name="hasChildren"> Whether the node has children. If it has no children, it does not increase the minimum width. </param>
    protected void AddNode(Node node, float width, bool hasChildren)
    {
        Nodes.Add(node);
        if (!hasChildren)
            return;

        if (DepthWidth.Count == node.Depth)
            DepthWidth.Add(width);
        else if (DepthWidth[node.Depth] < width)
            DepthWidth[node.Depth] = width;
    }

    /// <summary> The base class for node data. </summary>
    /// <param name="id"> The unique ID of the node. </param>
    /// <param name="setter"> A setter invoked when this condition should change through assignment. </param>
    /// <param name="parent"> The node type that is the parent of this condition. </param>
    /// <param name="depth"> The depth of the condition. </param>
    protected abstract class Node(NodeId id, Action<ICondition<TContext>?> setter, ParentConditionType parent, byte depth)
    {
        public readonly Action<ICondition<TContext>?> Setter = setter;
        public readonly NodeId                        Id     = id;
        public readonly ParentConditionType           Parent = parent;
        public readonly byte                          Depth  = depth;
        public          Vector2                       Position;
        public          float                         SubtreeHeight;

        /// <summary> The color of the node title. </summary>
        public virtual Rgba32 TitleColor(ConditionDrawerCache<TContext> drawerCache)
            => Rgba32.Transparent;

        /// <summary> The color of the node border. </summary>
        public virtual Rgba32 BorderColor(ConditionDrawerCache<TContext> drawerCache)
            => Rgba32.Transparent;

        /// <summary> Draw the content of the node, including buttons to manipulate it. </summary>
        /// <param name="drawerCache"> The parent cache drawing this whole editor. </param>
        /// <param name="node"> The current node drawn in the editor. </param>
        /// <returns> True if the node changed or was replaced via the setter. </returns>
        /// <remarks> Note that Input and Output pins are swapped since we are drawing from left to right. </remarks>
        public abstract bool DrawContent(ConditionDrawerCache<TContext> drawerCache, ImSharp.ImNodes.Node node);

        /// <summary> Get the size required to draw this node for position calculations. </summary>
        /// <param name="drawerCache"> The parent cache drawing this whole editor. </param>
        /// <returns> The size required for this node without spacing. </returns>
        public virtual Vector2 GetOwnSize(ConditionDrawerCache<TContext> drawerCache)
            => drawerCache.ConnectorNodeSize;

        /// <summary> Get the actual size, including the enlarged width due to wider nodes on the same level that have children. </summary>
        /// <param name="drawerCache"> The parent cache drawing this whole editor. </param>
        /// <returns> The size to draw this node in. </returns>
        public Vector2 GetActualSize(ConditionDrawerCache<TContext> drawerCache)
        {
            var ownSize = GetOwnSize(drawerCache);
            if (drawerCache.DepthWidth.Count > Depth && drawerCache.DepthWidth[Depth] > ownSize.X)
                return ownSize with { X = drawerCache.DepthWidth[Depth] };

            return ownSize;
        }

        /// <summary> Draw a vertically centered input pin on the right side (which means it is technically an output pin). </summary>
        /// <param name="node"> The node to draw the pin for. </param>
        /// <param name="size"> The size of the node, to center it. </param>
        /// <param name="input"> The unique ID of the input pin. </param>
        /// <param name="inputShape"> The shape of the input pin. </param>
        protected static void DrawInputConnector(ImSharp.ImNodes.Node node, Vector2 size, AttributeId input,
            PinShape inputShape = PinShape.Circle)
        {
            var pos = Im.Cursor.Position;
            using (AttributeFlags.DisableInteractivity.Push())
            {
                using (node.OutputPin(input, inputShape))
                {
                    Im.Dummy(size);
                }
            }

            Im.Cursor.Position = pos;
        }

        /// <summary> Draw a vertically centered output pin on the left side (which means it is technically an input pin). </summary>
        /// <param name="node"> The node to draw the pin for. </param>
        /// <param name="size"> The size of the node, to center it. </param>
        /// <param name="output"> The unique ID of the output pin. </param>
        /// <param name="outputShape"> The shape of the output pin. </param>
        protected static void DrawOutputConnector(ImSharp.ImNodes.Node node, Vector2 size, AttributeId output,
            PinShape outputShape = PinShape.CircleFilled)
        {
            var pos = Im.Cursor.Position;
            using (AttributeFlags.DisableInteractivity.Push())
            {
                using (node.InputPin(output, outputShape))
                {
                    Im.Dummy(size);
                }
            }

            Im.Cursor.Position = pos;
        }

        /// <summary> Draw a button to delete this condition, with destructive protection if it is a node with input. </summary>
        /// <param name="drawerCache"> The parent cache. </param>
        /// <param name="input"> Whether the node has an input. </param>
        /// <param name="buttonSize"> The size of the button. </param>
        /// <param name="corners"> Which corners to round on this button. </param>
        /// <returns> True if the node was deleted. </returns>
        protected bool DeleteConditionButton(ConditionDrawerCache<TContext> drawerCache, bool input, Vector2 buttonSize,
            Corners corners = Corners.BottomLeft)
        {
            var ret      = false;
            var disabled = input && !LunaStyle.Modifier.Destructive;
            if (ImEx.Icon.ButtonCorners(LunaStyle.DeleteIcon, corners, "Delete this condition and everything connected to its input."u8,
                    disabled, buttonSize))
            {
                Setter(null);
                ret = true;
            }

            if (disabled)
                LunaStyle.Modifier.Destructive.TooltipLineBreak("delete"u8);

            return ret;
        }

        /// <summary> Draw a button to negate this condition only if it is not already negated. This includes the necessary same line. </summary>
        /// <param name="drawerCache"> The parent cache. </param>
        /// <param name="condition"> The condition to negate. </param>
        /// <param name="buttonSize"> The size of the button. </param>
        /// <param name="corners"> Which corners to round on this button. </param>
        /// <returns> True if the node was deleted. </returns>
        protected bool NegateButton(ConditionDrawerCache<TContext> drawerCache, ICondition<TContext> condition, Vector2 buttonSize,
            Corners corners = Corners.BottomRight)
        {
            // Negate button, if not already negated - in which case the 'Not'-node has the un-negate button.
            if (Parent is ParentConditionType.Not)
                return false;

            Im.Line.NoSpacing();
            if (ImEx.Icon.ButtonCorners(LunaStyle.NegateIcon, corners, "Negate this condition by making it the child of a Not-condition."u8,
                    false, buttonSize))
            {
                Setter(new NotCondition<TContext>(condition).Reduce());
                return true;
            }

            return false;
        }
    }
}
