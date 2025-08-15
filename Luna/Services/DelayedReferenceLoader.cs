namespace Luna;

/// <summary> A helper class to load object associations by their identifiers. </summary>
/// <typeparam name="TParent"> The type of the object that references child objects. </typeparam>
/// <typeparam name="TChild"> The type of the referenced object inside the parent object. </typeparam>
/// <typeparam name="TData"> The type of the stored data with which to identify the correct child. </typeparam>
/// <param name="messager"> The messager service to inform of missing children. </param>
public abstract class DelayedReferenceLoader<TParent, TChild, TData>(MessageService messager)
    where TParent : class
    where TChild : class
{
    /// <summary> The messager service to inform of missing children. </summary>
    protected readonly MessageService Messager = messager;

    /// <summary> The queue of parent objects with data to identify their children by. </summary>
    private readonly ConcurrentQueue<(TParent Parent, TData Data)> _data = [];

    /// <summary> Identify a child object by the given data. </summary>
    /// <param name="identity"> The data uniquely identifying a child object. </param>
    /// <param name="child"> The child to obtain from the data, if it exists. </param>
    /// <returns> True if a corresponding child was found, false otherwise. </returns>
    protected abstract bool TryGetObject(in TData identity, [NotNullWhen(true)] out TChild? child);

    /// <summary> Set the identified child value inside the parent object. </summary>
    /// <param name="parent"> The parent object. </param>
    /// <param name="child"> The identified child object. </param>
    /// <param name="data"> The data identifying the child object. </param>
    /// <param name="error"> An error occuring due to optional verification for valid children. </param>
    /// <returns> True if the child could be set, false otherwise. </returns>
    protected abstract bool SetObject(TParent parent, TChild child, in TData data, out string error);

    /// <summary> Try to set all queued child objects in their parents. </summary>
    public virtual void SetAllObjects()
    {
        while (_data.TryDequeue(out var tuple))
        {
            // Get the child object from the identifier.
            if (TryGetObject(tuple.Data, out var child))
            {
                // Validate and set the child.
                if (!SetObject(tuple.Parent, child, tuple.Data, out var error))
                    HandleChildNotSet(tuple.Parent, child, error);
            }
            else
            {
                HandleChildNotFound(tuple.Parent, tuple.Data);
            }
        }
    }

    /// <summary>
    ///   Add a parent object and the identifying data for a child to the queue.<br/>
    ///   If the child can already be identified, it is immediately set and not queued.
    /// </summary>
    /// <param name="parent"> The parent object. </param>
    /// <param name="data"> The identifying data for the child object. </param>
    public void AddObject(TParent parent, in TData data)
    {
        if (!TryGetObject(data, out var childObject) || !SetObject(parent, childObject, data, out _))
            _data.Enqueue((parent, data));
    }

    /// <summary> The method to handle when no child matches the identifying data during <see cref="SetAllObjects"/>. </summary>
    /// <param name="parent"> The parent object. </param>
    /// <param name="data"> The identifying data for the child object. </param>
    protected virtual void HandleChildNotFound(TParent parent, in TData data)
        => Messager.AddMessage(new Notification($"Could not find the object corresponding to the identifier {data} for {parent}."));

    /// <summary> The method to handle when the validation of the matching child fails during <see cref="SetAllObjects"/>. </summary>
    protected virtual void HandleChildNotSet(TParent parent, TChild child, string error)
        => Messager.AddMessage(new Notification($"Could not add the child {child} to {parent}: {error}"));
}
