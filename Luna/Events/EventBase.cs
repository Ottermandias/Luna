using Microsoft.Extensions.Logging;

namespace Luna;

/// <summary> The base class for a parameterless custom event type that supports subscriber priorities and per-delegate exception checking. </summary>
/// <typeparam name="TPriority"> The type of the priority to assign the event subscribers. </typeparam>
/// <param name="name"> The name of the event for logging. </param>
/// <param name="log"> The logger to use. </param>
/// <param name="comparer"> An optional comparer to compare priorities. Set to <see cref="Comparer{TPriority}"/> if null. </param>
public abstract class EventBase<TPriority>(string name, ILogger log, IComparer<TPriority>? comparer = null) : IDisposable, IService
    where TPriority : IComparable<TPriority>
{
    /// <summary> The name of the event. </summary>
    public readonly string Name = name;

    /// <summary> The logger the event uses. </summary>
    protected readonly ILogger Log = log;

    /// <summary> The list containing the delegates to invoke, ordered by their priority. </summary>
    protected readonly SortedListAdapter<(Action Subscriber, TPriority Priority)> Event = new([],
        new PriorityComparer(comparer ?? Comparer<TPriority>.Default));

    /// <summary> The lock to assure thread-safety. </summary>
    protected readonly ReaderWriterLockSlim Lock = new(LockRecursionPolicy.SupportsRecursion);

    /// <summary> Whether the event has any subscribers. </summary>
    public bool HasSubscribers
        => Event.Count > 0;

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary> Custom disposal function if necessary. </summary>
    /// <param name="disposing"> Whether the disposal is invoked by finalizer or manual call of Dispose. </param>
    protected virtual void Dispose(bool disposing)
    {
        Lock.EnterWriteLock();
        Event.Clear();
        Lock.ExitWriteLock();
        Lock.Dispose();
    }

    /// <summary> Add a new subscriber to the event. </summary>
    /// <param name="subscriber"> The subscriber to add. If a delegate comparing equal to this is already subscribed to the event, it will be moved according to the new priority, but not added in duplicate. </param>
    /// <param name="priority"> The priority used to order the subscribers of this event. </param>
    public virtual void Subscribe(Action subscriber, TPriority priority)
    {
        Lock.EnterWriteLock();
        try
        {
            var idx  = Event.List.IndexOf(p => p.Subscriber.Equals(subscriber));
            var pair = (subscriber, priority);
            if (idx >= 0)
            {
                if (Event.Comparer.Compare(Event[idx], pair) is 0)
                    return;

                Event.RemoveAt(idx);
            }

            Event.Add((subscriber, priority));
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }

    /// <summary> Remove an existing subscriber from the event. </summary>
    /// <param name="subscriber"> The subscriber to remove. The first delegate found that compares equal to this is removed. If none is found, nothing is done. </param>
    public virtual void Unsubscribe(Action subscriber)
    {
        Lock.EnterWriteLock();
        try
        {
            var idx = Event.List.IndexOf(p => p.Subscriber.Equals(subscriber));
            if (idx < 0)
                return;

            Event.RemoveAt(idx);
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }

    /// <summary> Enumerate the subscribers within a lock. </summary>
    protected virtual IEnumerable<Action> Enumerate()
    {
        // The lock is upgradeable so that a subscriber can remove itself from the list or add another subscriber.
        Lock.EnterUpgradeableReadLock();
        try
        {
            for (var i = Event.Count - 1; i >= 0; --i)
                yield return Event[i].Subscriber;
        }
        finally
        {
            Lock.ExitUpgradeableReadLock();
        }
    }

    /// <summary> Invoke the event for all subscribers in order of priority. </summary>
    /// <remarks> This will not throw, as any exceptions will be caught subscriber-specific. </remarks>
    public virtual void Invoke()
    {
        foreach (var action in Enumerate())
        {
            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "[{Event}] Exception thrown during invocation.", Name);
                throw;
            }
        }
    }

    /// <summary> Convert a priority-comparer to a pair-comparer. </summary>
    private readonly struct PriorityComparer(IComparer<TPriority> comparer) : IComparer<(Action, TPriority)>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare((Action, TPriority) x, (Action, TPriority) y)
            => comparer.Compare(x.Item2, y.Item2);
    }

    ~EventBase()
        => Dispose(false);
}

