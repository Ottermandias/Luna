using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
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

/// <summary> Specialized disposable Provider for functions. </summary>
public sealed class FuncProvider<TRet> : BaseFuncProvider
{
    /// <summary> Create a function provider. </summary>
    public FuncProvider(IDalamudPluginInterface pi, string label, Func<TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<TRet>(label)).RegisterFunc(func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <summary> Create a function provider that adds all its callers to the passed set of plugins. </summary>
    public FuncProvider(IDalamudPluginInterface pi, HashSet<CallerPlugin> plugins, string label, Func<TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<TRet>(label)).RegisterFunc(Func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }

        return;

        TRet Func()
        {
            var context = Provider!.GetContext();
            if (context?.SourcePlugin is { } plugin)
                plugins.Add(new CallerPlugin(plugin));
            else
                LogUnknownExecution(ImSharpConfiguration.Logger, label);
            return func();
        }
    }

    /// <summary> Create a function provider that passes the calling plugins internal name to its function. </summary>
    public FuncProvider(IDalamudPluginInterface pi, string label, Func<string, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<TRet>(label)).RegisterFunc(Func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }

        return;

        TRet Func()
        {
            return func(Provider!.GetContext()?.SourcePlugin is { } plugin ? plugin.InternalName : "Unknown");
        }
    }
}

/// <inheritdoc cref="FuncProvider{TRet}"/>
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

        TRet Func(T1 a)
        {
            var context = Provider!.GetContext();
            if (context?.SourcePlugin is { } plugin)
                plugins.Add(new CallerPlugin(plugin));
            else
                LogUnknownExecution(ImSharpConfiguration.Logger, label);
            return func(a);
        }
    }

    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,string,Func{string,TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, string label, Func<T1, string, TRet> func)
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

        TRet Func(T1 a)
        {
            return func(a, Provider!.GetContext()?.SourcePlugin is { } plugin ? plugin.InternalName : "Unknown");
        }
    }
}

/// <inheritdoc cref="FuncProvider{TRet}"/>
public sealed class FuncProvider<T1, T2, TRet> : BaseFuncProvider
{
    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,string,Func{TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, string label, Func<T1, T2, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, TRet>(label)).RegisterFunc(func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,HashSet{CallerPlugin},string,Func{TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, HashSet<CallerPlugin> plugins, string label, Func<T1, T2, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, TRet>(label)).RegisterFunc(Func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }

        return;

        TRet Func(T1 a, T2 b)
        {
            var context = Provider!.GetContext();
            if (context?.SourcePlugin is { } plugin)
                plugins.Add(new CallerPlugin(plugin));
            else
                LogUnknownExecution(ImSharpConfiguration.Logger, label);
            return func(a, b);
        }
    }

    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,string,Func{string,TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, string label, Func<T1, T2, string, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, TRet>(label)).RegisterFunc(Func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }

        return;

        TRet Func(T1 a, T2 b)
        {
            return func(a, b, Provider!.GetContext()?.SourcePlugin is { } plugin ? plugin.InternalName : "Unknown");
        }
    }
}

/// <inheritdoc cref="FuncProvider{TRet}"/>
public sealed class FuncProvider<T1, T2, T3, TRet> : BaseFuncProvider
{
    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,string,Func{TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, string label, Func<T1, T2, T3, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, T3, TRet>(label)).RegisterFunc(func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,HashSet{CallerPlugin},string,Func{TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, HashSet<CallerPlugin> plugins, string label, Func<T1, T2, T3, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, T3, TRet>(label)).RegisterFunc(Func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }

        return;

        TRet Func(T1 a, T2 b, T3 c)
        {
            var context = Provider!.GetContext();
            if (context?.SourcePlugin is { } plugin)
                plugins.Add(new CallerPlugin(plugin));
            else
                LogUnknownExecution(ImSharpConfiguration.Logger, label);
            return func(a, b, c);
        }
    }

    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,string,Func{string,TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, string label, Func<T1, T2, T3, string, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, T3, TRet>(label)).RegisterFunc(Func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }

