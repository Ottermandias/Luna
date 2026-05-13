using System.Text.Json;

namespace Luna;

/// <summary> A condition that evaluates to true if any of its subconditions evaluates to true. </summary>
/// <typeparam name="TContext"><inheritdoc cref="ICondition{TContext}"/></typeparam>
public sealed class OrCondition<TContext>() : List<ICondition<TContext>>(), ICondition<TContext>
{
    /// <summary> Create an Or-Condition from existing conditions. </summary>
    public OrCondition(params IReadOnlyList<ICondition<TContext>> conditions)
        : this()
    {
        EnsureCapacity(conditions.Count);
        AddRange(conditions);
    }

    /// <inheritdoc/>
    public bool Evaluate(in TContext context)
    {
        foreach (var condition in this)
        {
            if (condition.Evaluate(context))
                return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public ICondition<TContext> Reduce()
    {
        // Reduce all child conditions, then check for any that are always true, remove all that are always false.
        for (var i = Count - 1; i >= 0; --i)
        {
            var condition = this[i].Reduce();
            switch (condition)
            {
                case TrueCondition<TContext>:  return TrueCondition<TContext>.Instance;
                case FalseCondition<TContext>: RemoveAt(i); break;
                default:                       this[i] = condition; break;
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
    public void WriteJson(Utf8JsonWriter j)
    {
        j.WriteStartObject();
        j.WriteString("Type"u8, "Or"u8);
        j.WriteStartArray("Conditions"u8);
        foreach (var condition in this)
            condition.WriteJson(j);
        j.WriteEndArray();
        j.WriteEndObject();
    }

    /// <inheritdoc/>
    public ICondition<TContext> DeepCopy()
    {
        var ret = new AndCondition<TContext>();
        ret.EnsureCapacity(Count);
        ret.AddRange(this.Select(c => c.DeepCopy()));
        return ret;
    }

    /// <inheritdoc/>
    public IEnumerable<ICondition<TContext>> Subconditions
        => this.SelectMany(c => c.Subconditions);

    /// <inheritdoc/>
    public int RemoveSubconditions(Func<ICondition<TContext>, bool> predicate)
    {
        var sum = 0;
        for (var i = Count - 1; i >= 0; --i)
        {
            sum += this[i].RemoveSubconditions(predicate);
            if (predicate(this[i]))
            {
                ++sum;
                RemoveAt(i);
            }
        }

        return sum;
    }
}
