using System.Text.Json;

namespace Luna;

/// <summary> A condition that always evaluates to <c>true</c>. </summary>
/// <typeparam name="TContext"><inheritdoc cref="ICondition{TContext}"/></typeparam>
public sealed record TrueCondition<TContext> : ICondition<TContext>
{
    /// <summary> There only needs to be a single instance of this condition per context type. </summary>
    public static readonly TrueCondition<TContext> Instance = new();

    /// <inheritdoc/>
    public bool Evaluate(in TContext context)
        => true;

    /// <inheritdoc/>
    public ICondition<TContext> Reduce()
        => Instance;

    /// <inheritdoc/>
    public void WriteJson(Utf8JsonWriter j)
    {
        j.WriteStartObject();
        j.WriteString("Type"u8, "True"u8);
        j.WriteEndObject();
    }

    /// <inheritdoc/>
    public ICondition<TContext> DeepCopy()
        => Instance;

    /// <inheritdoc/>
    public IEnumerable<ICondition<TContext>> Subconditions
        => [];

    /// <inheritdoc/>
    public int RemoveSubconditions(Func<ICondition<TContext>, bool> predicate)
        => 0;

    /// <inheritdoc/>
    public bool Equals(ICondition<TContext>? other)
        => other is TrueCondition<TContext>;

    /// <inheritdoc/>
    public override int GetHashCode()
        => typeof(TrueCondition<TContext>).GetHashCode();
}
