using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Luna.Generators;
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
[GenerateArities(8, IncludeZeroArity = true)]
public class ActionSubscriber<T1> : BaseActionSubscriber
{
    /// <summary> Create a subscriber with a given label. </summary>
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

    /// <summary> Invoke the action. See the source of the subscriber for details.</summary>
    /// <remarks> When the IPC action is not available, this does nothing. It does not throw. </remarks>
    protected void Invoke(in T1 a1)
        => (Subscriber as ICallGateSubscriber<T1, object?>)?.InvokeAction(a1);
}
