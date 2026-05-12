using System.Text.Json;

namespace Luna;

/// <summary> A condition that always evaluates to <c>false</c>. </summary>
/// <typeparam name="TContext"><inheritdoc cref="ICondition{TContext}"/></typeparam>
public sealed record FalseCondition<TContext> : ICondition<TContext>
{
    /// <summary> There only needs to be a single instance of this condition per context type. </summary>
    public static readonly FalseCondition<TContext> Instance = new();

    /// <inheritdoc/>
    public bool Evaluate(in TContext context)
        => false;

    /// <inheritdoc/>
    public ICondition<TContext> Reduce()
        => Instance;

    /// <inheritdoc/>
    public void WriteJson(Utf8JsonWriter j)
    {
        j.WriteStartObject();
        j.WriteString("Type"u8, "False"u8);
        j.WriteEndObject();
    }

    /// <inheritdoc/>
    public ICondition<TContext> DeepCopy()
        => Instance;
}
