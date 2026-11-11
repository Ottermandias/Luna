using Microsoft.Extensions.Logging;

namespace Luna;

public class EventBase(string name, ILogger log)
{
    /// <summary> The name of the event. </summary>
    public readonly string Name = name;

    /// <summary> The logger the event uses. </summary>
    protected readonly ILogger Log = log;

    /// <summary> The lock to assure thread-safety. </summary>
    protected readonly Lock Lock = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected void Unsubscribe<T, TPriority>(ref (T Subscriber, TPriority Priority)[] subscribers, T subscriber)
        where T : Delegate
    {
        lock (Lock)
        {
            var old = subscribers;
            var idx = old.IndexOf(subscriber, static (p, s) => p.Subscriber == s);
            if (idx < 0)
                return;

            var updated = old.Without(idx);
            Volatile.Write(ref subscribers, updated);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected void Subscribe<T, TPriority>(ref (T Subscriber, TPriority Priority)[] subscribers, T subscriber, TPriority priority,
        IComparer<TPriority> comparer)
        where T : Delegate
    {
        lock (Lock)
        {
            var old     = subscribers;
            var updated = InsertSorted(old, subscriber, priority, comparer);
            Volatile.Write(ref subscribers, updated);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected void Clear<T>(ref T[] subscribers)
    {
        lock (Lock)
        {
            Volatile.Write(ref subscribers, []);
        }
    }

    private protected static (T Subscriber, TPriority Priority)[] InsertSorted<T, TPriority>((T Subscriber, TPriority Priority)[] old,
        T subscriber,
        TPriority priority, IComparer<TPriority> comparer) where T : Delegate
    {
        // Find an existing subscription.
        var existing = old.IndexOf(subscriber, static (v, in s) => v.Subscriber == s);

        // Nothing to change.
        if (existing >= 0 && comparer.Compare(old[existing].Priority, priority) is 0)
            return old;

        // Create the new array.
        var length  = old.Length + (existing < 0 ? 1 : 0);
        var updated = new (T Subscriber, TPriority Priority)[length];

        // Find insertion position, ignoring the existing subscription.
        var insert = 0;
        for (var i = 0; i < old.Length; ++i)
        {
            if (i == existing)
                continue;

            if (comparer.Compare(old[i].Priority, priority) < 0)
                break;

            ++insert;
        }

        // Copy entries into the new array, skipping the old subscription.
        if (existing < 0)
        {
            old.AsSpan(0, insert).CopyTo(updated);
            old.AsSpan(insert).CopyTo(updated.AsSpan(insert + 1));
        }
        else if (existing < insert)
        {
            old.AsSpan(0,            existing).CopyTo(updated);
            old.AsSpan(existing + 1, insert - existing).CopyTo(updated.AsSpan(existing));
            old.AsSpan(insert + 1).CopyTo(updated.AsSpan(insert + 1));
        }
        else
        {
            old.AsSpan(0,      insert).CopyTo(updated);
            old.AsSpan(insert, existing - insert).CopyTo(updated.AsSpan(insert + 1));
            old.AsSpan(existing + 1).CopyTo(updated.AsSpan(existing + 1));
        }

        updated[insert] = (subscriber, priority);
        return updated;
    }
}

/// <summary> The base class for a parameterless custom event type that supports subscriber priorities and per-delegate exception checking. </summary>
/// <typeparam name="TPriority"> The type of the priority to assign the event subscribers. </typeparam>
/// <param name="name"> The name of the event for logging. </param>
/// <param name="log"> The logger to use. </param>
/// <param name="comparer"> An optional comparer to compare priorities. Set to <see cref="Comparer{TPriority}"/> if null. </param>
public abstract class EventBase<TPriority>(string name, ILogger log, IComparer<TPriority>? comparer = null)
    : EventBase(name, log), IDisposable, IScopedService
{
    /// <summary> The comparer to use for the priority values. </summary>
    protected readonly IComparer<TPriority> Comparer = comparer ?? Comparer<TPriority>.Default;

    /// <summary> The list containing the delegates to invoke, ordered by their priority. </summary>
    private (Action Subscriber, TPriority Priority)[] _subscribers = [];

    /// <summary> Whether the event has any subscribers. </summary>
    public bool HasSubscribers
        => Volatile.Read(ref _subscribers).Length > 0;

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary> Custom disposal function if necessary. </summary>
    /// <param name="disposing"> Whether the disposal is invoked by finalizer or manual call of Dispose. </param>
    protected virtual void Dispose(bool disposing)
        => Clear(ref _subscribers);

    /// <summary> Add a new subscriber to the event. </summary>
    /// <param name="subscriber"> The subscriber to add. If a delegate comparing equal to this is already subscribed to the event, it will be moved according to the new priority, but not added in duplicate. </param>
    /// <param name="priority"> The priority used to order the subscribers of this event. </param>
    public virtual void Subscribe(Action subscriber, TPriority priority)
        => Subscribe(ref _subscribers, subscriber, priority, Comparer);

    /// <summary> Remove an existing subscriber from the event. </summary>
    /// <param name="subscriber"> The subscriber to remove. The first delegate found that compares equal to this is removed. If none is found, nothing is done. </param>
    public virtual void Unsubscribe(Action subscriber)
        => Unsubscribe(ref _subscribers, subscriber);

    /// <summary> Invoke the event for all subscribers in order of priority. </summary>
    /// <remarks> Exceptions thrown by subscribers are logged and do not prevent subsequent subscribers from being invoked. </remarks>
    public virtual void Invoke()
    {
        var delegates = Volatile.Read(ref _subscribers);
        foreach (var action in delegates)
        {
            try
            {
                action.Subscriber.Invoke();
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "[{Event:l}] Exception thrown during invocation.", Name);
            }
        }
    }
}

/// <summary> The base class for a custom event type that supports subscriber priorities and per-delegate exception checking. </summary>
/// <typeparam name="TArguments"> The type of the event arguments. </typeparam>
/// <typeparam name="TPriority"> The type of the priority to assign the event subscribers. </typeparam>
/// <param name="name"> The name of the event for logging. </param>
/// <param name="log"> The logger to use. </param>
/// <param name="comparer"> An optional comparer to compare priorities. Set to <see cref="Comparer{TPriority}"/> if null. </param>
public abstract class EventBase<TArguments, TPriority>(string name, ILogger log, IComparer<TPriority>? comparer = null)
    : EventBase(name, log), IDisposable, IScopedService
    where TArguments : allows ref struct
{
    /// <summary> The comparer to use for the priority values. </summary>
    protected readonly IComparer<TPriority> Comparer = comparer ?? Comparer<TPriority>.Default;

    /// <summary> The list containing the delegates to invoke, ordered by their priority. </summary>
    private (InAction<TArguments> Subscriber, TPriority Priority)[] _subscribers = [];

    /// <summary> Whether the event has any subscribers. </summary>
    public bool HasSubscribers
        => Volatile.Read(ref _subscribers).Length > 0;

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary> Custom disposal function if necessary. </summary>
    /// <param name="disposing"> Whether the disposal is invoked by finalizer or manual call of Dispose. </param>
    protected virtual void Dispose(bool disposing)
        => Clear(ref _subscribers);

    /// <summary> Add a new subscriber to the event. </summary>
    /// <param name="subscriber"> The subscriber to add. If a delegate comparing equal to this is already subscribed to the event, it will be moved according to the new priority, but not added in duplicate. </param>
    /// <param name="priority"> The priority used to order the subscribers of this event. </param>
    public virtual void Subscribe(InAction<TArguments> subscriber, TPriority priority)
        => Subscribe(ref _subscribers, subscriber, priority, Comparer);

    /// <summary> Remove an existing subscriber from the event. </summary>
    /// <param name="subscriber"> The subscriber to remove. The first delegate found that compares equal to this is removed. If none is found, nothing is done. </param>
    public virtual void Unsubscribe(InAction<TArguments> subscriber)
        => Unsubscribe(ref _subscribers, subscriber);

    /// <summary> Invoke the event for all subscribers in order of priority. </summary>
    /// <param name="arguments"> The arguments to pass to the subscribers. </param>
    /// <remarks> Exceptions thrown by subscribers are logged and do not prevent subsequent subscribers from being invoked. </remarks>
    public virtual void Invoke(in TArguments arguments)
    {
        var delegates = Volatile.Read(ref _subscribers);
        foreach (var action in delegates)
        {
            try
            {
                action.Subscriber.Invoke(arguments);
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "[{Event:l}] Exception thrown during invocation.", Name);
            }
        }
    }
}

/// <summary> The arguments passed to an event. </summary>
public delegate void InAction<TArguments>(in TArguments arguments) where TArguments : allows ref struct;
