using Dalamud.Plugin;

namespace Luna;

/// <summary> The definition for a main plugin as used by a <see cref="PluginLoader{TPlugin}"/>. </summary>
/// <typeparam name="TSelf"> The plugins own type. </typeparam>
public interface IPluginDefinition<TSelf> where TSelf : class, IPluginDefinition<TSelf>, IAsyncDisposable
{
    /// <summary> Prepare the service manager for this plugin. </summary>
    /// <param name="pluginInterface"> The plugin interface passed by Dalamud. </param>
    /// <param name="log"> The main log service created by the <see cref="PluginLoader{TPlugin}"/>. </param>
    /// <returns> A prepared service manager. </returns>
    /// <remarks> Note that the service manager should not have built a provider yet, otherwise this will throw. </remarks>
    public abstract static ServiceManager CreateServiceManager(IDalamudPluginInterface pluginInterface, MainLogger log);

    /// <summary> Any methods to invoke before the initialization of the intermediary starts. Note that this is called before <see cref="InitializeLoggingAsync"/>. </summary>
    /// <param name="loader"> The parent loader. </param>
    /// <param name="cancel"> A cancellation token. </param>
    /// <returns> An awaiter. </returns>
    public abstract static Task PreInitializeAsync(PluginLoader<TSelf> loader, CancellationToken cancel);

    /// <summary> Initialize any additional logging services, statics or globals. </summary>
    /// <param name="loader"> The parent loader. </param>
    /// <param name="cancel"> A cancellation token. </param>
    /// <returns> An awaiter. </returns>
    public abstract static Task InitializeLoggingAsync(PluginLoader<TSelf> loader, CancellationToken cancel);

    /// <summary> Launch, i.e. create, the <see cref="IIntermediaryPlugin{TPlugin}"/>. </summary>
    /// <param name="loader"> The parent loader. </param>
    /// <param name="cancel"> A cancellation token. </param>
    /// <returns> The created, but not initialized <see cref="IIntermediaryPlugin{TPlugin}"/>. </returns>
    /// <remarks> If there is no intermediary functionality planned, use <see cref="NopIntermediary{TPlugin}"/>. </remarks>
    public abstract static Task<IIntermediaryPlugin<TSelf>> LaunchIntermediaryAsync(PluginLoader<TSelf> loader, CancellationToken cancel);

    /// <summary> Launch, i.e. create, the main plugin. </summary>
    /// <param name="loader"> The parent loader. </param>
    /// <param name="cancel"> A cancellation token. </param>
    /// <returns> The created, but not initialized main plugin. </returns>
    public abstract static Task<TSelf> LaunchPluginAsync(PluginLoader<TSelf> loader, CancellationToken cancel);

    /// <summary> Preprocess and load any required game data. </summary>
    /// <param name="loader"> The parent loader. </param>
    /// <param name="cancel"> A cancellation token. </param>
    /// <returns> An awaiter. </returns>
    /// <remarks>
    ///   It is not necessary to follow the provided structure of methods, you can also let your service provider handle the ordering of services. <br/>
    ///   For the execution order of the provided structure, see <see cref="PluginInitializationFailure.InitializationStage"/>.
    /// </remarks>
    public Task LoadGameDataAsync(PluginLoader<TSelf> loader, CancellationToken cancel);

    /// <summary> Create backups of any stored data. </summary>
    /// <param name="loader"> The parent loader. </param>
    /// <param name="cancel"> A cancellation token. </param>
    /// <returns> An awaiter. </returns>
    /// <remarks>
    ///   It is not necessary to follow the provided structure of methods, you can also let your service provider handle the ordering of services. <br/>
    ///   For the execution order of the provided structure, see <see cref="PluginInitializationFailure.InitializationStage"/>.
    /// </remarks>
    public Task CreateBackupsAsync(PluginLoader<TSelf> loader, CancellationToken cancel);

    /// <summary> Handle configuration loading, parsing and migration. </summary>
    /// <param name="loader"> The parent loader. </param>
    /// <param name="cancel"> A cancellation token. </param>
    /// <returns> An awaiter. </returns>
    /// <remarks>
    ///   It is not necessary to follow the provided structure of methods, you can also let your service provider handle the ordering of services. <br/>
    ///   For the execution order of the provided structure, see <see cref="PluginInitializationFailure.InitializationStage"/>.
    /// </remarks>
    public Task LoadAndMigrateConfigurationAsync(PluginLoader<TSelf> loader, CancellationToken cancel);

    /// <summary> Load objects managed by the plugin. </summary>
    /// <param name="loader"> The parent loader. </param>
    /// <param name="cancel"> A cancellation token. </param>
    /// <returns> An awaiter. </returns>
    /// <remarks>
    ///   It is not necessary to follow the provided structure of methods, you can also let your service provider handle the ordering of services. <br/>
    ///   For the execution order of the provided structure, see <see cref="PluginInitializationFailure.InitializationStage"/>.
    /// </remarks>
    public Task LoadPluginObjectsAsync(PluginLoader<TSelf> loader, CancellationToken cancel);

    /// <summary> Create injection points, hooks and fetch game object addresses. </summary>
    /// <param name="loader"> The parent loader. </param>
    /// <param name="cancel"> A cancellation token. </param>
    /// <returns> An awaiter. </returns>
    /// <remarks>
    ///   It is not necessary to follow the provided structure of methods, you can also let your service provider handle the ordering of services. <br/>
    ///   For the execution order of the provided structure, see <see cref="PluginInitializationFailure.InitializationStage"/>.
    /// </remarks>
    public Task CreateGameInteropAsync(PluginLoader<TSelf> loader, CancellationToken cancel);

    /// <summary> Create the plugin UI. </summary>
    /// <param name="loader"> The parent loader. </param>
    /// <param name="cancel"> A cancellation token. </param>
    /// <returns> An awaiter. </returns>
    /// <remarks>
    ///   It is not necessary to follow the provided structure of methods, you can also let your service provider handle the ordering of services. <br/>
    ///   For the execution order of the provided structure, see <see cref="PluginInitializationFailure.InitializationStage"/>.
    /// </remarks>
    public Task CreateUiAsync(PluginLoader<TSelf> loader, CancellationToken cancel);

    /// <summary> Create the plugins API/IPC surface. </summary>
    /// <param name="loader"> The parent loader. </param>
    /// <param name="cancel"> A cancellation token. </param>
    /// <returns> An awaiter. </returns>
    /// <remarks>
    ///   It is not necessary to follow the provided structure of methods, you can also let your service provider handle the ordering of services. <br/>
    ///   For the execution order of the provided structure, see <see cref="PluginInitializationFailure.InitializationStage"/>.
    /// </remarks>
    public Task CreateApiAsync(PluginLoader<TSelf> loader, CancellationToken cancel);
}
