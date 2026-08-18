using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Microsoft.Extensions.Logging;

namespace Luna;

/// <summary> The base class for action subscribers. </summary>
public abstract partial class BaseActionSubscriber
{
    /// <summary> The actual subscriber object, or null on registration failure. </summary>
    protected ICallGateSubscriber? Subscriber { get; init; }

    /// <summary> Whether the subscriber could successfully be created. </summary>
    public bool Valid
        => Subscriber is not null;

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Error, "Error registering IPC Subscriber for {Label}")]
    protected static partial void LogError(ILogger logger, Exception ex, string label);
}

/// <summary> Specialized subscriber only allowing to invoke actions. </summary>
public class ActionSubscriber : BaseActionSubscriber
{
    /// <summary> Create a subscriber with a given label. </summary>
    protected ActionSubscriber(IDalamudPluginInterface pi, string label)
    {
        try
        {
            Subscriber = pi.GetIpcSubscriber<object?>(label);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <summary> Invoke the action. See the source of the subscriber for details.</summary>
    /// <remarks> When the IPC action is not available, this does nothing. It does not throw. </remarks>
    protected void Invoke()
        => (Subscriber as ICallGateSubscriber<object?>)?.InvokeAction();
}

/// <inheritdoc cref="ActionSubscriber"/> 
public class ActionSubscriber<T1> : BaseActionSubscriber
{
    /// <inheritdoc cref="ActionSubscriber.ActionSubscriber(IDalamudPluginInterface,string)"/>
    protected ActionSubscriber(IDalamudPluginInterface pi, string label)
    {
        try
        {
            Subscriber = pi.GetIpcSubscriber<T1, object?>(label);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="ActionSubscriber.Invoke"/>
    protected void Invoke(in T1 a)
        => (Subscriber as ICallGateSubscriber<T1, object?>)?.InvokeAction(a);
}

/// <inheritdoc cref="ActionSubscriber"/> 
public class ActionSubscriber<T1, T2> : BaseActionSubscriber
{
    /// <inheritdoc cref="ActionSubscriber.ActionSubscriber(IDalamudPluginInterface,string)"/>
    protected ActionSubscriber(IDalamudPluginInterface pi, string label)
    {
        try
        {
            Subscriber = pi.GetIpcSubscriber<T1, T2, object?>(label);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="ActionSubscriber.Invoke"/>
    protected void Invoke(in T1 a, in T2 b)
        => (Subscriber as ICallGateSubscriber<T1, T2, object?>)?.InvokeAction(a, b);
}

/// <inheritdoc cref="ActionSubscriber"/> 
public class ActionSubscriber<T1, T2, T3> : BaseActionSubscriber
{
    /// <inheritdoc cref="ActionSubscriber.ActionSubscriber(IDalamudPluginInterface,string)"/>
    protected ActionSubscriber(IDalamudPluginInterface pi, string label)
    {
        try
        {
            Subscriber = pi.GetIpcSubscriber<T1, T2, T3, object?>(label);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="ActionSubscriber.Invoke"/>
    protected void Invoke(in T1 a, in T2 b, in T3 c)
        => (Subscriber as ICallGateSubscriber<T1, T2, T3, object?>)?.InvokeAction(a, b, c);
}

/// <inheritdoc cref="ActionSubscriber"/> 
public class ActionSubscriber<T1, T2, T3, T4> : BaseActionSubscriber
{
    /// <inheritdoc cref="ActionSubscriber.ActionSubscriber(IDalamudPluginInterface,string)"/>
    protected ActionSubscriber(IDalamudPluginInterface pi, string label)
    {
        try
        {
            Subscriber = pi.GetIpcSubscriber<T1, T2, T3, T4, object?>(label);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="ActionSubscriber.Invoke"/>
    protected void Invoke(in T1 a, in T2 b, in T3 c, in T4 d)
        => (Subscriber as ICallGateSubscriber<T1, T2, T3, T4, object?>)?.InvokeAction(a, b, c, d);
}

/// <inheritdoc cref="ActionSubscriber"/> 
public class ActionSubscriber<T1, T2, T3, T4, T5> : BaseActionSubscriber
{
    /// <inheritdoc cref="ActionSubscriber.ActionSubscriber(IDalamudPluginInterface,string)"/>
    protected ActionSubscriber(IDalamudPluginInterface pi, string label)
    {
        try
        {
            Subscriber = pi.GetIpcSubscriber<T1, T2, T3, T4, T5, object?>(label);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="ActionSubscriber.Invoke"/>
    protected void Invoke(in T1 a, in T2 b, in T3 c, in T4 d, in T5 e)
        => (Subscriber as ICallGateSubscriber<T1, T2, T3, T4, T5, object?>)?.InvokeAction(a, b, c, d, e);
}

/// <inheritdoc cref="ActionSubscriber"/> 
public class ActionSubscriber<T1, T2, T3, T4, T5, T6> : BaseActionSubscriber
{
    /// <inheritdoc cref="ActionSubscriber.ActionSubscriber(IDalamudPluginInterface,string)"/>
    protected ActionSubscriber(IDalamudPluginInterface pi, string label)
    {
        try
        {
            Subscriber = pi.GetIpcSubscriber<T1, T2, T3, T4, T5, T6, object?>(label);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="ActionSubscriber.Invoke"/>
    protected void Invoke(in T1 a, in T2 b, in T3 c, in T4 d, in T5 e, in T6 f)
        => (Subscriber as ICallGateSubscriber<T1, T2, T3, T4, T5, T6, object?>)?.InvokeAction(a, b, c, d, e, f);
}

/// <inheritdoc cref="ActionSubscriber"/> 
public class ActionSubscriber<T1, T2, T3, T4, T5, T6, T7> : BaseActionSubscriber
{
    /// <inheritdoc cref="ActionSubscriber.ActionSubscriber(IDalamudPluginInterface,string)"/>
    protected ActionSubscriber(IDalamudPluginInterface pi, string label)
    {
        try
        {
            Subscriber = pi.GetIpcSubscriber<T1, T2, T3, T4, T5, T6, T7, object?>(label);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="ActionSubscriber.Invoke"/>
    protected void Invoke(in T1 a, in T2 b, in T3 c, in T4 d, in T5 e, in T6 f, in T7 g)
        => (Subscriber as ICallGateSubscriber<T1, T2, T3, T4, T5, T6, T7, object?>)?.InvokeAction(a, b, c, d, e, f, g);
}

/// <inheritdoc cref="ActionSubscriber"/> 
public class ActionSubscriber<T1, T2, T3, T4, T5, T6, T7, T8> : BaseActionSubscriber
{
    /// <inheritdoc cref="ActionSubscriber.ActionSubscriber(IDalamudPluginInterface,string)"/>
    protected ActionSubscriber(IDalamudPluginInterface pi, string label)
    {
        try
        {
            Subscriber = pi.GetIpcSubscriber<T1, T2, T3, T4, T5, T6, T7, T8, object?>(label);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="ActionSubscriber.Invoke"/>
    protected void Invoke(in T1 a, in T2 b, in T3 c, in T4 d, in T5 e, in T6 f, in T7 g, in T8 h)
        => (Subscriber as ICallGateSubscriber<T1, T2, T3, T4, T5, T6, T7, T8, object?>)?.InvokeAction(a, b, c, d, e, f, g, h);
}
