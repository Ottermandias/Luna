using Dalamud.Plugin.Ipc;

namespace Luna;

/// <summary> A factory for adapters of specific type. </summary>
public interface IAdapterFactory
{
    /// <summary> The lifetime manager for IPC objects. </summary>
    public IpcObjectManager IpcManager { get; }

    /// <summary> Create a data share adapter. </summary>
    /// <param name="owner"> The requesting plugin. </param>
    /// <param name="data"> Arbitrary data for adapters that refer to specific objects instead of singletons. </param>
    /// <returns> The adapter. </returns>
    public IpcObjectManager.IBasicAdapter? CreateAdapter(string owner, object? data);
}

/// <summary> Extensions for <see cref="IAdapterFactory"/> implementations. </summary>
public static class IAdapterFactoryExtensions
{
    /// <summary> Create a data share adapter using the contained <see cref="IpcObjectManager"/> and store it within. </summary>
    /// <param name="factory"> The factory to create the adapter. </param>
    /// <param name="owner"> The requesting plugin. </param>
    /// <param name="data"> Arbitrary data for adapters that refer to specific objects instead of singletons. </param>
    /// <param name="callerName"> The name of the calling function. </param>
    /// <returns> The created adapter. </returns>
    public static IIdDataShareAdapter? Create(this IAdapterFactory factory, string owner, object? data = null,
        [CallerMemberName] string? callerName = null) => factory.IpcManager.Create(factory, owner, data, callerName);
}
