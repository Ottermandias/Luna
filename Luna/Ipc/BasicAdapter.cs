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

        /// <summary> Get all events this adapter is subscribed to. </summary>
        public IEnumerable<string> EventSubscriptions { get; }

        /// <summary> The version of this adapter. </summary>
        /// <remarks>
        ///   The implementation of this should use an <see cref="AdapterMethodAttribute"/> with the value <see cref="BasicWrapper.VersionMethod"/> (-1)
        ///   unless the associated <see cref="BasicWrapper{TSelf,TEnum}"/> implements its <see cref="BasicWrapper{TSelf,TEnum}.Version"/> property differently.
        ///   It is also recommended to use <see cref="AdapterMethodAttribute.AlwaysAlive"/> for this.
        /// </remarks>
        public (int Major, int Minor) Version { get; }

        /// <summary> Whether the adapter is still alive. </summary>
        /// <remarks>
        ///   The implementation of this should use an <see cref="AdapterMethodAttribute"/> with the value <see cref="BasicWrapper.AliveMethod"/> (-2)
        ///   unless the associated <see cref="BasicWrapper{TSelf,TEnum}"/> implements its <see cref="BasicWrapper{TSelf,TEnum}.Alive"/> property differently.
        ///   It is also recommended to use <see cref="AdapterMethodAttribute.AlwaysAlive"/> for this.
        /// </remarks>
        public bool Alive { get; }

        /// <summary> An event that is invoked when the adapter is being disposed. </summary>
        /// <remarks>
        ///   The implementation of this should use an <see cref="AdapterMethodAttribute"/> with the value <see cref="BasicWrapper.DisposedEventMethod"/> (-3)
        ///   unless the associated <see cref="BasicWrapper{TSelf,TEnum}"/> implements its <see cref="BasicWrapper{TSelf,TEnum}.Disposed"/> event differently.
        ///   It is also recommended to use <see cref="AdapterMethodAttribute.AlwaysAlive"/> for this.
        /// </remarks>
        public event Action Disposed;
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

        /// <inheritdoc cref="IBasicAdapter.Version"/>
        public abstract (int Major, int Minor) Version { get; }

        /// <inheritdoc cref="IBasicAdapter.Alive"/>
        public abstract bool Alive { get; }

        /// <summary> Invoke the <see cref="IBasicAdapter.Disposed"/> event. </summary>
        protected abstract void InvokeDisposed();

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
        protected static TOut? CheckValue<TIn, TOut>(int method, int numArguments, bool func, int argumentIndex, TIn arg)
            where TIn : allows ref struct
            where TOut : allows ref struct
        {
            if (arg is null)
                return default;

            var obj = Unsafe.As<TIn, object>(ref arg);
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
            if (!Alive)
                return;

            try
            {
                InvokeDisposed();
                SubscribedEvents.Clear();
                if (Parent?.IpcManager._disposed is false)
                {
                    lock (Parent.IpcManager._objects)
                    {
                        Parent.IpcManager._objects.RemoveValue(Owner, (IBasicAdapter)this);
                    }

                    LogDisposal(Parent.IpcManager._log, Type, Owner);
                }

                DisposeInternal();
                GC.SuppressFinalize(this);
            }
            finally
            {
                Parent = null;
            }
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
