using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Luna.Generators;

namespace Luna;

/// <summary> A basic interface for wrappers that needs to be implemented in addition to <see cref="BasicWrapper{TSelf,TEnum}"/> for creation. </summary>
public interface IBasicWrapper<out TSelf> : IDisposable
    where TSelf : IBasicWrapper<TSelf>
{
    /// <summary> The referenced adapter. </summary>
    public IIdDataShareAdapter Adapter { get; }

    /// <summary> Create a new wrapper for the given adapter. </summary>
    /// <remarks> Used internally. </remarks>
    public abstract static TSelf? CreateWrapper(IIdDataShareAdapter? adapter);
}

/// <summary> A wrapper to keep the <see cref="IBasicWrapper{T}.CreateWrapper"/> method somewhat hidden from consumers. </summary>
public static class BasicWrapper
{
    /// <summary> The constant value to use for the version method in method enums. </summary>
    public const int VersionMethod = -1;

    /// <inheritdoc cref="IBasicWrapper{TWrapper}.CreateWrapper"/>
    public static TWrapper? Create<TWrapper>(IIdDataShareAdapter? adapter) where TWrapper : IBasicWrapper<TWrapper>
        => TWrapper.CreateWrapper(adapter);

    /// <summary> An empty adapter supplying no functionality. </summary>
    public static readonly IIdDataShareAdapter EmptyAdapter = new EmptyAdapterObject();

    /// <summary> Fetch an adapter from IPC. </summary>
    /// <param name="pluginInterface"> The plugin interface to use for IPC. </param>
    /// <param name="label"> The IPC label to query. </param>
    /// <returns> The adapter to connect with or null if none could be fetched. </returns>
    public static IIdDataShareAdapter? GetAdapter(IDalamudPluginInterface pluginInterface, string label)
    {
        try
        {
            var subscriber = pluginInterface.GetIpcSubscriber<IIdDataShareAdapter>(label);
            return subscriber.InvokeFunc();
        }
        catch
        {
            return null;
        }
    }

    private sealed class EmptyAdapterObject : IIdDataShareAdapter
    {
        public void Dispose()
        { }
    }
}

