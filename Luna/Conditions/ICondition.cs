using System.Text.Json;

namespace Luna;

/// <summary> The basis for a simple boolean condition system. </summary>
/// <typeparam name="TContext"> The context type passed to evaluate conditions. </typeparam>
public interface ICondition<TContext> : IEquatable<ICondition<TContext>>
    where TContext : IConditionContext<TContext>
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

    /// <summary> Replace all condition objects contained in this by applying the result of the given method, if it is not null. Return the edited condition if anything changed. </summary>
    /// <param name="method"> The method to invoke on all conditions. It should return a non-null object if the condition is replaced or edited (it can return the same object passed to it with edits, too). </param>
    /// <returns> A new condition to replace this with in case of any changes, null if neither any sub conditions nor this condition itself were changed by <paramref name="method"/>. </returns>
    public ICondition<TContext>? EditConditions(Func<ICondition<TContext>, ICondition<TContext>?> method);
}

public interface IConditionContext<TSelf> where TSelf : IConditionContext<TSelf>
{
    /// <summary> Handle all custom condition types for this context. </summary>
    /// <param name="reader"> The reader, currently placed either at the start of an object or after the 'Type'-property, if it was the first in the object. </param>
    /// <param name="obj"> The object reader that can be used to stay within the current JSON object. </param>
    /// <param name="type"> The value of the 'Type'-property. </param>
    /// <returns> A non-null object when a custom condition could be parsed and created successfully, null otherwise. </returns>
    public abstract static ICondition<TSelf>? ParseCustomType(ref Utf8JsonReader reader, Utf8JsonObjectLimit obj, StringU8 type);
}