        return;

        TRet Func(T1 a, T2 b, T3 c)
        {
            return func(a, b, c, Provider!.GetContext()?.SourcePlugin is { } plugin ? plugin.InternalName : "Unknown");
        }
    }
}

/// <inheritdoc cref="FuncProvider{TRet}"/>
public sealed class FuncProvider<T1, T2, T3, T4, TRet> : BaseFuncProvider
{
    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,string,Func{TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, string label, Func<T1, T2, T3, T4, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, T3, T4, TRet>(label)).RegisterFunc(func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,HashSet{CallerPlugin},string,Func{TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, HashSet<CallerPlugin> plugins, string label, Func<T1, T2, T3, T4, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, T3, T4, TRet>(label)).RegisterFunc(Func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }

        return;

        TRet Func(T1 a, T2 b, T3 c, T4 d)
        {
            var context = Provider!.GetContext();
            if (context?.SourcePlugin is { } plugin)
                plugins.Add(new CallerPlugin(plugin));
            else
                LogUnknownExecution(ImSharpConfiguration.Logger, label);
            return func(a, b, c, d);
        }
    }

    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,string,Func{string,TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, string label, Func<T1, T2, T3, T4, string, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, T3, T4, TRet>(label)).RegisterFunc(Func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }

        return;

        TRet Func(T1 a, T2 b, T3 c, T4 d)
        {
            return func(a, b, c, d, Provider!.GetContext()?.SourcePlugin is { } plugin ? plugin.InternalName : "Unknown");
        }
    }
}

/// <inheritdoc cref="FuncProvider{TRet}"/>
public sealed class FuncProvider<T1, T2, T3, T4, T5, TRet> : BaseFuncProvider
{
    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,string,Func{TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, string label, Func<T1, T2, T3, T4, T5, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, T3, T4, T5, TRet>(label)).RegisterFunc(func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,HashSet{CallerPlugin},string,Func{TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, HashSet<CallerPlugin> plugins, string label, Func<T1, T2, T3, T4, T5, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, T3, T4, T5, TRet>(label)).RegisterFunc(Func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }

        return;

        TRet Func(T1 a, T2 b, T3 c, T4 d, T5 e)
        {
            var context = Provider!.GetContext();
            if (context?.SourcePlugin is { } plugin)
                plugins.Add(new CallerPlugin(plugin));
            else
                LogUnknownExecution(ImSharpConfiguration.Logger, label);
            return func(a, b, c, d, e);
        }
    }

    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,string,Func{string,TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, string label, Func<T1, T2, T3, T4, T5, string, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, T3, T4, T5, TRet>(label)).RegisterFunc(Func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }

        return;

        TRet Func(T1 a, T2 b, T3 c, T4 d, T5 e)
        {
            return func(a, b, c, d, e, Provider!.GetContext()?.SourcePlugin is { } plugin ? plugin.InternalName : "Unknown");
        }
    }
}

/// <inheritdoc cref="FuncProvider{TRet}"/>
public sealed class FuncProvider<T1, T2, T3, T4, T5, T6, TRet> : BaseFuncProvider
{
    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,string,Func{TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, string label, Func<T1, T2, T3, T4, T5, T6, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, T3, T4, T5, T6, TRet>(label)).RegisterFunc(func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,HashSet{CallerPlugin},string,Func{TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, HashSet<CallerPlugin> plugins, string label, Func<T1, T2, T3, T4, T5, T6, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, T3, T4, T5, T6, TRet>(label)).RegisterFunc(Func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }

        return;

        TRet Func(T1 a, T2 b, T3 c, T4 d, T5 e, T6 f)
        {
            var context = Provider!.GetContext();
            if (context?.SourcePlugin is { } plugin)
                plugins.Add(new CallerPlugin(plugin));
            else
                LogUnknownExecution(ImSharpConfiguration.Logger, label);
            return func(a, b, c, d, e, f);
        }
    }

    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,string,Func{string,TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, string label, Func<T1, T2, T3, T4, T5, T6, string, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, T3, T4, T5, T6, TRet>(label)).RegisterFunc(Func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }

