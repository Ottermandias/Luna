using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Luna.Generators;
using Microsoft.Extensions.Logging;

namespace Luna;

/// <summary> The base class for function providers. </summary>
public abstract partial class BaseFuncProvider : IDisposable
{
    protected ICallGateProvider? Provider;

    /// <summary> Get whether the registration was successful. </summary>
    public bool IsRegistered
        => Provider is not null;

    /// <inheritdoc/>
    public void Dispose()
    {
        Provider?.UnregisterFunc();
        Provider = null;
        GC.SuppressFinalize(this);
    }

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Error, "Error registering IPC Provider for {Label}")]
    protected static partial void LogError(ILogger logger, Exception ex, string label);

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Error, "Unknown plugin executed {Label}")]
    protected static partial void LogUnknownExecution(ILogger logger, string label);


    /// <summary> Set the provider while returning it as its own type. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected T SetProvider<T>(T provider) where T : ICallGateProvider
    {
        Provider = provider;
        return provider;
    }
}

/// <inheritdoc cref="FuncProvider{TRet}"/>
[GenerateArities(8, IncludeZeroArity = true)]
public sealed class FuncProvider<T1, TRet> : BaseFuncProvider
{
    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,string,Func{TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, string label, Func<T1, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, TRet>(label)).RegisterFunc(func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,HashSet{CallerPlugin},string,Func{TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, HashSet<CallerPlugin> plugins, string label, Func<T1, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, TRet>(label)).RegisterFunc(Func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }

        return;

        TRet Func(T1 a1)
        {
            var context = Provider!.GetContext();
            if (context?.SourcePlugin is { } plugin)
                plugins.Add(CallerPlugin.FromPlugin(plugin));
            else
                LogUnknownExecution(ImSharpConfiguration.Logger, label);
            return func(a1);
        }
    }

    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,string,Func{CallerPlugin,TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, string label, Func<T1, CallerPlugin, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, TRet>(label)).RegisterFunc(Func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }

        return;

        TRet Func(T1 a1)
        {
            return func(a1, CallerPlugin.FromPlugin(Provider!.GetContext()?.SourcePlugin));
        }
    }
}
