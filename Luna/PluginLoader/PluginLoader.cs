using Dalamud.Plugin;

namespace Luna;

/// <summary> The base plugin loader for a given definition. This represents the single entry point for a Dalamud plugin if implemented. </summary>
/// <typeparam name="TPlugin"> The plugin definition. </typeparam>
public abstract class PluginLoader<TPlugin> : IPluginLoader, IAsyncDalamudPlugin
    where TPlugin : class, IPluginDefinition<TPlugin>, IAsyncDisposable
{
    /// <summary> A global cancellation token for manual cancellation in addition to Dalamud's cancellation. </summary>
    private CancellationTokenSource? _globalCancellation;

    /// <summary> Whether the plugin was successfully and fully initialized. </summary>
    private bool _pluginInitialized;

    /// <inheritdoc cref="IPluginLoader.PluginInterface"/>
    public readonly IDalamudPluginInterface PluginInterface;

    /// <inheritdoc cref="IPluginLoader.Log"/>
    public readonly MainLogger Log;

    /// <inheritdoc cref="IPluginLoader.Services"/>
    public readonly ServiceManager Services;

    /// <inheritdoc cref="IPluginLoader.Source"/>
    public readonly PluginLoadSource Source;

    /// <inheritdoc cref="IPluginLoader.Version"/>
    public readonly Version Version;

    /// <inheritdoc cref="IPluginLoader.CommitHash"/>
    public readonly string CommitHash;

    /// <summary> The intermediary plugin during the validation and plugin launch steps, or when either of those fail. </summary>
    public IIntermediaryPlugin<TPlugin>? IntermediaryPlugin { get; private set; }

    /// <summary> The main plugin. </summary>
    public TPlugin? Plugin { get; private set; }

    /// <inheritdoc/>
    public bool HasIntermediaryPlugin
        => IntermediaryPlugin is not null;

    /// <inheritdoc/>
    public bool HasPlugin
        => _pluginInitialized;

    /// <inheritdoc/>
    public string PluginName
        => Log.PluginName;

    /// <summary> Initialize the plugin loader. </summary>
    /// <param name="pluginInterface"> The plugin interface passed by Dalamud. </param>
    /// <param name="pluginName"> The custom name of the plugin. If null, the internal name will be used. </param>
    protected PluginLoader(IDalamudPluginInterface pluginInterface, string? pluginName)
    {
        PluginInterface = pluginInterface;
        Log             = new MainLogger(pluginName ?? PluginInterface.InternalName);
        Source          = IPluginLoader.FetchSource(PluginInterface);
        Version         = IPluginLoader.FetchVersion<TPlugin>(PluginInterface, Log, Source);
        CommitHash      = IPluginLoader.FetchHash<TPlugin>();
        Services        = CreateManager(this, PluginInterface, Log, Source, Version);
    }

    /// <inheritdoc/>
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_globalCancellation is not null)
            {
                await _globalCancellation.CancelAsync().ConfigureAwait(false);
                _globalCancellation.Dispose();
            }

            _globalCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await InitializeIntermediaryAsync().ConfigureAwait(false);
        }
        catch
        {
            try
            {
                await DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed disposing plugin after unsuccessful load:\n{ex}");
            }

            throw;
        }
    }

    /// <summary> Create and initialize the intermediary plugin and anything that should happen before that. </summary>
    /// <returns> An awaiter. </returns>
    /// <remarks> This uses the global cancellation token and thus is not passed its own. </remarks>
    protected virtual async Task InitializeIntermediaryAsync()
    {
        var token = _globalCancellation?.Token ?? CancellationToken.None;
        try
        {
            await TPlugin.PreInitializeAsync(this, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error($"Plugin construction failed pre-initialization:\n{ex}");
            throw;
        }

        try
        {
            await TPlugin.InitializeLoggingAsync(this, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error($"Plugin construction failed before advanced logging could be setup:\n{ex}");
            throw;
        }

        try
        {
            IntermediaryPlugin = await TPlugin.LaunchIntermediaryAsync(this, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error($"Plugin construction failed while launching intermediary:\n{ex}");
            throw;
        }

        try
        {
            await IntermediaryPlugin!.Initialize(token).ConfigureAwait(false);
        }
        catch
        {
            if (IntermediaryPlugin is null)
                throw;

            try
            {
                await IntermediaryPlugin.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed disposing the plugin intermediary after unsuccessful initialization:\n{ex}");
            }
            finally
            {
                IntermediaryPlugin = null;
            }

            throw;
        }
    }

    /// <summary> Create and initialize the main plugin. </summary>
    /// <param name="cancel"> A cancellation token. </param>
    /// <returns> An awaiter. </returns>
    /// <remarks>
    ///   This is called immediately by the intermediary plugin initialization if the validation succeeds.
    ///   Otherwise, the intermediary plugin is responsible for calling it on user interaction.
    /// </remarks>
    public virtual async Task InitializePluginAsync(CancellationToken cancel)
    {
        var stage = PluginInitializationFailure.InitializationStage.Launch;
        try
        {
            Plugin = await TPlugin.LaunchPluginAsync(this, cancel).ConfigureAwait(false);
            stage  = PluginInitializationFailure.InitializationStage.BackupsGameData;
            var backupTask   = Plugin.CreateBackupsAsync(this, cancel);
            var gameDataTask = Plugin.LoadGameDataAsync(this, cancel);
            await Task.WhenAll(backupTask, gameDataTask).ConfigureAwait(false);
            stage = PluginInitializationFailure.InitializationStage.Configuration;
            await Plugin.LoadAndMigrateConfigurationAsync(this, cancel).ConfigureAwait(false);
            stage = PluginInitializationFailure.InitializationStage.PluginObjects;
            await Plugin.LoadPluginObjectsAsync(this, cancel).ConfigureAwait(false);
            stage = PluginInitializationFailure.InitializationStage.GameInterop;
            await Plugin.CreateGameInteropAsync(this, cancel).ConfigureAwait(false);
            stage = PluginInitializationFailure.InitializationStage.Ui;
            await Plugin.CreateUiAsync(this, cancel).ConfigureAwait(false);
            stage = PluginInitializationFailure.InitializationStage.Api;
            await Plugin.CreateApiAsync(this, cancel).ConfigureAwait(false);
            _pluginInitialized = true;

            if (IntermediaryPlugin is not null)
                try
                {
                    await IntermediaryPlugin.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed disposing the plugin intermediary:\n{ex}");
                }
                finally
                {
                    IntermediaryPlugin = null;
                }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error($"Plugin Initialization failed during {stage.String} stage:\n{ex}");
            var handling = PluginInitializationFailure.FailureHandling.Rethrow;
            try
            {
                if (IntermediaryPlugin is not null)
                    handling = await IntermediaryPlugin.OnPluginInitializationFailedAsync(new PluginInitializationFailure(stage, ex), cancel)
                        .ConfigureAwait(false);
            }
            finally
            {
                if (Plugin is not null)
                    try
                    {
                        await Plugin.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex2)
                    {
                        Log.Error($"Failed to dispose plugin after failure to initialize:\n{ex2}");
                    }
                    finally
                    {
                        Plugin = null;
                    }
            }

            if (handling is PluginInitializationFailure.FailureHandling.Rethrow)
                throw;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_globalCancellation is not null)
            {
                await _globalCancellation!.CancelAsync().ConfigureAwait(false);
                _globalCancellation.Dispose();
            }
        }
        catch
        {
            // ignored
        }
        finally
        {
            _globalCancellation = null;
        }

        if (Plugin is not null)
            try
            {
                await Plugin.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to dispose plugin:\n{ex}");
            }
            finally
            {
                Plugin             = null;
                _pluginInitialized = false;
            }


        if (IntermediaryPlugin is not null)
            try
            {
                await IntermediaryPlugin.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to dispose plugin intermediary:\n{ex}");
            }
            finally
            {
                IntermediaryPlugin = null;
            }

        try
        {
            Services.Dispose();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to dispose service manager:\n{ex}");
        }

        try
        {
            await DisposeAsync(true).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to dispose plugin loader:\n{ex}");
        }

        GC.SuppressFinalize(this);
    }

    /// <summary> Custom additional disposal if necessary. </summary>
    /// <param name="disposing"> Whether this is called from disposal. </param>
    /// <returns> An awaiter. </returns>
    protected virtual Task DisposeAsync(bool disposing)
        => Task.CompletedTask;

    /// <inheritdoc/>
    IDalamudPluginInterface IPluginLoader.PluginInterface
        => PluginInterface;

    /// <inheritdoc/>
    MainLogger IPluginLoader.Log
        => Log;

    /// <inheritdoc/>
    ServiceManager IPluginLoader.Services
        => Services;

    /// <inheritdoc/>
    PluginLoadSource IPluginLoader.Source
        => Source;

    /// <inheritdoc/>
    Version IPluginLoader.Version
        => Version;

    /// <inheritdoc/>
    string IPluginLoader.CommitHash
        => CommitHash;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ServiceManager CreateManager(IPluginLoader self, IDalamudPluginInterface pluginInterface, MainLogger log,
        PluginLoadSource source, Version version)
    {
        try
        {
            // Create the service manager as given by the plugin definition.
            // We require it to not have a provider since we add Self to the services and build them afterward.
            var ret = TPlugin.CreateServiceManager(pluginInterface, log);
            if (ret.Provider is not null)
                throw new ArgumentException(
                    $"The service manager created by {nameof(TPlugin.CreateServiceManager)} should not have built a provider.");

            ret.AddExistingService(self);
            ret.BuildProvider();
            log.Information($"Set up service manager for {log.PluginName} v{version}{source.ToName()}.");
            return ret;
        }
        catch (Exception ex)
        {
            log.Error($"Error setting up service manager for {log.PluginName} v{version}{source.ToName()}:\n{ex}");
            throw;
        }
    }
}
