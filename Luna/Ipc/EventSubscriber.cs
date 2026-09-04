using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Luna.Generators;
using Microsoft.Extensions.Logging;

namespace Luna;

/// <summary> The base class for event subscribers. </summary>
public abstract partial class BaseEventSubscriber : IDisposable
{
    /// <summary> The label of the event. </summary>
    public string Label { get; protected init; } = string.Empty;

    /// <summary> A dictionary mapping external to internal delegates. </summary>
    protected readonly Dictionary<Delegate, Delegate> Delegates = [];

    /// <summary> The actual subscriber. Only null after disposal. </summary>
    protected ICallGateSubscriber? Subscriber;

    /// <summary> Whether the event is currently enabled or disabled. </summary>
    public bool Disabled { get; protected set; }

    /// <summary>
    ///   Enable all currently subscribed actions registered with this EventSubscriber.
    ///   Does nothing if it is already enabled.
    /// </summary>
    public void Enable()
    {
        if (!Disabled)
            return;

        ObjectDisposedException.ThrowIf(Subscriber is null, this);
        foreach (var action in Delegates.Values)
            Subscribe(action);
        Disabled = false;
    }

    /// <summary>
    ///   Disable all subscribed actions registered with this EventSubscriber.
    ///   Does nothing if it is already disabled.
    ///   Does not forget the actions, only disables them.
    /// </summary>
    public void Disable()
    {
        if (Disabled)
            return;

        ObjectDisposedException.ThrowIf(Subscriber is null, this);
        foreach (var action in Delegates.Values)
            Unsubscribe(action);
        Disabled = true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Subscriber is null)
            return;

        Disable();
        Subscriber = null;
        Delegates.Clear();
    }

    /// <summary> Initialize the event subscriber with the requested subscriber and a list of delegates. </summary>
    protected void Init(ICallGateSubscriber subscriber, params IEnumerable<Delegate> delegates)
    {
        Subscriber = subscriber;
        Disabled   = false;
        foreach (var @delegate in delegates)
            AddDelegate(@delegate);
    }

    /// <summary> Subscribe an internal delegate to the event. Assumed to have the correct type. </summary>
    protected abstract void Subscribe(Delegate func);

    /// <summary> Unsubscribe an internal delegate from the event. Assumed to have the correct type. </summary>
    protected abstract void Unsubscribe(Delegate func);

    /// <summary> Create the internal delegate for the passed delegate. </summary>
    protected abstract Delegate CreateAction(Delegate parent);

    /// <summary> Convert the passed delegate to an internal one and add it to the event. </summary>
    protected void AddDelegate(Delegate parent)
    {
        ObjectDisposedException.ThrowIf(Subscriber is null, this);
        if (Delegates.ContainsKey(parent))
            return;

        var child = CreateAction(parent);
        if (Delegates.TryAdd(parent, child) && !Disabled)
            Subscribe(child);
    }

    /// <summary> Remove the matching internal delegate for the passed delegate from the event. </summary>
    protected void RemoveDelegate(Delegate parent)
    {
        ObjectDisposedException.ThrowIf(Subscriber is null, this);
        if (Delegates.Remove(parent, out var child))
            Unsubscribe(child);
    }

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Error, "Error registering IPC Provider for {Label}")]
    protected static partial void LogRegisterError(ILogger logger, Exception ex, string label);

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Error, "Exception thrown invoking IPC event {Label}")]
    protected static partial void LogInvokeError(ILogger logger, Exception ex, string label);
}

/// <summary>
///   Specialized disposable Subscriber for Events.<para />
///   Subscriptions are wrapped to be individually exception-safe.<para/>
///   Can be enabled and disabled.<para/>
/// </summary>
[GenerateArities(8, IncludeZeroArity = true)]
public sealed class EventSubscriber<T1> : BaseEventSubscriber
{
    /// <summary> Create an event subscriber with the given label and immediately subscribe the passed actions. </summary>
    public EventSubscriber(IDalamudPluginInterface pi, string label, params IEnumerable<Action<T1>> actions)
    {
        Label = label;
        try
        {
            Init(pi.GetIpcSubscriber<T1, object?>(label), actions);
        }
        catch (Exception e)
        {
            LogRegisterError(ImSharpConfiguration.Logger, e, Label);
        }
    }

    /// <summary> Add or remove an action to the IPC event, if it is valid. </summary>
    public event Action<T1> Event
    {
        add => AddDelegate(value);
        remove => RemoveDelegate(value);
    }

    /// <inheritdoc/>
    protected override void Subscribe(Delegate func)
        => (Subscriber as ICallGateSubscriber<T1, object?>)?.Subscribe((Action<T1>)func);

    /// <inheritdoc/>
    protected override void Unsubscribe(Delegate func)
        => (Subscriber as ICallGateSubscriber<T1, object?>)?.Unsubscribe((Action<T1>)func);

    /// <inheritdoc/>
    protected override Delegate CreateAction(Delegate parent)
    {
        return parent is Action<T1> action
            ? ChildAction
            : throw new ArgumentException($"Invalid action subscriber in event {Label}");

        void ChildAction(T1 a1)
        {
            try
            {
                action(a1);
            }
            catch (Exception e)
            {
                LogInvokeError(ImSharpConfiguration.Logger, e, Label);
            }
        }
    }
}
