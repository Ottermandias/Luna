using Dalamud.Interface;
using ImSharp.ImNodes;

namespace Luna;

public abstract partial class ConditionDrawerCache<TContext>
{
    /// <summary> Handle custom condition types. </summary>
    protected virtual CustomNode? HandleCustomCondition(Action<ICondition<TContext>?> setter,
        ICondition<TContext> condition, ParentConditionType parent, byte depth)
        => null;

    /// <summary> The base class for custom nodes, i.e. actual literal conditions. </summary>
    /// <inheritdoc/>
    protected abstract class CustomNode(
        NodeId id,
        ICondition<TContext> condition,
        Action<ICondition<TContext>?> setter,
        AttributeId output,
        ParentConditionType parent,
        byte depth)
        : OutputNode(id, setter, output, parent, depth)
    {
        public readonly ICondition<TContext> Condition = condition;

        /// <inheritdoc/>
        public override Rgba32 TitleColor(ConditionDrawerCache<TContext> drawerCache)
            => drawerCache.NodeColors.CustomTitle;

        /// <inheritdoc/>
        public override Rgba32 BorderColor(ConditionDrawerCache<TContext> drawerCache)
            => drawerCache.NodeColors.CustomBorder;

        /// <summary> A button that adds a new condition conjunctive with the given condition. </summary>
        /// <param name="drawerCache"> The parent cache. </param>
        /// <param name="node"> The current node. </param>
        /// <param name="buttonSize"> The size of this button. </param>
        /// <param name="corners"> Which corners to round on this button. </param>
        /// <returns> True if a new condition was added. </returns>
        public virtual bool AddAndButton(ConditionDrawerCache<TContext> drawerCache, ImSharp.ImNodes.Node node, Vector2 buttonSize,
            Corners corners = Corners.None)
        {
            if (!ImEx.Icon.ButtonCorners(FontAwesomeIcon.UserTimes.Icon(), corners,
                    "Replace this with an And-condition of this with a new condition. This may be reduced for simplicity."u8,
                    false, buttonSize)
             || drawerCache.CreateNewCondition() is not { } condition)
                return false;

            Setter(new AndCondition<TContext>(Condition, condition).Reduce());
            return true;
        }

        /// <summary> A button that adds a new condition disjunctive with the given condition. </summary>
        /// <param name="drawerCache"> The parent cache. </param>
        /// <param name="node"> The current node. </param>
        /// <param name="buttonSize"> The size of this button. </param>
        /// <param name="corners"> Which corners to round on this button. </param>
        /// <returns> True if a new condition was added. </returns>
        public virtual bool AddOrButton(ConditionDrawerCache<TContext> drawerCache, ImSharp.ImNodes.Node node, Vector2 buttonSize,
            Corners corners = Corners.None)
        {
            if (!ImEx.Icon.ButtonCorners(FontAwesomeIcon.UserPlus.Icon(), corners,
                    "Replace this with an Or-condition of this with a new condition. This may be reduced for simplicity."u8,
                    false, buttonSize)
             || drawerCache.CreateNewCondition() is not { } condition)
                return false;

            Setter(new OrCondition<TContext>(Condition, condition).Reduce());
            return true;
        }

        /// <summary> Draw the 4 default buttons, 'Delete', 'Add (Conjunctive)', 'Add (Disjunctive)', and 'Negate'. </summary>
        /// <param name="drawerCache"> The parent cache. </param>
        /// <param name="node"> The current node. </param>
        /// <param name="buttonSize"> The size of each individual button. </param>
        /// <returns> True if any button was pressed. </returns>
        public virtual bool DefaultButtons(ConditionDrawerCache<TContext> drawerCache, ImSharp.ImNodes.Node node, Vector2 buttonSize)
        {
            var reducedSize = buttonSize with { X = buttonSize.X - ImNodes.Style.NodeBorderThickness };
            var ret         = DeleteConditionButton(drawerCache, false, reducedSize);
            Im.Line.NoSpacing();
            ret |= AddAndButton(drawerCache, node, buttonSize);
            Im.Line.NoSpacing();
            ret |= Parent is ParentConditionType.Not
                ? AddOrButton(drawerCache, node, reducedSize, Corners.BottomRight)
                : AddOrButton(drawerCache, node, buttonSize,  Corners.None);
            ret |= NegateButton(drawerCache, Condition, reducedSize);

            return ret;
        }
    }
}
