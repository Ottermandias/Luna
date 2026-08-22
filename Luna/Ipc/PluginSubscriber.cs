using Dalamud.Plugin;

namespace Luna;

/// <summary> A basic subscription service that tracks plugin disposals and loads and checks for API versioning. </summary>
public abstract class PluginSubscriber : IDisposable
{
    /// <summary> The plugin interface. </summary>
    protected readonly IDalamudPluginInterface PluginInterface;

    /// <summary> The logger. </summary>
    protected readonly LunaLogger Log;

    /// <summary> The required major API version for this plugin to work. This must match exactly. </summary>
    public readonly int RequiredMajorVersion;

    /// <summary> The minimum required minor API version for this plugin to work. This must be less or equal than the actual minor version. </summary>
    public readonly int RequiredMinorVersion;

    /// <summary> The name of the plugin we are subscribing to for logging purposes. </summary>
    public readonly string PluginName;

    /// <summary> The initialization event to track. </summary>
    private readonly EventSubscriber _initializedEvent;

    /// <summary> The disposal event to track. </summary>
    private readonly EventSubscriber _disposedEvent;

    /// <summary> A wrapped disposal event that is invoked by this service within a try-catch block. </summary>
    public event Action? Disposed;

    /// <summary> A wrapped initialization event that is invoked by this service within a try-catch block. </summary>
    public event Action? Initialized;

    /// <summary> Whether IPC for the requested plugin is currently available. </summary>
    public bool Available { get; private set; }

    /// <summary> The actual major API version of the connected plugin. </summary>
    public int CurrentMajorVersion { get; private set; }

    /// <summary> The actual minor API version of the connected plugin. </summary>
    public int CurrentMinorVersion { get; private set; }

    /// <summary> The establishing time of the current connection. </summary>
    public DateTime AttachTime { get; private set; }

    /// <summary> Additional actions to take when a connection is established and the plugin fulfills the required version checks. </summary>
    protected abstract void PluginInitialized();

    /// <summary> Additional actions to take when a connection is broken by plugin disposal. </summary>
    protected abstract void PluginDisposed();

    /// <summary> A function to obtain the major and minor API versions of the connected plugin. </summary>
    protected abstract (int Major, int Minor) GetVersionInfo();

    /// <summary> Get whether the subscriber's version requirements are fulfilled. </summary>
    public bool MatchesVersions
        => CurrentMajorVersion == RequiredMajorVersion && CurrentMinorVersion >= RequiredMinorVersion;

    /// <summary> Create a new plugin subscriber. </summary>
    /// <param name="log"> The logger. </param>
    /// <param name="pluginInterface"> The plugin interface. </param>
    /// <param name="initialized"> A subscriber for the initialization event for the other plugin. This is taken ownership of. </param>
    /// <param name="disposed"> A subscriber for the disposal event for the other plugin. This is taken ownership of. </param>
    /// <param name="requiredMajor"> The required major API version of the other plugin. </param>
    /// <param name="requiredMinor"> The required minor API version of the other plugin. </param>
    /// <param name="pluginName"> The name of the other plugin for logging purposes. </param>
    public PluginSubscriber(LunaLogger log, IDalamudPluginInterface pluginInterface, EventSubscriber initialized, EventSubscriber disposed,
        int requiredMajor, int requiredMinor, string pluginName)
    {
        Log                  = log;
        PluginInterface      = pluginInterface;
        _initializedEvent    = initialized;
        _disposedEvent       = disposed;
        RequiredMajorVersion = requiredMajor;
        RequiredMinorVersion = requiredMinor;
        PluginName           = pluginName;

        _initializedEvent.Event += OnPluginLoad;
        _disposedEvent.Event    += OnPluginDispose;
        // ReSharper disable once VirtualMemberCallInConstructor
        Initialize();
        OnPluginLoad();
    }

    /// <summary> Recreate the plugin connection from scratch. </summary>
    public void Reattach()
        => OnPluginLoad();

    /// <summary> Terminate the current plugin connection. </summary>
    public void Detach()
        => OnPluginDispose();

    /// <summary> Initialize additional objects in the subscriber before subscription is triggered. </summary>
    protected virtual void Initialize()
    { }

    /// <inheritdoc/>
    public void Dispose()
    {
        OnPluginDispose();
        InternalDispose();
        _initializedEvent.Dispose();
        _disposedEvent.Dispose();
    }

    /// <summary> Dispose additional objects in the subscriber. </summary>
    protected virtual void InternalDispose()
    { }

    private void OnPluginDispose()
    {
        if (!Available)
            return;

        Available = false;
        try
        {
            PluginDisposed();
            Disposed?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Debug($"Error detaching from {PluginName}:\n{ex}");
        }
    }

    private void OnPluginLoad()
    {
        try
        {
            OnPluginDispose();
            QueryVersion();
            PluginInitialized();
            Available = true;
            Initialized?.Invoke();
            Log.Debug($"Attached to {PluginName} with IPC version {CurrentMajorVersion}.{CurrentMinorVersion}.");
        }
        catch (Exception e)
        {
            OnPluginDispose();
            Log.Debug($"Could not attach to {PluginName}:\n{e}");
        }
    }

    private void QueryVersion()
    {
        AttachTime = DateTime.UtcNow;
        try
        {
            (CurrentMajorVersion, CurrentMinorVersion) = GetVersionInfo();
        }
        catch
        {
            CurrentMajorVersion = 0;
            CurrentMinorVersion = 0;
            throw;
        }

        if (!MatchesVersions)
            throw new Exception(
                $"Invalid {PluginName} Version {CurrentMajorVersion}.{CurrentMinorVersion:D4}, required major Version {RequiredMajorVersion} with feature greater or equal to {RequiredMinorVersion}.");
    }
}
