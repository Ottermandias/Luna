using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;
using Luna.Generators;
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
[GenerateArities(8, IncludeZeroArity = true)]
public class FuncSubscriber<T1, TRet> : BaseFuncSubscriber
{
    /// <summary> Create a function subscriber with a given label. </summary>
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

    /// <summary> Invoke the function. See the source of the subscriber for details.</summary>
    /// <remarks> If the IPC method is not available, this will throw <see cref="IpcNotReadyError"/>. </remarks>
    protected TRet Invoke(in T1 a1)
        => GetSubscriber<ICallGateSubscriber<T1, TRet>>().InvokeFunc(a1);
}
