using ImSharp.ImNodes;

namespace Luna;

public abstract partial class ConditionDrawerCache<TContext>
{
    /// <summary> A node that has an output, i.e. anything but the root node. </summary>
    /// <param name="id"><inheritdoc/></param>
    /// <param name="setter"><inheritdoc/></param>
    /// <param name="output"> The unique ID of the output pin. </param>
    /// <param name="parent"><inheritdoc/></param>
    /// <param name="depth"><inheritdoc/></param>
    protected abstract class OutputNode(
        NodeId id,
        Action<ICondition<TContext>?> setter,
        AttributeId output,
        ParentConditionType parent,
        byte depth)
        : Node(id, setter, parent, depth)
    {
        public readonly AttributeId Output = output;
    }
}
