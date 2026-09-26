using Dalamud.Plugin;

namespace Luna;

/// <summary> A stub loader that gets passed its already constructed plugin to pretend to be a finished loader. </summary>
/// <typeparam name="TPlugin"> The plugin definition. </typeparam>
/// <remarks> This does not take ownership of anything and is not disposable. </remarks>
public sealed class StubPluginLoader<TPlugin> : IPluginLoader
{
    /// <summary> Create a stub from an existing plugin definition. </summary>
    /// <param name="plugin"> The existing plugin. </param>
    /// <param name="pluginInterface"> The plugin interface passed by Dalamud. </param>
    /// <param name="log"> An existing logger. </param>
    /// <param name="services"> An existing and fully built service manager. </param>
    public StubPluginLoader(TPlugin plugin, IDalamudPluginInterface pluginInterface, MainLogger log, ServiceManager services)
    {
        Plugin          = plugin;
        PluginInterface = pluginInterface;
        Log             = log;
        Services        = services;
        Source          = IPluginLoader.FetchSource(pluginInterface);
        Version         = IPluginLoader.FetchVersion<TPlugin>(pluginInterface, log, Source);
        CommitHash      = IPluginLoader.FetchHash<TPlugin>();
    }

    /// <inheritdoc/>
    public IDalamudPluginInterface PluginInterface { get; }

    /// <inheritdoc/>
    public MainLogger Log { get; }

    /// <inheritdoc/>
    public ServiceManager Services { get; }

    /// <inheritdoc/>
    public PluginLoadSource Source { get; }

    /// <summary> The main plugin. </summary>
    public TPlugin Plugin { get; }

    /// <inheritdoc/>
    public string PluginName
        => Log.PluginName;

    /// <inheritdoc/>
    public Version Version { get; }

    /// <inheritdoc/>
    public string CommitHash { get; }

    /// <inheritdoc/>
    public bool HasIntermediaryPlugin
        => false;

    /// <inheritdoc/>
    public bool HasPlugin
        => true;
}
