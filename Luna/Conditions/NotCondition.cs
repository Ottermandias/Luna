using System.Text.Json;

namespace Luna;

/// <summary> A condition that inverts its subcondition. </summary>
/// <typeparam name="TContext"><inheritdoc cref="ICondition{TContext}"/></typeparam>
public sealed class NotCondition<TContext>(ICondition<TContext> condition) : ICondition<TContext>
{
    /// <summary> The inverted condition. </summary>
    public ICondition<TContext> Condition = condition;

    /// <inheritdoc/>
    public bool Evaluate(in TContext context)
        => !Condition.Evaluate(context);

    /// <inheritdoc/>
    public ICondition<TContext> Reduce()
    {
        switch (Condition.Reduce())
        {
            case TrueCondition<TContext>:  return FalseCondition<TContext>.Instance;
            case FalseCondition<TContext>: return TrueCondition<TContext>.Instance;
            case NotCondition<TContext> n: return n.Condition;
            case { } d:                    Condition = d; break;
        }

        return this;
    }

    /// <inheritdoc/>
    public void WriteJson(Utf8JsonWriter j)
    {
        j.WriteStartObject();
        j.WriteString("Type"u8, "Not"u8);
        j.WritePropertyName("Condition"u8);
        Condition.WriteJson(j);
        j.WriteEndObject();
    }

    /// <inheritdoc/>
    public ICondition<TContext> DeepCopy()
        => new NotCondition<TContext>(Condition.DeepCopy());

    /// <inheritdoc/>
    public IEnumerable<ICondition<TContext>> Subconditions
        => Condition.Subconditions;

    /// <inheritdoc/>
    public int RemoveSubconditions(Func<ICondition<TContext>, bool> predicate)
        => 0;
}