/// <summary> A base class for wrappers for a specific method ID enumeration type (assumed to be based on <see cref="int"/>). </summary>
/// <param name="adapter"> An initial adapter to connect to. </param>
/// <typeparam name="TSelf"> The own type for creation. </typeparam>
/// <typeparam name="TEnum">
///   The method enum. This should have a '<see cref="Version"/>' method with the value <see cref="BasicWrapper.VersionMethod"/>
///   or the wrapper should override the <see cref="Version"/> attribute with some other implementation.
/// </typeparam>
public abstract partial class BasicWrapper<TSelf, TEnum>(IIdDataShareAdapter? adapter = null) : IDisposable
    where TSelf : BasicWrapper<TSelf, TEnum>, IBasicWrapper<TSelf>
    where TEnum : unmanaged, Enum
{
    /// <summary> A delegate map for any events that need to be mapped. </summary>
    protected readonly ConcurrentDictionary<(Delegate Subscriber, TEnum Event), Delegate> DelegateMap = [];

    /// <inheritdoc cref="IBasicWrapper{TSelf}.Adapter"/>
    public IIdDataShareAdapter Adapter { get; private set; } = adapter ?? BasicWrapper.EmptyAdapter;

    /// <summary> Get whether the wrapper currently wraps an actual adapter. Note that this adapter may still be already disposed. </summary>
    public bool HasAdapter
        => Adapter != BasicWrapper.EmptyAdapter;

    /// <summary> Get the version of the referenced adapter. </summary>
    public virtual (int Major, int Minor) Version
        => Adapter.TryInvoke<(int Major, int Minor)>(BasicWrapper.VersionMethod, out var ret) ? ret : (0, 0);

    protected abstract string IpcLabel { get; }

    /// <summary> Connect the wrapper to an adapter newly requested via IPC. </summary>
    /// <param name="pluginInterface"> The plugin interface to use for IPC. </param>
    /// <param name="requiredMajorVersion"> The required major version of the adapter. Unchecked if null. </param>
    /// <param name="minimumMinorVersion"> The required minimum minor version of the adapter. This is only checked if a major version is passed. </param>
    /// <returns> False if no adapter could be obtained or the version requirements are not met, true otherwise. </returns>
    public bool Reconnect(IDalamudPluginInterface pluginInterface, int? requiredMajorVersion = null, int minimumMinorVersion = 0)
    {
        var adapter = BasicWrapper.GetAdapter(pluginInterface, IpcLabel);
        UpdateAdapter(adapter);
        if (!HasAdapter)
            return false;

        if (requiredMajorVersion is null)
            return true;

        var (major, minor) = Version;
        return major == requiredMajorVersion.Value && minor >= minimumMinorVersion;
    }

    /// <summary> Close the current connection. </summary>
    public void Disconnect()
        => UpdateAdapter(null);

    /// <summary> Set the wrapped adapter to a new one. </summary>
    /// <param name="adapter"> The new adapter to wrap. If this is null, an empty adapter will be wrapped which will throw on any invocation. </param>
    private unsafe void UpdateAdapter(IIdDataShareAdapter? adapter)
    {
        // If we get the same adapter, do nothing.
        adapter ??= BasicWrapper.EmptyAdapter;
        if (adapter == Adapter)
            return;

        // Dispose the old adapter.
        Adapter.Dispose();

        // Assign the new adapter.
        Adapter = adapter;
        // Move all event subscriptions to the new adapters events.
        // This works because the managed type check correctly identifies
        // the delegates as their actual action types.
        if (HasAdapter)
            foreach (var ((_, @event), subscriber) in DelegateMap)
                Adapter.Invoke(*(int*)&@event, subscriber, false);
    }

    /// <inheritdoc cref="IIdDataShareAdapter.Invoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [GenerateArities(9, IncludeZeroArity = true)]
    protected unsafe void Invoke<T1>(TEnum methodId, T1 a1)
        where T1 : allows ref struct
        => Adapter.Invoke(*(int*)&methodId, a1);

    /// <inheritdoc cref="IIdDataShareAdapter.TryInvoke{T1,T2,T3,T4,T5,T6,T7,T8,T9,TRet}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [GenerateArities(9, IncludeZeroArity = true)]
    protected unsafe TRet? Invoke<T1, TRet>(TEnum methodId, T1 a1)
        where T1 : allows ref struct
        where TRet : allows ref struct
        => Adapter.TryInvoke(*(int*)&methodId, a1, out TRet? ret) ? ret : throw new InvocationException<TEnum>(methodId);


    /// <summary> Remove a delegate from the delegate map and the current subscription, if an adapter is wrapped. </summary>
    /// <param name="action"> The external action. </param>
    /// <param name="method"> The event. </param>
    /// <remarks> We do not need to specify the type since the managed type check in the invocation will do that. </remarks>
    protected void RemoveDelegate(TEnum method, Delegate? action)
    {
        if (action is null)
            return;

        if (!DelegateMap.TryRemove((action, method), out var del))
            del = action;

        if (HasAdapter)
            Invoke(method, del, true);
    }

    /// <summary> Add a delegate to the subscriber map and the current subscription, if an adapter is wrapped. </summary>
    /// <typeparam name="TIn"> The external action type. </typeparam>
    /// <typeparam name="TOut"> The internal action type. </typeparam>
    /// <param name="original"> The external action. </param>
    /// <param name="createInternal"> A converter between external and internal actions. </param>
    /// <param name="method"> The event. </param>
    /// <remarks> The strong typing helps to ensure compliance, but is not necessary for the actual invocation. </remarks>
    protected void AddDelegate<TIn, TOut>(TEnum method, TIn? original, Func<TIn, TOut> createInternal)
        where TIn : Delegate
        where TOut : Delegate
    {
        if (original is null)
            return;

        if (DelegateMap.ContainsKey((original, method)))
            return;

        var ret = createInternal(original);
        if (!DelegateMap.TryAdd((original, method), ret))
            return;

        if (HasAdapter)
            Invoke(method, ret, false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (HasAdapter)
            try
            {
                foreach (var ((_, b), c) in DelegateMap)
                    Invoke(b, c, true);
            }
            catch (AdapterMethodMissingException)
            {
                // Ignored
            }
            catch (ObjectDisposedException)
            {
                // Ignored
            }


        DelegateMap.Clear();
        Adapter.Dispose();
        Adapter = BasicWrapper.EmptyAdapter;
    }
}
