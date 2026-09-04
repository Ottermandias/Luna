using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Luna.Generators;
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
[GenerateArities(8, IncludeZeroArity = true)]
public sealed class EventProvider<T1> : BaseEventProvider
{
    /// <summary> Invoke the event. </summary>
    public void Invoke(T1 a1)
    {
        try
        {
            (Provider as ICallGateProvider<T1, object?>)?.SendMessage(a1);
        }
        catch (Exception e)
        {
            LogInvokeError(ImSharpConfiguration.Logger, e);
        }
    }

    /// <summary> Create an event provider with specified add and delete actions. </summary>
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

    /// <summary> Create an event provider with specified add and delete actions. </summary>
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
