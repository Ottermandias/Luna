using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Microsoft.Extensions.Logging;

namespace Luna;

/// <summary> A manager for data share adapters used for IPC communication across plugins. </summary>
public sealed partial class IpcObjectManager : IDisposable, IApiService
{
    private readonly IDalamudPluginInterface              _pluginInterface;
    private readonly LunaLogger                           _log;
    private readonly SetDictionary<string, IBasicAdapter> _objects = [];
    private          bool                                 _disposed;

    /// <summary> A manager for data share adapters used for IPC communication across plugins. </summary>
    /// <param name="log"> A logger. </param>
    /// <param name="pluginInterface"> The plugin interface to listen for active plugin changes. </param>
    public IpcObjectManager(LunaLogger log, IDalamudPluginInterface pluginInterface)
    {
        _log                                  =  log;
        _pluginInterface                      =  pluginInterface;
        _pluginInterface.ActivePluginsChanged += OnActivePluginsChanged;
    }

    /// <summary> Create a new wrapped data share object with managed lifetime. </summary>
    /// <param name="factory"> A factory to create the adapter. </param>
    /// <param name="owner"> The requesting plugin. </param>
    /// <param name="data"> Arbitrary data for adapters that refer to specific objects instead of singletons. </param>
    /// <param name="callerName"> The name of the calling function. </param>
    /// <returns> The created adapter. </returns>
    /// <exception cref="ObjectDisposedException" />
    /// <remarks> Prefer to use <see cref="IAdapterFactoryExtensions.Create"/> on the <see cref="IAdapterFactory"/> directly. </remarks>
    public IIdDataShareAdapter? Create(IAdapterFactory factory, string owner, object? data = null, [CallerMemberName] string? callerName = null)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(IpcObjectManager));

        if (factory.CreateAdapter(owner, data) is not { } adapter)
            return null;

        lock (_objects)
        {
            _objects.TryAdd(owner, adapter);
        }

        LogCreation(_log, adapter.Type, adapter.Owner, callerName ?? "Unknown");
        return adapter;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Prevent the objects from logging and removing themselves from the set.
        _disposed                             =  true;
        _pluginInterface.ActivePluginsChanged -= OnActivePluginsChanged;

        // Dispose all remaining objects.
        lock (_objects)
        {
            foreach (var obj in _objects.Values)
                obj.Dispose();

            // Clear the set.
            _objects.Clear();
        }
    }

    private void OnActivePluginsChanged(IActivePluginsChangedEventArgs args)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(IpcObjectManager));

        if (args.Kind is PluginListInvalidationKind.Loaded)
            return;


        lock (_objects)
        {
            _disposed = true;
            foreach (var internalName in args.AffectedInternalNames)
            {
                if (!_objects.Remove(internalName, out var objects))
                    continue;

                foreach (var @object in objects)
                {
                    @object.Dispose();
                    LogStaleRemoval(_log, @object.Type, @object.Owner);
                }
            }

            _disposed = false;
        }
    }

    public void DrawDebug()
    {
        using var id = Im.Id.Empty();
        lock (_objects)
        {
            foreach (var (owner, objects) in _objects.Grouped)
            {
                id.PushNext();
                using var tree = Im.Tree.Node(owner);
                if (tree)
                    foreach (var adapter in objects)
                    {
                        Im.Tree.Leaf(adapter.Type);
                        Im.Line.Same(300 * Im.Style.GlobalScale);
                        using (Im.Group())
                        {
                            foreach (var @event in adapter.EventSubscriptions)
                                Im.Text(@event);
                        }
                    }

                id.Pop();
            }
        }
    }


    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Trace, "Provided IPC wrapper {Type} for {Owner} from {Caller}.")]
    static partial void LogCreation(LunaLogger logger, string type, string owner, string caller);

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Trace, "Relinquished IPC wrapper {Type} for {Owner}.")]
    static partial void LogDisposal(LunaLogger logger, string type, string owner);

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Warning,
        "Removed stale IPC wrapper {Type} for {Owner} after it unloaded without relinquishing.")]
    static partial void LogStaleRemoval(LunaLogger logger, string type, string owner);
}
