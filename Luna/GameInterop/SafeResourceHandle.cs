using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;

namespace Luna;

/// <summary> A safe handle that wraps a game <see cref="ResourceHandle"/> object. </summary>
public unsafe class SafeResourceHandle : SafeHandle, ICloneable
{
    /// <summary> Gets the wrapped resource handle. </summary>
    public ResourceHandle* ResourceHandle
        // ReSharper disable once InconsistentlySynchronizedField
        => (ResourceHandle*)handle;

    /// <inheritdoc/>
    public override bool IsInvalid
        => handle == nint.Zero;

    /// <summary> Constructs a new <see cref="SafeResourceHandle"/> wrapping the given resource handle. </summary>
    /// <param name="handle"> The game resource handle to wrap. </param>
    /// <param name="incRef"> Whether to increment the reference count of <paramref name="handle"/>. </param>
    /// <param name="ownsHandle"> Whether to decrement the reference count of <paramref name="handle"/> on disposal. </param>
    /// <exception cref="ArgumentException"> <paramref name="incRef"/> is <c>true</c> but <paramref name="ownsHandle"/> is false. </exception>
    public SafeResourceHandle(ResourceHandle* handle, bool incRef = true, bool ownsHandle = true)
        : base(0, ownsHandle)
    {
        if (incRef && !ownsHandle)
            throw new ArgumentException("Non-owning SafeResourceHandle with IncRef is unsupported");

        if (incRef && handle is not null)
            handle->IncRef();
        SetHandle((nint)handle);
    }

    /// <summary> Creates a new <see cref="SafeResourceHandle"/> that wraps the same resource handle as this one. </summary>
    /// <returns> A new <see cref="SafeResourceHandle"/> that wraps the same resource handle as this one. </returns>
    public SafeResourceHandle Clone()
        => new(ResourceHandle);

    /// <inheritdoc/>
    object ICloneable.Clone()
        => Clone();

    /// <summary> Exchanges the wrapped resource handle with the one at the given location. </summary>
    /// <param name="ppResource"> The location with which to exchange resource handles. </param>
    public void Exchange(ref nint ppResource)
    {
        lock (this)
        {
            handle = Interlocked.Exchange(ref ppResource, handle);
        }
    }

    /// <summary> Creates an invalid <see cref="SafeResourceHandle"/>. </summary>
    /// <returns> An invalid <see cref="SafeResourceHandle"/>. </returns>
    public static SafeResourceHandle CreateInvalid()
        => new(null, false);

    /// <inheritdoc/>
    protected override bool ReleaseHandle()
    {
        var localHandle = Interlocked.Exchange(ref handle, 0);
        if (localHandle != nint.Zero)
            ((ResourceHandle*)localHandle)->DecRef();

        return true;
    }
}
