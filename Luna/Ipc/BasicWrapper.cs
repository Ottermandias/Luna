using Dalamud.Plugin.Ipc;

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
    /// <inheritdoc cref="IBasicWrapper{TWrapper}.CreateWrapper"/>
    public static TWrapper? Create<TWrapper>(IIdDataShareAdapter? adapter) where TWrapper : IBasicWrapper<TWrapper>
        => TWrapper.CreateWrapper(adapter);
}

/// <summary> A base class for wrappers for a specific method ID enumeration type (assumed to be based on <see cref="int"/>). </summary>
public abstract class BasicWrapper<TSelf, TEnum>(IIdDataShareAdapter adapter) : IDisposable
    where TSelf : BasicWrapper<TSelf, TEnum>, IBasicWrapper<TSelf>
    where TEnum : unmanaged, Enum
{
    /// <inheritdoc cref="IBasicWrapper{TSelf}.Adapter"/>
    public IIdDataShareAdapter Adapter { get; } = adapter;

    /// <inheritdoc cref="IIdDataShareAdapter.Invoke"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe void Invoke(TEnum methodId)
        => Adapter.Invoke(*(int*)&methodId);

    /// <inheritdoc cref="IIdDataShareAdapter.Invoke{T1}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe void Invoke<T1>(TEnum methodId, T1 a)
        where T1 : allows ref struct
        => Adapter.Invoke(*(int*)&methodId, a);

    /// <inheritdoc cref="IIdDataShareAdapter.Invoke{T1,T2}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe void Invoke<T1, T2>(TEnum methodId, T1 a, T2 b)
        where T1 : allows ref struct
        where T2 : allows ref struct
        => Adapter.Invoke(*(int*)&methodId, a, b);

    /// <inheritdoc cref="IIdDataShareAdapter.Invoke{T1,T2,T3}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe void Invoke<T1, T2, T3>(TEnum methodId, T1 a, T2 b, T3 c)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        => Adapter.Invoke(*(int*)&methodId, a, b, c);

    /// <inheritdoc cref="IIdDataShareAdapter.Invoke{T1,T2,T3,T4}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe void Invoke<T1, T2, T3, T4>(TEnum methodId, T1 a, T2 b, T3 c, T4 d)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        => Adapter.Invoke(*(int*)&methodId, a, b, c, d);

    /// <inheritdoc cref="IIdDataShareAdapter.Invoke{T1,T2,T3,T4,T5}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe void Invoke<T1, T2, T3, T4, T5>(TEnum methodId, T1 a, T2 b, T3 c, T4 d, T5 e)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        => Adapter.Invoke(*(int*)&methodId, a, b, c, d, e);

    /// <inheritdoc cref="IIdDataShareAdapter.Invoke{T1,T2,T3,T4,T5,T6}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe void Invoke<T1, T2, T3, T4, T5, T6>(
        TEnum methodId, T1 a, T2 b, T3 c, T4 d, T5 e, T6 f)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where T6 : allows ref struct
        => Adapter.Invoke(*(int*)&methodId, a, b, c, d, e, f);

    /// <inheritdoc cref="IIdDataShareAdapter.Invoke{T1,T2,T3,T4,T5,T6,T7}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe void Invoke<T1, T2, T3, T4, T5, T6, T7>(
        TEnum methodId, T1 a, T2 b, T3 c, T4 d, T5 e, T6 f, T7 g)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where T6 : allows ref struct
        where T7 : allows ref struct
        => Adapter.Invoke(*(int*)&methodId, a, b, c, d, e, f, g);

    /// <inheritdoc cref="IIdDataShareAdapter.Invoke{T1,T2,T3,T4,T5,T6,T7,T8}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe void Invoke<T1, T2, T3, T4, T5, T6, T7, T8>(
        TEnum methodId, T1 a, T2 b, T3 c, T4 d, T5 e, T6 f, T7 g, T8 h)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where T6 : allows ref struct
        where T7 : allows ref struct
        where T8 : allows ref struct
        => Adapter.Invoke(*(int*)&methodId, a, b, c, d, e, f, g, h);

    /// <inheritdoc cref="IIdDataShareAdapter.Invoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
        TEnum methodId, T1 a, T2 b, T3 c, T4 d, T5 e, T6 f, T7 g, T8 h, T9 i)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where T6 : allows ref struct
        where T7 : allows ref struct
        where T8 : allows ref struct
        where T9 : allows ref struct
        => Adapter.Invoke(*(int*)&methodId, a, b, c, d, e, f, g, h, i);

    /// <inheritdoc cref="IIdDataShareAdapter.Invoke"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe TRet? Invoke<TRet>(TEnum methodId)
        where TRet : allows ref struct
        => Adapter.TryInvoke(*(int*)&methodId, out TRet? ret) ? ret : throw new InvocationException<TEnum>(methodId);

    /// <inheritdoc cref="IIdDataShareAdapter.Invoke{T1}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe TRet? Invoke<T1, TRet>(TEnum methodId, T1 a)
        where T1 : allows ref struct
        where TRet : allows ref struct
        => Adapter.TryInvoke(*(int*)&methodId, a, out TRet? ret) ? ret : throw new InvocationException<TEnum>(methodId);

    /// <inheritdoc cref="IIdDataShareAdapter.Invoke{T1,T2}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe TRet? Invoke<T1, T2, TRet>(TEnum methodId, T1 a, T2 b)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where TRet : allows ref struct
        => Adapter.TryInvoke(*(int*)&methodId, a, b, out TRet? ret) ? ret : throw new InvocationException<TEnum>(methodId);

    /// <inheritdoc cref="IIdDataShareAdapter.Invoke{T1,T2,T3}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe TRet? Invoke<T1, T2, T3, TRet>(TEnum methodId, T1 a, T2 b, T3 c)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where TRet : allows ref struct
        => Adapter.TryInvoke(*(int*)&methodId, a, b, c, out TRet? ret) ? ret : throw new InvocationException<TEnum>(methodId);

    /// <inheritdoc cref="IIdDataShareAdapter.Invoke{T1,T2,T3,T4}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe TRet? Invoke<T1, T2, T3, T4, TRet>(TEnum methodId, T1 a, T2 b, T3 c, T4 d)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where TRet : allows ref struct
        => Adapter.TryInvoke(*(int*)&methodId, a, b, c, d, out TRet? ret) ? ret : throw new InvocationException<TEnum>(methodId);

    /// <inheritdoc cref="IIdDataShareAdapter.Invoke{T1,T2,T3,T4,T5}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe TRet? Invoke<T1, T2, T3, T4, T5, TRet>(TEnum methodId, T1 a, T2 b, T3 c, T4 d, T5 e)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where TRet : allows ref struct
        => Adapter.TryInvoke(*(int*)&methodId, a, b, c, d, e, out TRet? ret) ? ret : throw new InvocationException<TEnum>(methodId);

    /// <inheritdoc cref="IIdDataShareAdapter.Invoke{T1,T2,T3,T4,T5,T6}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe TRet? Invoke<T1, T2, T3, T4, T5, T6, TRet>(
        TEnum methodId, T1 a, T2 b, T3 c, T4 d, T5 e, T6 f)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where T6 : allows ref struct
        where TRet : allows ref struct
        => Adapter.TryInvoke(*(int*)&methodId, a, b, c, d, e, f, out TRet? ret) ? ret : throw new InvocationException<TEnum>(methodId);

    /// <inheritdoc cref="IIdDataShareAdapter.Invoke{T1,T2,T3,T4,T5,T6,T7}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe TRet? Invoke<T1, T2, T3, T4, T5, T6, T7, TRet>(
        TEnum methodId, T1 a, T2 b, T3 c, T4 d, T5 e, T6 f, T7 g)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where T6 : allows ref struct
        where T7 : allows ref struct
        where TRet : allows ref struct
        => Adapter.TryInvoke(*(int*)&methodId, a, b, c, d, e, f, g, out TRet? ret) ? ret : throw new InvocationException<TEnum>(methodId);

    /// <inheritdoc cref="IIdDataShareAdapter.Invoke{T1,T2,T3,T4,T5,T6,T7,T8}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe TRet? Invoke<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(
        TEnum methodId, T1 a, T2 b, T3 c, T4 d, T5 e, T6 f, T7 g, T8 h)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where T6 : allows ref struct
        where T7 : allows ref struct
        where T8 : allows ref struct
        where TRet : allows ref struct
        => Adapter.TryInvoke(*(int*)&methodId, a, b, c, d, e, f, g, h, out TRet? ret) ? ret : throw new InvocationException<TEnum>(methodId);

    /// <inheritdoc cref="IIdDataShareAdapter.Invoke{T1,T2,T3,T4,T5,T6,T7,T8,T9}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe TRet? Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet>(
        TEnum methodId, T1 a, T2 b, T3 c, T4 d, T5 e, T6 f, T7 g, T8 h, T9 i)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        where T4 : allows ref struct
        where T5 : allows ref struct
        where T6 : allows ref struct
        where T7 : allows ref struct
        where T8 : allows ref struct
        where T9 : allows ref struct
        where TRet : allows ref struct
        => Adapter.TryInvoke(*(int*)&methodId, a, b, c, d, e, f, g, h, i, out TRet? ret) ? ret : throw new InvocationException<TEnum>(methodId);

    /// <inheritdoc />
    public void Dispose()
        => Adapter.Dispose();
}
