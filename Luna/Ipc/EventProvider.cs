using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Microsoft.Extensions.Logging;

namespace Luna;

/// <summary> The base class for event providers. </summary>
public abstract partial class BaseEventProvider : IDisposable
{
    /// <summary> The actual provider or null on failure. </summary>
    protected ICallGateProvider? Provider;

    /// <summary> A method to unsubscribe the event. </summary>
    protected Delegate? Unsubscriber;

    /// <inheritdoc/>
    public void Dispose()
    {
        // Handle unsubscribers.
        switch (Unsubscriber)
        {
            case Action<Delegate> a:          UnsubscribeAction(a); break;
            case Action<BaseEventProvider> b: b(this); break;
        }

        Unsubscriber = null;
        Provider     = null;
    }

    /// <summary> Handle unsubscription if a <see cref="Action{Delegate}"/> was passed. </summary>
    protected abstract void UnsubscribeAction(Action<Delegate> unsubscriber);

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Error, "Error registering IPC Provider for {Label}")]
    protected static partial void LogRegisterError(ILogger logger, Exception ex, string label);

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Error, "Exception thrown on IPC event")]
    protected static partial void LogInvokeError(ILogger logger, Exception ex);
}

/// <summary>
///   Specialized disposable Provider for Events.<para />
///   Will execute the unsubscriber action on disposal if any is provided.<para />
///   Can only be invoked and disposed.
/// </summary>
public sealed class EventProvider : BaseEventProvider
{
    /// <summary> Invoke the event.</summary>
    public void Invoke()
    {
        try
        {
            (Provider as ICallGateProvider<object?>)?.SendMessage();
        }
        catch (Exception e)
        {
            LogInvokeError(ImSharpConfiguration.Logger, e);
        }
    }