/// <summary> The base class for a custom event type that supports subscriber priorities and per-delegate exception checking. </summary>
/// <typeparam name="TArguments"> The type of the event arguments. </typeparam>
/// <typeparam name="TPriority"> The type of the priority to assign the event subscribers. </typeparam>
/// <param name="name"> The name of the event for logging. </param>
/// <param name="log"> The logger to use. </param>
/// <param name="comparer"> An optional comparer to compare priorities. Set to <see cref="Comparer{TPriority}"/> if null. </param>
public abstract class EventBase<TArguments, TPriority>(string name, ILogger log, IComparer<TPriority>? comparer = null) : IDisposable, IService
    where TArguments : allows ref struct
    where TPriority : IComparable<TPriority>
{
    /// <summary> The name of the event. </summary>
    public readonly string Name = name;

    /// <summary> The logger the event uses. </summary>
    protected readonly ILogger Log = log;

    /// <summary> The list containing the delegates to invoke, ordered by their priority. </summary>
    protected readonly SortedListAdapter<(Action<TArguments> Subscriber, TPriority Priority)> Event = new([],
        new PriorityComparer(comparer ?? Comparer<TPriority>.Default));

    /// <summary> The lock to assure thread-safety. </summary>
    protected readonly ReaderWriterLockSlim Lock = new(LockRecursionPolicy.SupportsRecursion);

    /// <summary> Whether the event has any subscribers. </summary>
    public bool HasSubscribers
        => Event.Count > 0;

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary> Custom disposal function if necessary. </summary>
    /// <param name="disposing"> Whether the disposal is invoked by finalizer or manual call of Dispose. </param>
    protected virtual void Dispose(bool disposing)
    {
        Lock.EnterWriteLock();
        Event.Clear();
        Lock.ExitWriteLock();
        Lock.Dispose();
    }

    /// <summary> Add a new subscriber to the event. </summary>
    /// <param name="subscriber"> The subscriber to add. If a delegate comparing equal to this is already subscribed to the event, it will be moved according to the new priority, but not added in duplicate. </param>
    /// <param name="priority"> The priority used to order the subscribers of this event. </param>
    public virtual void Subscribe(Action<TArguments> subscriber, TPriority priority)
    {
        Lock.EnterWriteLock();
        try
        {
            var idx  = Event.List.IndexOf(p => p.Subscriber.Equals(subscriber));
            var pair = (subscriber, priority);
            if (idx >= 0)
            {
                if (Event.Comparer.Compare(Event[idx], pair) is 0)
                    return;

                Event.RemoveAt(idx);
            }

            Event.Add((subscriber, priority));
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }

    /// <summary> Remove an existing subscriber from the event. </summary>
    /// <param name="subscriber"> The subscriber to remove. The first delegate found that compares equal to this is removed. If none is found, nothing is done. </param>
    public virtual void Unsubscribe(Action<TArguments> subscriber)
    {
        Lock.EnterWriteLock();
        try
        {
            var idx = Event.List.IndexOf(p => p.Subscriber.Equals(subscriber));
            if (idx < 0)
                return;

            Event.RemoveAt(idx);
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }

    /// <summary> Enumerate the subscribers within a lock. </summary>
    protected virtual IEnumerable<Action<TArguments>> Enumerate()
    {
        // The lock is upgradeable so that a subscriber can remove itself from the list or add another subscriber.
        Lock.EnterUpgradeableReadLock();
        try
        {
            for (var i = Event.Count - 1; i >= 0; --i)
                yield return Event[i].Subscriber;
        }
        finally
        {
            Lock.ExitUpgradeableReadLock();
        }
    }

    /// <summary> Invoke the event for all subscribers in order of priority. </summary>
    /// <param name="arguments"> The arguments to pass to the subscribers. </param>
    /// <remarks> This will not throw, as any exceptions will be caught subscriber-specific. </remarks>
    public virtual void Invoke(in TArguments arguments)
    {
        foreach (var action in Enumerate())
        {
            try
            {
                action.Invoke(arguments);
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "[{Event}] Exception thrown during invocation.", Name);
                throw;
            }
        }
    }

    /// <summary> Convert a priority-comparer to a pair-comparer. </summary>
    private readonly struct PriorityComparer(IComparer<TPriority> comparer) : IComparer<(Action<TArguments>, TPriority)>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare((Action<TArguments>, TPriority) x, (Action<TArguments>, TPriority) y)
            => comparer.Compare(x.Item2, y.Item2);
    }

    ~EventBase()
        => Dispose(false);
}
