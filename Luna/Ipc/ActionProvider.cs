using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
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
public sealed class ActionProvider : ActionProviderBase
{
    /// <summary> Create an action provider for a given action and with a given label. </summary>
    public ActionProvider(IDalamudPluginInterface pi, string label, Action action)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<object?>(label)).RegisterAction(action);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }
}

/// <inheritdoc cref="ActionProvider"/>
public sealed class ActionProvider<T1> : ActionProviderBase
{
    /// <inheritdoc cref="ActionProvider.ActionProvider(IDalamudPluginInterface,string,Action)"/>
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

/// <inheritdoc cref="ActionProvider"/>
public sealed class ActionProvider<T1, T2> : ActionProviderBase
{
    /// <inheritdoc cref="ActionProvider.ActionProvider(IDalamudPluginInterface,string,Action)"/>
    public ActionProvider(IDalamudPluginInterface pi, string label, Action<T1, T2> action)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, object?>(label)).RegisterAction(action);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }
}

/// <inheritdoc cref="ActionProvider"/>
public sealed class ActionProvider<T1, T2, T3> : ActionProviderBase
{
    /// <inheritdoc cref="ActionProvider.ActionProvider(IDalamudPluginInterface,string,Action)"/>
    public ActionProvider(IDalamudPluginInterface pi, string label, Action<T1, T2, T3> action)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, T3, object?>(label)).RegisterAction(action);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }
}

/// <inheritdoc cref="ActionProvider"/>
public sealed class ActionProvider<T1, T2, T3, T4> : ActionProviderBase
{
    /// <inheritdoc cref="ActionProvider.ActionProvider(IDalamudPluginInterface,string,Action)"/>
    public ActionProvider(IDalamudPluginInterface pi, string label, Action<T1, T2, T3, T4> action)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, T3, T4, object?>(label)).RegisterAction(action);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }
}

/// <inheritdoc cref="ActionProvider"/>
public sealed class ActionProvider<T1, T2, T3, T4, T5> : ActionProviderBase
{
    /// <inheritdoc cref="ActionProvider.ActionProvider(IDalamudPluginInterface,string,Action)"/>
    public ActionProvider(IDalamudPluginInterface pi, string label, Action<T1, T2, T3, T4, T5> action)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, T3, T4, T5, object?>(label)).RegisterAction(action);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }
}

/// <inheritdoc cref="ActionProvider"/>
public sealed class ActionProvider<T1, T2, T3, T4, T5, T6> : ActionProviderBase
{
    /// <inheritdoc cref="ActionProvider.ActionProvider(IDalamudPluginInterface,string,Action)"/>
    public ActionProvider(IDalamudPluginInterface pi, string label, Action<T1, T2, T3, T4, T5, T6> action)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, T3, T4, T5, T6, object?>(label)).RegisterAction(action);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }
}

/// <inheritdoc cref="ActionProvider"/>
public sealed class ActionProvider<T1, T2, T3, T4, T5, T6, T7> : ActionProviderBase
{
    /// <inheritdoc cref="ActionProvider.ActionProvider(IDalamudPluginInterface,string,Action)"/>
    public ActionProvider(IDalamudPluginInterface pi, string label, Action<T1, T2, T3, T4, T5, T6, T7> action)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, T3, T4, T5, T6, T7, object?>(label)).RegisterAction(action);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }
}

/// <inheritdoc cref="ActionProvider"/>
public sealed class ActionProvider<T1, T2, T3, T4, T5, T6, T7, T8> : ActionProviderBase
{
    /// <inheritdoc cref="ActionProvider.ActionProvider(IDalamudPluginInterface,string,Action)"/>
    public ActionProvider(IDalamudPluginInterface pi, string label, Action<T1, T2, T3, T4, T5, T6, T7, T8> action)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, T3, T4, T5, T6, T7, T8, object?>(label)).RegisterAction(action);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }
}
