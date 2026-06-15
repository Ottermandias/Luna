namespace Luna;

/// <summary> A condition that evaluates to true if any of its subconditions evaluates to true. </summary>
/// <typeparam name="TContext"><inheritdoc cref="ICondition{TContext}"/></typeparam>
public sealed class OrCondition<TContext>() : ListCondition<TContext>
    where TContext : IConditionContext<TContext>
{
    /// <summary> Create an Or-Condition from existing conditions. </summary>
    public OrCondition(params IReadOnlyList<ICondition<TContext>> conditions)
        : this()
    {
        EnsureCapacity(conditions.Count);
        AddRange(conditions);
    }

    /// <inheritdoc/>
    protected override ReadOnlySpan<byte> Type
        => "Or"u8;

    /// <inheritdoc/>
    public override bool Evaluate(in TContext context)
    {
        foreach (var condition in this)
        {
            if (condition.Evaluate(context))
                return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public override ICondition<TContext> Reduce()
    {
        // Reduce all child conditions, then check for any that are always true, remove all that are always false.
        for (var i = Count - 1; i >= 0; --i)
        {
            var condition = this[i].Reduce();
            switch (condition)
            {
                case TrueCondition<TContext>:  return TrueCondition<TContext>.Instance;
                case FalseCondition<TContext>: RemoveAt(i); break;
                case OrCondition<TContext> subOr:
                    RemoveAt(i);
                    InsertRange(i, subOr);
                    break;
                default: this[i] = condition; break;
            }
        }

        // If a single condition remains, return that. If none remain, this is always false.
        return Count switch
        {
            0 => FalseCondition<TContext>.Instance,
            1 => this[0],
            _ => this,
        };
    }


    /// <inheritdoc/>
    public override ICondition<TContext> DeepCopy()
    {
        var ret = new OrCondition<TContext>();
        ret.EnsureCapacity(Count);
        ret.AddRange(this.Select(c => c.DeepCopy()));
        return ret;
    }


    /// <inheritdoc/>
    public override bool Equals(ICondition<TContext>? other)
        => other is OrCondition<TContext> a && a.SequenceEqual(this);

    /// <inheritdoc/>
    public override int GetHashCode()
        => this.Aggregate(typeof(OrCondition<TContext>).GetHashCode(), HashCode.Combine);
}
