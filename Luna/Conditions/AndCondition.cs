namespace Luna;

/// <summary> A condition that evaluates to true if all its subconditions evaluate to true. </summary>
/// <typeparam name="TContext"><inheritdoc cref="ICondition{TContext}"/></typeparam>
public sealed class AndCondition<TContext>() : ListCondition<TContext>
    where TContext : IConditionContext<TContext>
{
    /// <summary> Create an And-Condition from existing conditions. </summary>
    public AndCondition(params IReadOnlyList<ICondition<TContext>> conditions)
        : this()
    {
        EnsureCapacity(conditions.Count);
        AddRange(conditions);
    }

    /// <inheritdoc/>
    protected override ReadOnlySpan<byte> Type
        => "And"u8;

    /// <inheritdoc/>
    public override bool Evaluate(in TContext context)
    {
        foreach (var condition in this)
        {
            if (!condition.Evaluate(context))
                return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public override ICondition<TContext> Reduce()
    {
        // Reduce all child conditions, then check for any that are always false, remove all that are always true.
        for (var i = Count - 1; i >= 0; --i)
        {
            var condition = this[i].Reduce();
            switch (condition)
            {
                case TrueCondition<TContext>:  RemoveAt(i); break;
                case FalseCondition<TContext>: return FalseCondition<TContext>.Instance;
                default:                       this[i] = condition; break;
            }
        }

        // If a single condition remains, return that. If none remain, this is always true.
        return Count switch
        {
            0 => TrueCondition<TContext>.Instance,
            1 => this[0],
            _ => this,
        };
    }

    /// <inheritdoc/>
    public override ICondition<TContext> DeepCopy()
    {
        var ret = new AndCondition<TContext>();
        ret.EnsureCapacity(Count);
        ret.AddRange(this.Select(c => c.DeepCopy()));
        return ret;
    }

    /// <inheritdoc/>
    public override bool Equals(ICondition<TContext>? other)
        => other is AndCondition<TContext> a && a.SequenceEqual(this);

    /// <inheritdoc/>
    public override int GetHashCode()
        => this.Aggregate(typeof(AndCondition<TContext>).GetHashCode(), HashCode.Combine);
}
