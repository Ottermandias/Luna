using System.Text.Json;

namespace Luna;

/// <summary> A condition that inverts its subcondition. </summary>
/// <typeparam name="TContext"><inheritdoc cref="ICondition{TContext}"/></typeparam>
public sealed record NotCondition<TContext>(ICondition<TContext> Condition) : ICondition<TContext>
{
    /// <inheritdoc/>
    public bool Evaluate(in TContext context)
        => !Condition.Evaluate(context);

    /// <inheritdoc/>
    public ICondition<TContext> Reduce()
        => Condition.Reduce() switch
        {
            TrueCondition<TContext>  => FalseCondition<TContext>.Instance,
            FalseCondition<TContext> => TrueCondition<TContext>.Instance,
            { } d                    => new NotCondition<TContext>(d),
        };

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
