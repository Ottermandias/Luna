namespace Luna;

/// <summary> A general service interface to be collected by the <see cref="ServiceManager"/>. </summary>
[UsedImplicitly(ImplicitUseTargetFlags.Itself | ImplicitUseTargetFlags.WithInheritors)]
public interface IService;

/// <summary> A service type specific for precomputing data, generally from the game, that loads on startup and is shared via IPC, with some statistics. </summary>
public interface IDataContainer : IService
{
    /// <summary> The name of the service for sharing, logging and displaying statistics. </summary>
    public string Name { get; }

    /// <summary> The time the initial construction of the service took on the first plugin to initialize this container. </summary>
    public long Time { get; }

    /// <summary> The approximate memory used by this service. </summary>
    public long Memory { get; }

    /// <summary> The total count of data points in this container. </summary>
    public int TotalCount { get; }
}

/// <summary> An asynchronously initializing service. </summary>
public interface IAsyncService : IService
{
    /// <summary> An awaiter for this service that finishes when all initialization steps have finished. </summary>
    public Task Awaiter { get; }

    /// <summary> Whether this service is fully prepared, i.e. all initialization steps have finished. </summary>
    public bool Finished { get; }
}

/// <summary> A marker for required service singletons that get created by <see cref="ServiceManager.EnsureRequiredServices"/> regardless of being requested by any other objects. </summary>
public interface IRequiredService : IService;

/// <summary> An asynchronous required services. </summary>
public interface IAwaitedService : IRequiredService, IAsyncService;

/// <summary> A service type specifically for hooks that can be enabled and disabled. </summary>
public interface IHookService : IAwaitedService
{
    public nint Address { get; }
    public void Enable();
    public void Disable();
}

/// <summary> An asynchronous data container. </summary>
public interface IAsyncDataContainer : IDataContainer, IAsyncService;

/// <summary> A marker for API specific services. </summary>
public interface IApiService : IService;

/// <summary> A marker for UI specific services. </summary>
public interface IUiService : IService;


