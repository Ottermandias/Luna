using System.Text.Json;

namespace Luna;

/// <summary> The basis for a simple boolean condition system. </summary>
/// <typeparam name="TContext"> The context type passed to evaluate conditions. </typeparam>
public interface ICondition<TContext> : IEquatable<ICondition<TContext>>
{
    /// <summary> Evaluate this condition. </summary>
    /// <param name="context"> The context on which to base the evaluation. </param>
    /// <returns> True if the condition is fulfilled, false otherwise. </returns>
    public bool Evaluate(in TContext context);

    /// <summary> Reduce this condition to a simpler statement if possible. Should return itself if no reduction is possible. </summary>
    /// <returns> The simplified condition. </returns>
    public ICondition<TContext> Reduce();

    /// <summary> Write this condition to a JSON writer. Should begin and end its own object. </summary>
    /// <param name="j"> The JSON writer. </param>
    public void WriteJson(Utf8JsonWriter j);

    /// <summary> Create a deep copy of this condition. </summary>
    public ICondition<TContext> DeepCopy();

    /// <summary> Get all condition objects contained in this condition. </summary>
    public IEnumerable<ICondition<TContext>> Subconditions { get; }

    /// <summary> Remove all condition objects contained in this that match the predicate. </summary>
    /// <param name="predicate"> The predicate, any condition returning true shall be removed. </param>
    /// <returns> The number of removed subconditions in all descendant conditions of this. </returns>
    public int RemoveSubconditions(Func<ICondition<TContext>, bool> predicate);
}