        return;

        TRet Func(T1 a, T2 b, T3 c, T4 d, T5 e, T6 f)
        {
            return func(a, b, c, d, e, f, Provider!.GetContext()?.SourcePlugin is { } plugin ? plugin.InternalName : "Unknown");
        }
    }
}

/// <inheritdoc cref="FuncProvider{TRet}"/>
public sealed class FuncProvider<T1, T2, T3, T4, T5, T6, T7, TRet> : BaseFuncProvider
{
    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,string,Func{TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, string label, Func<T1, T2, T3, T4, T5, T6, T7, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, T3, T4, T5, T6, T7, TRet>(label)).RegisterFunc(func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,HashSet{CallerPlugin},string,Func{TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, HashSet<CallerPlugin> plugins, string label, Func<T1, T2, T3, T4, T5, T6, T7, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, T3, T4, T5, T6, T7, TRet>(label)).RegisterFunc(Func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }

        return;

        TRet Func(T1 a, T2 b, T3 c, T4 d, T5 e, T6 f, T7 g)
        {
            var context = Provider!.GetContext();
            if (context?.SourcePlugin is { } plugin)
                plugins.Add(new CallerPlugin(plugin));
            else
                LogUnknownExecution(ImSharpConfiguration.Logger, label);
            return func(a, b, c, d, e, f, g);
        }
    }

    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,string,Func{string,TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, string label, Func<T1, T2, T3, T4, T5, T6, T7, string, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, T3, T4, T5, T6, T7, TRet>(label)).RegisterFunc(Func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }

        return;

        TRet Func(T1 a, T2 b, T3 c, T4 d, T5 e, T6 f, T7 g)
        {
            return func(a, b, c, d, e, f, g, Provider!.GetContext()?.SourcePlugin is { } plugin ? plugin.InternalName : "Unknown");
        }
    }
}

/// <inheritdoc cref="FuncProvider{TRet}"/>
public sealed class FuncProvider<T1, T2, T3, T4, T5, T6, T7, T8, TRet> : BaseFuncProvider
{
    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,string,Func{TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, string label, Func<T1, T2, T3, T4, T5, T6, T7, T8, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(label)).RegisterFunc(func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }
    }

    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,HashSet{CallerPlugin},string,Func{TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, HashSet<CallerPlugin> plugins, string label,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(label)).RegisterFunc(Func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }

        return;

        TRet Func(T1 a, T2 b, T3 c, T4 d, T5 e, T6 f, T7 g, T8 h)
        {
            var context = Provider!.GetContext();
            if (context?.SourcePlugin is { } plugin)
                plugins.Add(new CallerPlugin(plugin));
            else
                LogUnknownExecution(ImSharpConfiguration.Logger, label);
            return func(a, b, c, d, e, f, g, h);
        }
    }

    /// <inheritdoc cref="FuncProvider{TRet}.FuncProvider(IDalamudPluginInterface,string,Func{string,TRet})"/>
    public FuncProvider(IDalamudPluginInterface pi, string label, Func<T1, T2, T3, T4, T5, T6, T7, T8, string, TRet> func)
    {
        try
        {
            SetProvider(pi.GetIpcProvider<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(label)).RegisterFunc(Func);
        }
        catch (Exception e)
        {
            LogError(ImSharpConfiguration.Logger, e, label);
        }

        return;

        TRet Func(T1 a, T2 b, T3 c, T4 d, T5 e, T6 f, T7 g, T8 h)
        {
            return func(a, b, c, d, e, f, g, h, Provider!.GetContext()?.SourcePlugin is { } plugin ? plugin.InternalName : "Unknown");
        }
    }
}
