using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;
using Microsoft.Extensions.Logging;

namespace Luna;

/// <summary> A base class for function subscribers. </summary>
public abstract partial class BaseFuncSubscriber(string label)
{
    /// <summary> The label for the subscriber. </summary>
    public readonly string FunctionLabel = label;

    /// <summary> The subscriber on success, null if nothing provides the given function. </summary>
    public ICallGateSubscriber? Subscriber { get; protected init; }

    /// <summary> Whether the subscriber could successfully be created. </summary>
    public bool Valid
        => Subscriber is not null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected T GetSubscriber<T>() where T : class, ICallGateSubscriber
        => Subscriber as T ?? throw new IpcNotReadyError(FunctionLabel);

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Error, "Error registering IPC Provider for {Label}")]
    protected static partial void LogError(ILogger logger, Exception ex, string label);
}

/// <summary> Specialized subscriber only allowing to invoke functions with a return. </summary>
public class FuncSubscriber<TRet> : BaseFuncSubscriber
{
    /// <summary> Create a function subscriber with a given label. </summary>
    protected FuncSubscriber(IDalamudPluginInterface pi, string label)
        : base(label)
    {
        try
        {
            Subscriber = pi.GetIpcSubscriber<TRet>(label);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <summary> Invoke the function. See the source of the subscriber for details.</summary>
    /// <remarks> If the IPC method is not available, this will throw <see cref="IpcNotReadyError"/>. </remarks>
    protected TRet Invoke()
        => GetSubscriber<ICallGateSubscriber<TRet>>().InvokeFunc();
}

/// <inheritdoc cref="FuncSubscriber{TRet}"/>
public class FuncSubscriber<T1, TRet> : BaseFuncSubscriber
{
    /// <inheritdoc cref="FuncSubscriber{TRet}.FuncSubscriber(IDalamudPluginInterface,string)"/>
    protected FuncSubscriber(IDalamudPluginInterface pi, string label)
        : base(label)
    {
        try
        {
            Subscriber = pi.GetIpcSubscriber<T1, TRet>(label);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="FuncSubscriber{TRet}.Invoke"/>
    protected TRet Invoke(in T1 a)
        => GetSubscriber<ICallGateSubscriber<T1, TRet>>().InvokeFunc(a);
}

/// <inheritdoc cref="FuncSubscriber{TRet}"/>
public class FuncSubscriber<T1, T2, TRet> : BaseFuncSubscriber
{
    /// <inheritdoc cref="FuncSubscriber{TRet}.FuncSubscriber(IDalamudPluginInterface,string)"/>
    protected FuncSubscriber(IDalamudPluginInterface pi, string label)
        : base(label)
    {
        try
        {
            Subscriber = pi.GetIpcSubscriber<T1, T2, TRet>(label);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="FuncSubscriber{TRet}.Invoke"/>
    protected TRet Invoke(in T1 a, in T2 b)
        => GetSubscriber<ICallGateSubscriber<T1, T2, TRet>>().InvokeFunc(a, b);
}

/// <inheritdoc cref="FuncSubscriber{TRet}"/>
public class FuncSubscriber<T1, T2, T3, TRet> : BaseFuncSubscriber
{
    /// <inheritdoc cref="FuncSubscriber{TRet}.FuncSubscriber(IDalamudPluginInterface,string)"/>
    protected FuncSubscriber(IDalamudPluginInterface pi, string label)
        : base(label)
    {
        try
        {
            Subscriber = pi.GetIpcSubscriber<T1, T2, T3, TRet>(label);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="FuncSubscriber{TRet}.Invoke"/>
    protected TRet Invoke(in T1 a, in T2 b, in T3 c)
        => GetSubscriber<ICallGateSubscriber<T1, T2, T3, TRet>>().InvokeFunc(a, b, c);
}

/// <inheritdoc cref="FuncSubscriber{TRet}"/>
public class FuncSubscriber<T1, T2, T3, T4, TRet> : BaseFuncSubscriber
{
    /// <inheritdoc cref="FuncSubscriber{TRet}.FuncSubscriber(IDalamudPluginInterface,string)"/>
    protected FuncSubscriber(IDalamudPluginInterface pi, string label)
        : base(label)
    {
        try
        {
            Subscriber = pi.GetIpcSubscriber<T1, T2, T3, T4, TRet>(label);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="FuncSubscriber{TRet}.Invoke"/>
    protected TRet Invoke(in T1 a, in T2 b, in T3 c, in T4 d)
        => GetSubscriber<ICallGateSubscriber<T1, T2, T3, T4, TRet>>().InvokeFunc(a, b, c, d);
}

/// <inheritdoc cref="FuncSubscriber{TRet}"/>
public class FuncSubscriber<T1, T2, T3, T4, T5, TRet> : BaseFuncSubscriber
{
    /// <inheritdoc cref="FuncSubscriber{TRet}.FuncSubscriber(IDalamudPluginInterface,string)"/>
    protected FuncSubscriber(IDalamudPluginInterface pi, string label)
        : base(label)
    {
        try
        {
            Subscriber = pi.GetIpcSubscriber<T1, T2, T3, T4, T5, TRet>(label);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="FuncSubscriber{TRet}.Invoke"/>
    protected TRet Invoke(in T1 a, in T2 b, in T3 c, in T4 d, in T5 e)
        => GetSubscriber<ICallGateSubscriber<T1, T2, T3, T4, T5, TRet>>().InvokeFunc(a, b, c, d, e);
}

/// <inheritdoc cref="FuncSubscriber{TRet}"/>
public class FuncSubscriber<T1, T2, T3, T4, T5, T6, TRet> : BaseFuncSubscriber
{
    /// <inheritdoc cref="FuncSubscriber{TRet}.FuncSubscriber(IDalamudPluginInterface,string)"/>
    protected FuncSubscriber(IDalamudPluginInterface pi, string label)
        : base(label)
    {
        try
        {
            Subscriber = pi.GetIpcSubscriber<T1, T2, T3, T4, T5, T6, TRet>(label);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="FuncSubscriber{TRet}.Invoke"/>
    protected TRet Invoke(in T1 a, in T2 b, in T3 c, in T4 d, in T5 e, in T6 f)
        => GetSubscriber<ICallGateSubscriber<T1, T2, T3, T4, T5, T6, TRet>>().InvokeFunc(a, b, c, d, e, f);
}

/// <inheritdoc cref="FuncSubscriber{TRet}"/>
public class FuncSubscriber<T1, T2, T3, T4, T5, T6, T7, TRet> : BaseFuncSubscriber
{
    /// <inheritdoc cref="FuncSubscriber{TRet}.FuncSubscriber(IDalamudPluginInterface,string)"/>
    protected FuncSubscriber(IDalamudPluginInterface pi, string label)
        : base(label)
    {
        try
        {
            Subscriber = pi.GetIpcSubscriber<T1, T2, T3, T4, T5, T6, T7, TRet>(label);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="FuncSubscriber{TRet}.Invoke"/>
    protected TRet Invoke(in T1 a, in T2 b, in T3 c, in T4 d, in T5 e, in T6 f, in T7 g)
        => GetSubscriber<ICallGateSubscriber<T1, T2, T3, T4, T5, T6, T7, TRet>>().InvokeFunc(a, b, c, d, e, f, g);
}

/// <inheritdoc cref="FuncSubscriber{TRet}"/>
public class FuncSubscriber<T1, T2, T3, T4, T5, T6, T7, T8, TRet> : BaseFuncSubscriber
{
    /// <inheritdoc cref="FuncSubscriber{TRet}.FuncSubscriber(IDalamudPluginInterface,string)"/>
    protected FuncSubscriber(IDalamudPluginInterface pi, string label)
        : base(label)
    {
        try
        {
            Subscriber = pi.GetIpcSubscriber<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(label);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="FuncSubscriber{TRet}.Invoke"/>
    protected TRet Invoke(in T1 a, in T2 b, in T3 c, in T4 d, in T5 e, in T6 f, in T7 g, in T8 h)
        => GetSubscriber<ICallGateSubscriber<T1, T2, T3, T4, T5, T6, T7, T8, TRet>>().InvokeFunc(a, b, c, d, e, f, g, h);
}
