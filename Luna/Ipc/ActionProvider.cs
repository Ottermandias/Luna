using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Luna.Generators;
using Microsoft.Extensions.Logging;

namespace Luna;

/// <summary> The base class for action providers. </summary>
public abstract partial class ActionProviderBase : IDisposable
{
    protected ICallGateProvider? Provider;

    /// <summary> Get whether the registration was successful. </summary>
    public bool IsRegistered
        => Provider is not null;

    /// <inheritdoc/>
    public void Dispose()
    {
        Provider?.UnregisterAction();
        Provider = null;
    }

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Error, "Error registering IPC Provider for {Label}")]
    protected static partial void LogError(ILogger logger, Exception ex, string label);

    /// <summary> Set the provider while returning it as its own type. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected T SetProvider<T>(T provider) where T : ICallGateProvider
    {
        Provider = provider;
        return provider;
    }
}

/// <summary> Specialized disposable Provider for Actions. </summary>
[GenerateArities(8, IncludeZeroArity = true)]
public sealed class ActionProvider<T1> : ActionProviderBase
{
    /// <summary> Create an action provider for a given action and with a given label. </summary>
    public ActionProvider(IDalamudPluginInterface pi, string label, Action<T1> action)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, object?>(label)).RegisterAction(action);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }
}
