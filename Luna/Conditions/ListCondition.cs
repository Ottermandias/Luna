using System.Text.Json;

namespace Luna;

/// <summary> Base for list-based conditions. </summary>
/// <typeparam name="TContext"> The evaluation context. </typeparam>
public abstract class ListCondition<TContext> : List<ICondition<TContext>>, ICondition<TContext>
    where TContext : IConditionContext<TContext>
{
    /// <summary> Used for JSON serialization. </summary>
    protected abstract ReadOnlySpan<byte> Type { get; }

    /// <inheritdoc/>
    public IEnumerable<ICondition<TContext>> Subconditions
        => this.SelectMany(c => c.Subconditions.Prepend(c));

    /// <inheritdoc/>
    public int RemoveSubconditions(Func<ICondition<TContext>, bool> predicate)
    {
        var sum = 0;
        for (var i = Count - 1; i >= 0; --i)
        {
            sum += this[i].RemoveSubconditions(predicate);
            if (!predicate(this[i]))
                continue;

            ++sum;
            RemoveAt(i);
        }

        return sum;
    }

    /// <inheritdoc/>
    public ICondition<TContext>? EditConditions(Func<ICondition<TContext>, ICondition<TContext>?> method)
    {
        var changes = false;
        for (var i = Count - 1; i >= 0; --i)
        {
            if (this[i].EditConditions(method) is not { } newCondition)
                continue;

            this[i] = newCondition;
            changes = true;
        }

        if (method(this) is { } retCondition)
            return retCondition;
        if (changes)
            return this;

        return null;
    }

    /// <inheritdoc/>
    public abstract bool Evaluate(in TContext context);

    /// <inheritdoc/>
    public abstract ICondition<TContext> Reduce();

    /// <inheritdoc/>
    public void WriteJson(Utf8JsonWriter j)
    {
        j.WriteStartObject();
        j.WriteString("Type"u8, Type);
        j.WriteStartArray("Conditions"u8);
        foreach (var condition in this)
            condition.WriteJson(j);
        j.WriteEndArray();
        j.WriteEndObject();
    }

    /// <inheritdoc/>
    public abstract ICondition<TContext> DeepCopy();

    /// <inheritdoc/>
    public abstract bool Equals(ICondition<TContext>? other);
}
