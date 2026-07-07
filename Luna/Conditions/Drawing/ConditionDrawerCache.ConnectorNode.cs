using ImSharp.ImNodes;

namespace Luna;

public abstract partial class ConditionDrawerCache<TContext>
{
    /// <summary> Handle 'And'- and 'Or'-conditions and their children. </summary>
    protected virtual OutputNode HandleListCondition(Action<ICondition<TContext>?> setter,
        ListCondition<TContext> condition, ParentConditionType parent, byte depth)
    {
        // Create the new node.
        ConnectorNode node = condition is OrCondition<TContext> or
            ? new OrNode(IdCounter++, or, setter, IdCounter++, IdCounter++, parent, depth)
            : new AndNode(IdCounter++, (AndCondition<TContext>)condition, setter, IdCounter++, IdCounter++, parent, depth);
        var ownSize = node.GetOwnSize(this);
        AddNode(node, ownSize.X, condition.Count > 0);

        var totalHeight = 0f;
        // Set the current position, iterate through all children and compute their total height.
        var type = node is OrNode ? ParentConditionType.Or : ParentConditionType.And;
        ++depth;
        for (var i = 0; i < condition.Count; ++i)
        {
            var subIndex = i;
            var output = HandleCondition(c =>
            {
                if (c is null)
                    condition.RemoveAt(subIndex);
                else
                    condition[subIndex] = c;
            }, condition[subIndex], type, depth);
            totalHeight += output!.SubtreeHeight;
            Links.Add(new Link(IdCounter++, output.Output, node.Input));
        }

        // Remove the last unnecessary spacing and compute total Height.
        totalHeight        += (condition.Count - 1) * NodeSpacing.Y;
        node.SubtreeHeight =  MathF.Max(totalHeight, ownSize.Y);
        return node;
    }

    /// <summary> A basic connector node with an input and an output, i.e. anything that is not a literal condition node. </summary>
    /// <param name="id"><inheritdoc/></param>
    /// <param name="condition"> The actual condition in the condition tree. </param>
    /// <param name="setter"><inheritdoc/></param>
    /// <param name="input"> The unique ID of the input pin. </param>
    /// <param name="output"><inheritdoc/></param>
    /// <param name="parent"><inheritdoc/></param>
    /// <param name="depth"><inheritdoc/></param>
    protected abstract class ConnectorNode(
        NodeId id,
        ICondition<TContext> condition,
        Action<ICondition<TContext>?> setter,
        AttributeId input,
        AttributeId output,
        ParentConditionType parent,
        byte depth)
        : OutputNode(id, setter, output, parent, depth)
    {
        public readonly ICondition<TContext> Condition = condition;
        public readonly AttributeId          Input     = input;

        /// <summary> Get the title text of this connector node, will be 'And', 'Or', or 'Not'. </summary>
        protected abstract ReadOnlySpan<byte> Text { get; }

        /// <inheritdoc/>
        public override Vector2 GetOwnSize(ConditionDrawerCache<TContext> drawerCache)
            => Parent is ParentConditionType.Not
                ? drawerCache.ConnectorNodeSize with { X = 3 * drawerCache.ButtonSize.X }
                : drawerCache.ConnectorNodeSize;

        /// <summary> Draw a button to add a new condition to the And or Or condition. </summary>
        /// <param name="drawerCache"> The parent cache. </param>
        /// <param name="buttonSize"> The size of the button. </param>
        /// <returns> Whether a new condition was added. </returns>
        protected bool AddConditionButton(ConditionDrawerCache<TContext> drawerCache, Vector2 buttonSize)
        {
            if (ImEx.Icon.ButtonCorners(LunaStyle.AddObjectIcon, Corners.None, "Add a new condition to this condition group."u8, false,
                    buttonSize)
             && drawerCache.CreateNewCondition() is { } newCondition)
            {
                ((ListCondition<TContext>)Condition).Add(newCondition);
                return true;
            }

            return false;
        }

        /// <summary> Draw the available buttons for this connector node. </summary>
        /// <param name="drawerCache"> The parent cache. </param>
        /// <param name="node"> The current node. </param>
        /// <param name="actualSize"> The actual size of the node. </param>
        /// <returns> True if the node was changed or assigned to. </returns>
        protected abstract bool DrawButtons(ConditionDrawerCache<TContext> drawerCache, ImSharp.ImNodes.Node node, Vector2 actualSize);

        /// <inheritdoc/>
        public override bool DrawContent(ConditionDrawerCache<TContext> drawerCache, ImSharp.ImNodes.Node node)
        {
            var       requiredSize = GetActualSize(drawerCache);
            using var id           = Im.Id.Push(node.Id);
            DrawInputConnector(node, requiredSize, Input);
            DrawOutputConnector(node, requiredSize, Output);

            // Then draw a frame-height title bar group.
            using (node.TitleBar())
            {
                ImEx.TextFramed(Text, requiredSize with { Y = drawerCache.ButtonSize.Y }, Rgba32.Transparent);
            }

            // And a frame-height row of buttons without spacing for a total height of 2 * frame height.
            return DrawButtons(drawerCache, node, requiredSize);
        }
    }
}
