using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Luna.DirectX;

using static DxUtility;

/// <summary> A Direct3D buffer. </summary>
public abstract class Buffer : IDisposable
{
    private ComPtr<ID3D11Buffer> _buffer;
    private bool                 _dirty;

    /// <summary> How this buffer will be bound. </summary>
    public abstract D3D11_BIND_FLAG BindFlags { get; }

    /// <summary> A raw view of the contents of this buffer. </summary>
    public abstract Span<byte> ContentsAsBytes { get; }

    ~Buffer()
        => Dispose(false);

    /// <summary> Releases the resources used by this object. </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary> Releases the resources used by this object. </summary>
    /// <param name="disposing"> True if called explicitly, false if garbage collected. </param>
    protected virtual void Dispose(bool disposing)
        => _buffer.Dispose();

    /// <summary> Schedules an update of this buffer object on the next use. </summary>
    public void SetDirty()
        => _dirty = true;

    /// <summary> Gets the Direct3D buffer object, creating it if necessary. </summary>
    /// <param name="deviceContext"> The device context to use if the buffer needs to be updated. </param>
    /// <returns> The buffer object. </returns>
    public unsafe ID3D11Buffer* GetOrCreateBuffer(ID3D11DeviceContext* deviceContext)
    {
        if (!_buffer.Valid)
        {
            _buffer.Attach(Create(ContentsAsBytes, BindFlags));
            _dirty = false;
        }
        else if (_dirty)
        {
            var contents = ContentsAsBytes;
            var bind     = BindFlags;

            var desc = GetDescription(_buffer);
            if (contents.Length == desc.ByteWidth && (bind & (D3D11_BIND_FLAG)desc.BindFlags) == bind)
            {
                fixed (byte* pContents = contents)
                {
                    UnsafeUpdate(deviceContext, _buffer, pContents);
                }
            }
            else
            {
                _buffer.Attach(Create(contents, bind));
            }

            _dirty = false;
        }

        return _buffer;
    }

    /// <summary> Creates a Direct3D buffer object. </summary>
    /// <param name="initialContents"> The initial contents of the buffer. May be null. </param>
    /// <param name="size"> The size of the buffer. Some types of buffers require it to be a multiple of 16 bytes. </param>
    /// <param name="bind"> How the buffer will be bound. </param>
    /// <returns> The buffer object. </returns>
    public static unsafe ID3D11Buffer* Create(void* initialContents, int size, D3D11_BIND_FLAG bind)
    {
        if (bind.HasFlag(D3D11_BIND_FLAG.D3D11_BIND_CONSTANT_BUFFER) && (size & 15) is not 0)
            throw new ArgumentException("Constant buffer must be a multiple of 16 bytes in size.");

        var bufferDesc = new D3D11_BUFFER_DESC
        {
            ByteWidth           = (uint)size,
            Usage               = D3D11_USAGE.D3D11_USAGE_DEFAULT,
            BindFlags           = (uint)bind,
            CPUAccessFlags      = 0,
            MiscFlags           = 0,
            StructureByteStride = 0,
        };

        ID3D11Buffer* buffer;
        var subresData = new D3D11_SUBRESOURCE_DATA
        {
            pSysMem          = initialContents,
            SysMemPitch      = 0,
            SysMemSlicePitch = 0,
        };

        Marshal.ThrowExceptionForHR(CustomRenderManager.Instance.Device->CreateBuffer(&bufferDesc,
            initialContents is not null ? &subresData : null, &buffer));

        return buffer;
    }

    /// <summary> Creates a Direct3D buffer object. </summary>
    /// <param name="initialContents"> The initial contents of the buffer. Must be a multiple of 16 bytes in size. </param>
    /// <param name="bind"> How the buffer will be bound. </param>
    /// <returns> The buffer object. </returns>
    public static unsafe ID3D11Buffer* Create(ReadOnlySpan<byte> initialContents, D3D11_BIND_FLAG bind)
    {
        fixed (byte* pContents = initialContents)
        {
            return Create(pContents, initialContents.Length, bind);
        }
    }

    /// <summary> Updates a Direct3D buffer object. </summary>
    /// <param name="deviceContext"> The device context to use to perform the update. </param>
    /// <param name="buffer"> The buffer object. </param>
    /// <param name="newContents"> The new contents of the buffer. This must point at a region of memory of the same size as the buffer. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void UnsafeUpdate(ID3D11DeviceContext* deviceContext, ID3D11Buffer* buffer, void* newContents)
        => deviceContext->UpdateSubresource((ID3D11Resource*)buffer, 0, null, newContents, 0, 0);

    /// <summary> Updates a Direct3D buffer object. </summary>
    /// <param name="deviceContext"> The device context to use to perform the update. </param>
    /// <param name="buffer"> The buffer object. </param>
    /// <param name="newContents"> The new contents of the buffer. </param>
    /// <param name="length"> The size of the region of memory <paramref name="newContents"/> points at. Must be the same as the buffer's size. </param>
    public static unsafe void Update(ID3D11DeviceContext* deviceContext, ID3D11Buffer* buffer, void* newContents, int length)
    {
        var desc = GetDescription(buffer);
        if (length != desc.ByteWidth)
            throw new ArgumentException("The new contents must be of the same size as the buffer itself.");
        UnsafeUpdate(deviceContext, buffer, newContents);
    }

    /// <summary> Updates a Direct3D buffer object. </summary>
    /// <param name="deviceContext"> The device context to use to perform the update. </param>
    /// <param name="buffer"> The buffer object. </param>
    /// <param name="newContents"> The new contents of the buffer. </param>
    public static unsafe void Update(ID3D11DeviceContext* deviceContext, ID3D11Buffer* buffer, ReadOnlySpan<byte> newContents)
    {
        var desc = GetDescription(buffer);
        if (newContents.Length != desc.ByteWidth)
            throw new ArgumentException("The new contents must be of the same size as the buffer itself.");
        fixed (byte* pContents = newContents)
        {
            UnsafeUpdate(deviceContext, buffer, pContents);
        }
    }
}