    /// <summary> Create an event provider with specified add and delete actions. </summary>
    public EventProvider(IDalamudPluginInterface pi, string label, (Action<Action> Add, Action<Action> Del)? subscribe = null)
    {
        try
        {
            Provider     = pi.GetIpcProvider<object?>(label);
            Unsubscriber = subscribe?.Del;
            subscribe?.Add(Invoke);
        }
        catch (Exception e)
        {
            LogRegisterError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <summary> Create an event provider with specified add and delete actions. </summary>
    public EventProvider(IDalamudPluginInterface pi, string label, Action<EventProvider> add, Action<EventProvider> del)
    {
        try
        {
            Provider     = pi.GetIpcProvider<object?>(label);
            Unsubscriber = del;
            add(this);
        }
        catch (Exception e)
        {
            LogRegisterError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc/>
    protected override void UnsubscribeAction(Action<Delegate> unsubscriber)
        => unsubscriber(Invoke);
}

/// <inheritdoc cref="EventProvider"/>
public sealed class EventProvider<T1> : BaseEventProvider
{
    /// <inheritdoc cref="EventProvider.Invoke"/>
    public void Invoke(T1 a)
    {
        try
        {
            (Provider as ICallGateProvider<T1, object?>)?.SendMessage(a);
        }
        catch (Exception e)
        {
            LogInvokeError(ImSharpConfiguration.Logger, e);
        }
    }

    /// <inheritdoc cref="EventProvider.EventProvider(IDalamudPluginInterface,string,ValueTuple{Action{Action},Action{Action}}?)"/>
    public EventProvider(IDalamudPluginInterface pi, string label, (Action<Action<T1>> Add, Action<Action<T1>> Del)? subscribe = null)
    {
        try
        {
            Provider     = pi.GetIpcProvider<T1, object?>(label);
            Unsubscriber = subscribe?.Del;
            subscribe?.Add(Invoke);
        }
        catch (Exception e)
        {
            LogRegisterError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="EventProvider.EventProvider(IDalamudPluginInterface,string,Action{EventProvider},Action{EventProvider})"/>
    public EventProvider(IDalamudPluginInterface pi, string label, Action<EventProvider<T1>> add, Action<EventProvider<T1>> del)
    {
        try
        {
            Provider     = pi.GetIpcProvider<T1, object?>(label);
            Unsubscriber = del;
            add(this);
        }
        catch (Exception e)
        {
            LogRegisterError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc/>
    protected override void UnsubscribeAction(Action<Delegate> unsubscriber)
        => unsubscriber(Invoke);
}

/// <inheritdoc cref="EventProvider"/>
public sealed class EventProvider<T1, T2> : BaseEventProvider
{
    /// <inheritdoc cref="EventProvider.Invoke"/>
    public void Invoke(T1 a, T2 b)
    {
        try
        {
            (Provider as ICallGateProvider<T1, T2, object?>)?.SendMessage(a, b);
        }
        catch (Exception e)
        {
            LogInvokeError(ImSharpConfiguration.Logger, e);
        }
    }

    /// <inheritdoc cref="EventProvider.EventProvider(IDalamudPluginInterface,string,ValueTuple{Action{Action},Action{Action}}?)"/>
    public EventProvider(IDalamudPluginInterface pi, string label, (Action<Action<T1, T2>> Add, Action<Action<T1, T2>> Del)? subscribe = null)
    {
        try
        {
            Provider     = pi.GetIpcProvider<T1, T2, object?>(label);
            Unsubscriber = subscribe?.Del;
            subscribe?.Add(Invoke);
        }
        catch (Exception e)
        {
            LogRegisterError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="EventProvider.EventProvider(IDalamudPluginInterface,string,Action{EventProvider},Action{EventProvider})"/>
    public EventProvider(IDalamudPluginInterface pi, string label, Action<EventProvider<T1, T2>> add, Action<EventProvider<T1, T2>> del)
    {
        try
        {
            Provider     = pi.GetIpcProvider<T1, T2, object?>(label);
            Unsubscriber = del;
            add(this);
        }
        catch (Exception e)
        {
            LogRegisterError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc/>
    protected override void UnsubscribeAction(Action<Delegate> unsubscriber)
        => unsubscriber(Invoke);
}

/// <inheritdoc cref="EventProvider"/>
public sealed class EventProvider<T1, T2, T3> : BaseEventProvider
{
    /// <inheritdoc cref="EventProvider.Invoke"/>
    public void Invoke(T1 a, T2 b, T3 c)
    {
        try
        {
            (Provider as ICallGateProvider<T1, T2, T3, object?>)?.SendMessage(a, b, c);
        }
        catch (Exception e)
        {
            LogInvokeError(ImSharpConfiguration.Logger, e);
        }
    }

    /// <inheritdoc cref="EventProvider.EventProvider(IDalamudPluginInterface,string,ValueTuple{Action{Action},Action{Action}}?)"/>
    public EventProvider(IDalamudPluginInterface pi, string label,
        (Action<Action<T1, T2, T3>> Add, Action<Action<T1, T2, T3>> Del)? subscribe = null)
    {
        try
        {
            Provider     = pi.GetIpcProvider<T1, T2, T3, object?>(label);
            Unsubscriber = subscribe?.Del;
            subscribe?.Add(Invoke);
        }
        catch (Exception e)
        {
            LogRegisterError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="EventProvider.EventProvider(IDalamudPluginInterface,string,Action{EventProvider},Action{EventProvider})"/>
    public EventProvider(IDalamudPluginInterface pi, string label, Action<EventProvider<T1, T2, T3>> add, Action<EventProvider<T1, T2, T3>> del)
    {
        try
        {
            Provider     = pi.GetIpcProvider<T1, T2, T3, object?>(label);
            Unsubscriber = del;
            add(this);
        }
        catch (Exception e)
        {
            LogRegisterError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc/>
    protected override void UnsubscribeAction(Action<Delegate> unsubscriber)
        => unsubscriber(Invoke);
}

/// <inheritdoc cref="EventProvider"/>
public sealed class EventProvider<T1, T2, T3, T4> : BaseEventProvider
{
    /// <inheritdoc cref="EventProvider.Invoke"/>
    public void Invoke(T1 a, T2 b, T3 c, T4 d)
    {
        try
        {
            (Provider as ICallGateProvider<T1, T2, T3, T4, object?>)?.SendMessage(a, b, c, d);
        }
        catch (Exception e)
        {
            LogInvokeError(ImSharpConfiguration.Logger, e);
        }
    }

    /// <inheritdoc cref="EventProvider.EventProvider(IDalamudPluginInterface,string,ValueTuple{Action{Action},Action{Action}}?)"/>
    public EventProvider(IDalamudPluginInterface pi, string label,
        (Action<Action<T1, T2, T3, T4>> Add, Action<Action<T1, T2, T3, T4>> Del)? subscribe = null)
    {
        try
        {
            Provider     = pi.GetIpcProvider<T1, T2, T3, T4, object?>(label);
            Unsubscriber = subscribe?.Del;
            subscribe?.Add(Invoke);
        }
        catch (Exception e)
        {
            LogRegisterError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="EventProvider.EventProvider(IDalamudPluginInterface,string,Action{EventProvider},Action{EventProvider})"/>
    public EventProvider(IDalamudPluginInterface pi, string label, Action<EventProvider<T1, T2, T3, T4>> add,
        Action<EventProvider<T1, T2, T3, T4>> del)
    {
        try
        {
            Provider     = pi.GetIpcProvider<T1, T2, T3, T4, object?>(label);
            Unsubscriber = del;
            add(this);
        }
        catch (Exception e)
        {
            LogRegisterError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc/>
    protected override void UnsubscribeAction(Action<Delegate> unsubscriber)
        => unsubscriber(Invoke);
}

/// <inheritdoc cref="EventProvider"/>
public sealed class EventProvider<T1, T2, T3, T4, T5> : BaseEventProvider
{
    /// <inheritdoc cref="EventProvider.Invoke"/>
    public void Invoke(T1 a, T2 b, T3 c, T4 d, T5 e)
    {
        try
        {
            (Provider as ICallGateProvider<T1, T2, T3, T4, T5, object?>)?.SendMessage(a, b, c, d, e);
        }
        catch (Exception ex)
        {
            LogInvokeError(ImSharpConfiguration.Logger, ex);
        }
    }

    /// <inheritdoc cref="EventProvider.EventProvider(IDalamudPluginInterface,string,ValueTuple{Action{Action},Action{Action}}?)"/>
    public EventProvider(IDalamudPluginInterface pi, string label,
        (Action<Action<T1, T2, T3, T4, T5>> Add, Action<Action<T1, T2, T3, T4, T5>> Del)? subscribe = null)
    {
        try
        {
            Provider     = pi.GetIpcProvider<T1, T2, T3, T4, T5, object?>(label);
            Unsubscriber = subscribe?.Del;
            subscribe?.Add(Invoke);
        }
        catch (Exception e)
        {
            LogRegisterError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="EventProvider.EventProvider(IDalamudPluginInterface,string,Action{EventProvider},Action{EventProvider})"/>
    public EventProvider(IDalamudPluginInterface pi, string label, Action<EventProvider<T1, T2, T3, T4, T5>> add,
        Action<EventProvider<T1, T2, T3, T4, T5>> del)
    {
        try
        {
            Provider     = pi.GetIpcProvider<T1, T2, T3, T4, T5, object?>(label);
            Unsubscriber = del;
            add(this);
        }
        catch (Exception e)
        {
            LogRegisterError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc/>
    protected override void UnsubscribeAction(Action<Delegate> unsubscriber)
        => unsubscriber(Invoke);
}

/// <inheritdoc cref="EventProvider"/>
public sealed class EventProvider<T1, T2, T3, T4, T5, T6> : BaseEventProvider
{
    /// <inheritdoc cref="EventProvider.Invoke"/>
    public void Invoke(T1 a, T2 b, T3 c, T4 d, T5 e, T6 f)
    {
        try
        {
            (Provider as ICallGateProvider<T1, T2, T3, T4, T5, T6, object?>)?.SendMessage(a, b, c, d, e, f);
        }
        catch (Exception ex)
        {
            LogInvokeError(ImSharpConfiguration.Logger, ex);
        }
    }

    /// <inheritdoc cref="EventProvider.EventProvider(IDalamudPluginInterface,string,ValueTuple{Action{Action},Action{Action}}?)"/>
    public EventProvider(IDalamudPluginInterface pi, string label,
        (Action<Action<T1, T2, T3, T4, T5, T6>> Add, Action<Action<T1, T2, T3, T4, T5, T6>> Del)? subscribe = null)
    {
        try
        {
            Provider     = pi.GetIpcProvider<T1, T2, T3, T4, T5, T6, object?>(label);
            Unsubscriber = subscribe?.Del;
            subscribe?.Add(Invoke);
        }
        catch (Exception e)
        {
            LogRegisterError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="EventProvider.EventProvider(IDalamudPluginInterface,string,Action{EventProvider},Action{EventProvider})"/>
    public EventProvider(IDalamudPluginInterface pi, string label, Action<EventProvider<T1, T2, T3, T4, T5, T6>> add,
        Action<EventProvider<T1, T2, T3, T4, T5, T6>> del)
    {
        try
        {
            Provider     = pi.GetIpcProvider<T1, T2, T3, T4, T5, T6, object?>(label);
            Unsubscriber = del;
            add(this);
        }
        catch (Exception e)
        {
            LogRegisterError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc/>
    protected override void UnsubscribeAction(Action<Delegate> unsubscriber)
        => unsubscriber(Invoke);
}

/// <inheritdoc cref="EventProvider"/>
public sealed class EventProvider<T1, T2, T3, T4, T5, T6, T7> : BaseEventProvider
{
    /// <inheritdoc cref="EventProvider.Invoke"/>
    public void Invoke(T1 a, T2 b, T3 c, T4 d, T5 e, T6 f, T7 g)
    {
        try
        {
            (Provider as ICallGateProvider<T1, T2, T3, T4, T5, T6, T7, object?>)?.SendMessage(a, b, c, d, e, f, g);
        }
        catch (Exception ex)
        {
            LogInvokeError(ImSharpConfiguration.Logger, ex);
        }
    }

    /// <inheritdoc cref="EventProvider.EventProvider(IDalamudPluginInterface,string,ValueTuple{Action{Action},Action{Action}}?)"/>
    public EventProvider(IDalamudPluginInterface pi, string label,
        (Action<Action<T1, T2, T3, T4, T5, T6, T7>> Add, Action<Action<T1, T2, T3, T4, T5, T6, T7>> Del)? subscribe = null)
    {
        try
        {
            Provider     = pi.GetIpcProvider<T1, T2, T3, T4, T5, T6, T7, object?>(label);
            Unsubscriber = subscribe?.Del;
            subscribe?.Add(Invoke);
        }
        catch (Exception e)
        {
            LogRegisterError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="EventProvider.EventProvider(IDalamudPluginInterface,string,Action{EventProvider},Action{EventProvider})"/>
    public EventProvider(IDalamudPluginInterface pi, string label, Action<EventProvider<T1, T2, T3, T4, T5, T6, T7>> add,
        Action<EventProvider<T1, T2, T3, T4, T5, T6, T7>> del)
    {
        try
        {
            Provider     = pi.GetIpcProvider<T1, T2, T3, T4, T5, T6, T7, object?>(label);
            Unsubscriber = del;
            add(this);
        }
        catch (Exception e)
        {
            LogRegisterError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc/>
    protected override void UnsubscribeAction(Action<Delegate> unsubscriber)
        => unsubscriber(Invoke);
}

/// <inheritdoc cref="EventProvider"/>
public sealed class EventProvider<T1, T2, T3, T4, T5, T6, T7, T8> : BaseEventProvider
{
    /// <inheritdoc cref="EventProvider.Invoke"/>
    public void Invoke(T1 a, T2 b, T3 c, T4 d, T5 e, T6 f, T7 g, T8 h)
    {
        try
        {
            (Provider as ICallGateProvider<T1, T2, T3, T4, T5, T6, T7, T8, object?>)?.SendMessage(a, b, c, d, e, f, g, h);
        }
        catch (Exception ex)
        {
            LogInvokeError(ImSharpConfiguration.Logger, ex);
        }
    }

    /// <inheritdoc cref="EventProvider.EventProvider(IDalamudPluginInterface,string,ValueTuple{Action{Action},Action{Action}}?)"/>
    public EventProvider(IDalamudPluginInterface pi, string label,
        (Action<Action<T1, T2, T3, T4, T5, T6, T7, T8>> Add, Action<Action<T1, T2, T3, T4, T5, T6, T7, T8>> Del)? subscribe = null)
    {
        try
        {
            Provider     = pi.GetIpcProvider<T1, T2, T3, T4, T5, T6, T7, T8, object?>(label);
            Unsubscriber = subscribe?.Del;
            subscribe?.Add(Invoke);
        }
        catch (Exception e)
        {
            LogRegisterError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="EventProvider.EventProvider(IDalamudPluginInterface,string,Action{EventProvider},Action{EventProvider})"/>
    public EventProvider(IDalamudPluginInterface pi, string label, Action<EventProvider<T1, T2, T3, T4, T5, T6, T7, T8>> add,
        Action<EventProvider<T1, T2, T3, T4, T5, T6, T7, T8>> del)
    {
        try
        {
            Provider     = pi.GetIpcProvider<T1, T2, T3, T4, T5, T6, T7, T8, object?>(label);
            Unsubscriber = del;
            add(this);
        }
        catch (Exception e)
        {
            LogRegisterError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc/>
    protected override void UnsubscribeAction(Action<Delegate> unsubscriber)
        => unsubscriber(Invoke);
}
