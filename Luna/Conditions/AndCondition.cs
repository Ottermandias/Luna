using System.Text.Json;

namespace Luna;

/// <summary> A condition that evaluates to true if all its subconditions evaluate to true. </summary>
/// <typeparam name="TContext"><inheritdoc cref="ICondition{TContext}"/></typeparam>
public sealed class AndCondition<TContext>() : List<ICondition<TContext>>(), ICondition<TContext>
{
    /// <summary> Create an And-Condition from existing conditions. </summary>
    public AndCondition(params IReadOnlyList<ICondition<TContext>> conditions)
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
            if (!condition.Evaluate(context))
                return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public ICondition<TContext> Reduce()
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
    public void WriteJson(Utf8JsonWriter j)
    {
        j.WriteStartObject();
        j.WriteString("Type"u8, "And"u8);
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
}
