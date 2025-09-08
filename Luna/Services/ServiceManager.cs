using Dalamud.IoC;
using Dalamud.Plugin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Luna;

/// <summary> A service manager that handles some additional things like Dalamud services for Dependency Injection. </summary>
/// <remarks> Call <see cref="EnsureRequiredServices"/> when finished setting up to create the service provider. </remarks>
public class ServiceManager : IDisposable
{
    private readonly Logger               _logger;
    private readonly ServiceCollection    _collection   = [];
    private readonly HashSet<IDisposable> _ownedObjects = [];

    /// <summary> Keeps track of the time required to launch all services. </summary>
    public readonly StartTimeTracker Timers = new();

    /// <summary> The base service provider after setup. </summary>
    public ServiceProvider? Provider { get; private set; }

    /// <summary> Create a 
    /// 
    /// </summary>
    /// <param name="logger"></param>
    public ServiceManager(Logger logger)
    {
        _logger = logger;

        // Add logging services and self.
        _collection.AddSingleton(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));
        _collection.AddSingleton<ILogger>(_logger);
        _collection.AddSingleton(_logger);
        _collection.AddSingleton(this);
    }

    /// <summary> Get all services that implement a specific interface or class. </summary>
    /// <typeparam name="T"> The interface or class to implement. </typeparam>
    /// <returns> An enumeration of all services that can be assigned to objects of type <typeparamref name="T"/>. </returns>
    public IEnumerable<T> GetServicesImplementing<T>()
    {
        if (Provider is null)
            yield break;

        var type = typeof(T);
        foreach (var typeDescriptor in _collection)
        {
            if (typeDescriptor.Lifetime is ServiceLifetime.Singleton
             && typeDescriptor.ServiceType.IsAssignableTo(type))
                yield return (T)Provider.GetRequiredService(typeDescriptor.ServiceType);
        }
    }

    /// <summary> Get a service of specific type. </summary>
    /// <typeparam name="T"> The type of service to get. </typeparam>
    /// <returns> The service, if available. If not, it will throw. </returns>
    public T GetService<T>() where T : class
        => Provider!.GetRequiredService<T>();

    /// <summary> Create the provider and ensure that all services implementing <see cref="IRequiredService"/> that have been registered are created. </summary>
    public void EnsureRequiredServices()
    {
        BuildProvider();
        foreach (var service in _collection)
        {
            if (service.ServiceType.IsAssignableTo(typeof(IRequiredService)))
                Provider!.GetRequiredService(service.ServiceType);
        }
    }

    /// <summary> Create the provider. </summary>
    public void BuildProvider()
    {
        Provider ??= _collection.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes  = false,
        });
    }

    /// <summary> Add a specific type as a singleton to the collection. </summary>
    /// <typeparam name="T"> The type to add. </typeparam>
    /// <returns> This object to chain calls. </returns>
    /// <remarks> Singletons are objects that are only instantiated once. This needs to be called before <see cref="EnsureRequiredServices"/>. </remarks>
    public ServiceManager AddSingleton<T>()
        => AddSingleton(typeof(T));

    /// <summary> Add a specific type as a singleton to the collection but so that it is constructed via factory method. </summary>
    /// <param name="factory"> The function that gets invoked to create this type. </param>
    /// <typeparam name="T"> The type to add. </typeparam>
    /// <returns> This object to chain calls. </returns>
    /// <remarks> Singletons are objects that are only instantiated once. This needs to be called before <see cref="EnsureRequiredServices"/>. </remarks>
    public ServiceManager AddSingleton<T>(Func<IServiceProvider, T> factory) where T : class
    {
        _collection.AddSingleton(Func);
        return this;

        T Func(IServiceProvider p)
        {
            _logger.Verbose($"Constructing Service {typeof(T).Name} with custom factory function.");
            using var timer = Timers.Measure(typeof(T).Name);
            return factory(p);
        }
    }

    /// <summary> Add all services from an assembly implementing <see cref="IService"/> and that are neither interfaces nor abstract to the collection. </summary>
    /// <param name="assembly"> The assembly to fetch the services from. </param>
    public void AddIServices(Assembly assembly)
    {
        var iType = typeof(IService);
        foreach (var type in assembly.ExportedTypes.Where(t => t is { IsInterface: false, IsAbstract: false } && iType.IsAssignableFrom(t)))
        {
            if (_collection.All(t => t.ServiceType != type))
                AddSingleton(type);
        }
    }

    /// <summary> Add all services from an assembly implementing <see cref="TInterface"/> and that are neither interfaces nor abstract to the collection. </summary>
    /// <typeparam name="TInterface"> The interface to check for. </typeparam>
    /// <param name="assembly"> The assembly to fetch the services from. </param>
    public void AddIServices<TInterface>(Assembly assembly)
    {
        var iType = typeof(TInterface);
        foreach (var type in assembly.ExportedTypes.Where(t => t is { IsInterface: false, IsAbstract: false } && iType.IsAssignableFrom(t)))
        {
            if (_collection.All(t => t.ServiceType != type))
                AddSingleton(type);
        }
    }

    /// <summary> Add a Dalamud-provided service to the collection as a singleton. </summary>
    /// <typeparam name="T"> The Dalamud-provided service to add. </typeparam>
    /// <param name="pi"> The plugin interface to fetch the object from. </param>
    /// <returns> This object to chain calls. </returns>
    public ServiceManager AddDalamudService<T>(IDalamudPluginInterface pi) where T : class
    {
        var wrapper = new DalamudServiceWrapper<T>(pi);
        _collection.AddSingleton(wrapper.Service);
        _collection.AddSingleton(pi);
        if (wrapper.Service is IDisposable disposable)
            _ownedObjects.Add(disposable);
        if (pi is IDisposable disposableInterface)
            _ownedObjects.Add(disposableInterface);
        return this;
    }

    /// <summary> Add an existing object as a singleton service for a specific type. </summary>
    /// <typeparam name="T"> The type of the object. </typeparam>
    /// <param name="service"> The existing service. </param>
    /// <param name="takeOwnership"> If the object is an <see cref="IDisposable"/> and this is true, the service container will dispose it at the end of its lifetime. </param>
    /// <returns> This object to chain calls. </returns>
    public ServiceManager AddExistingService<T>(T service, bool takeOwnership = false) where T : class
    {
        _collection.AddSingleton(service);
        if (takeOwnership && service is IDisposable disposable)
            _ownedObjects.Add(disposable);
        return this;
    }

    /// <summary> Dispose all services created via this service provider or taken ownership of. </summary>
    public void Dispose()
    {
        _logger.Debug("Disposing all services.");
        Provider?.Dispose();
        foreach (var disposable in _ownedObjects)
            disposable.Dispose();
        _ownedObjects.Clear();
        _logger.Debug("Disposed all services.");
        GC.SuppressFinalize(this);
    }

    /// <summary> Wrapper for adding singletons with some custom logging and timing. </summary>
    private ServiceManager AddSingleton(Type type)
    {
        _collection.AddSingleton(type, Func);
        return this;

        object Func(IServiceProvider p)
        {
            var constructor = type.GetConstructors().MaxBy(c => c.GetParameters().Length);
            if (constructor == null)
                return Activator.CreateInstance(type) ?? throw new Exception($"No constructor available for {type.Name}.");

            var parameterTypes = constructor.GetParameters();
            var parameters     = parameterTypes.Select(t => p.GetRequiredService(t.ParameterType)).ToArray();
            _logger.Verbose(
                $"Constructing Service {type.Name} with {string.Join(", ", parameterTypes.Select(name => $"{name.ParameterType}"))}.");
            using var timer = Timers.Measure(type.Name);
            return constructor.Invoke(parameters);
        }
    }

    /// <summary> Wrapper for adding Dalamud-provided services. </summary>
    private class DalamudServiceWrapper<T>
    {
        [PluginService]
        public T Service { get; private set; } = default!;

        public DalamudServiceWrapper(IDalamudPluginInterface pi)
        {
            pi.Inject(this);
        }
    }
}
