using Dalamud.Plugin.Ipc;
using Luna.Generators;

namespace Luna;

public sealed partial class IpcObjectManager
{
    /// <summary> Since <see cref="BasicAdapter"/> can not implement <see cref="IIdDataShareAdapter"/> directly due to intermediate classes breaking default interface resolution, we use this basic interface instead. </summary>
    public interface IBasicAdapter : IIdDataShareAdapter
    {
        /// <summary> The internal name of the plugin that requested this adapter. </summary>
        public string Owner { get; }

        /// <summary> The actual type name of this adapter. </summary>
        public string Type { get; }

        /// <summary> Whether the adapter is still alive. </summary>
        public bool Alive { get; }

        /// <summary> Get all events this adapter is subscribed to. </summary>
        public IEnumerable<string> EventSubscriptions { get; }

        /// <summary> The version of this adapter. </summary>
        /// <remarks>
        ///   The implementation of this should use an <see cref="AdapterMethodAttribute"/> with the value <c>-1</c>
        ///   unless the associated <see cref="BasicWrapper{TSelf,TEnum}"/> implements its <see cref="BasicWrapper{TSelf,TEnum}.Version"/> attribute differently.
        /// </remarks>
        public (int Major, int Minor) Version { get; }
    }

    /// <summary> Utility functions for data adapters. </summary>
    public abstract class BasicAdapter(IAdapterFactory parent, string owner, string type) : IDisposable
    {
        /// <summary> Display names of events we are subscribed to. </summary>
        protected readonly ConcurrentSet<string> SubscribedEvents = [];

        /// <inheritdoc cref="IBasicAdapter.EventSubscriptions"/>
        public IEnumerable<string> EventSubscriptions
            => SubscribedEvents;

        /// <summary> The factory that created this adapter. </summary>
        protected IAdapterFactory? Parent { get; private set; } = parent;

        /// <summary> The internal name of the plugin that requested this adapter. </summary>
        public string Owner { get; } = owner;

        /// <summary> The actual type name of this adapter. </summary>
        public string Type { get; } = type;

        /// <summary> Whether the adapter is still alive. </summary>
        public bool Alive
            => Parent is not null;

        /// <summary> The version of this adapter. </summary>
        /// <remarks>
        ///   The implementation of this should use an <see cref="AdapterMethodAttribute"/> with the value <c>-1</c>
        ///   unless the associated <see cref="BasicWrapper{TSelf,TEnum}"/> implements its <see cref="BasicWrapper{TSelf,TEnum}.Version"/> attribute differently.
        /// </remarks>
        public abstract (int Major, int Minor) Version { get; }

        /// <summary> Check that a passed unmanaged type matches the expected input type and convert it. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static TOut CheckValue<TArg, TOut>(int method, int numArguments, bool func, int argumentIndex, ref TArg arg)
            where TArg : allows ref struct
            where TOut : allows ref struct
        {
            if (typeof(TArg) != typeof(TOut))
                throw new AdapterTypeMismatchException(method, numArguments, func, argumentIndex, typeof(TArg));

            return Unsafe.As<TArg, TOut>(ref arg);
        }

        /// <summary> Check that a passed unmanaged type matches the expected return type and convert it. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static TRet? CheckRet<TRet, TOut>(int method, int numArguments, TOut? value)
            where TRet : allows ref struct
            where TOut : allows ref struct
        {
            if (typeof(TRet) != typeof(TOut))
                throw new AdapterTypeMismatchException(method, numArguments, true, -1, typeof(TRet));

            if (value is null)
                return default;

            return Unsafe.As<TOut, TRet>(ref value);
        }

        /// <summary> Check that a passed managed type matches the expected input type and convert it. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static TOut? CheckValue<T1, TOut>(int method, int numArguments, bool func, int argumentIndex, T1 arg)
            where T1 : allows ref struct
            where TOut : allows ref struct
        {
            if (arg is null)
                return default;

            var obj = Unsafe.As<T1, object>(ref arg);
            if (obj is not TOut ret)
                throw new AdapterTypeMismatchException(method, numArguments, func, argumentIndex, obj.GetType());

            return ret;
        }

        /// <summary> Check that a passed managed object matches the expected return type and convert it. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static TRet? CheckRet<TRet>(int method, int numArguments, object? value, bool dispose = false)
            where TRet : allows ref struct
        {
            if (value is null)
                return default;

            if (value is not TRet ret)
            {
                if (dispose)
                    (value as IDisposable)?.Dispose();
                throw new AdapterTypeMismatchException(method, numArguments, true, -1, value.GetType());
            }

            return ret;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Parent is null)
                return;

            SubscribedEvents.Clear();
            if (!Parent.IpcManager._disposed)
            {
                lock (Parent.IpcManager._objects)
                {
                    Parent.IpcManager._objects.RemoveValue(Owner, (IBasicAdapter)this);
                }

                LogDisposal(Parent.IpcManager._log, Type, Owner);
            }

            DisposeInternal();
            GC.SuppressFinalize(this);
            Parent = null;
        }

        /// <summary> Any additional cleanup the adapter needs to do. </summary>
        protected virtual void DisposeInternal()
        { }

        /// <summary> Check whether the adapter is still alive and throw if not. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [MemberNotNull(nameof(Parent))]
        protected void CheckAlive()
            => ObjectDisposedException.ThrowIf(Parent is null, this);
    }
}
