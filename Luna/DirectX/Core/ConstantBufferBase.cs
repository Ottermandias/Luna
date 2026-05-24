using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Luna.DirectX;

using static DxUtility;

/// <summary> A Direct3D constant buffer. </summary>
public abstract class ConstantBufferBase : IDisposable
{
    private ComPtr<ID3D11Buffer> _buffer;
    private bool                 _dirty;

    /// <summary> A raw view of the contents of this constant buffer. </summary>
    public abstract ReadOnlySpan<byte> ContentsAsBytes { get; }

    ~ConstantBufferBase()
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

    /// <summary> Schedules an update of this constant buffer object on the next use. </summary>
    public void SetDirty()
        => _dirty = true;

    /// <summary> Gets the Direct3D constant buffer object, creating it if necessary. </summary>
    /// <param name="deviceContext"> The device context to use if the buffer needs to be updated. </param>
    /// <returns> The constant buffer object. </returns>
    public unsafe ID3D11Buffer* GetOrCreateBuffer(ID3D11DeviceContext* deviceContext)
    {
        if (!_buffer.Valid)
        {
            _buffer.Attach(Create(ContentsAsBytes));
            _dirty = false;
        }
        else if (_dirty)
        {
            var contents = ContentsAsBytes;

            GetDescription(_buffer, out var desc);
            if (contents.Length == desc.ByteWidth)
            {
                fixed (byte* pContents = contents)
                {
                    Update(deviceContext, _buffer, pContents);
                }
            }
            else
            {
                _buffer.Attach(Create(contents));
            }

            _dirty = false;
        }

        return _buffer;
    }

    /// <summary> Creates a Direct3D constant buffer object. </summary>
    /// <param name="initialContents"> The initial contents of the buffer. May be null. </param>
    /// <param name="size"> The size of the buffer. Will be rounded up to a multiple of 16 bytes. </param>
    /// <returns> The constant buffer object. </returns>
    public static unsafe ID3D11Buffer* Create(void* initialContents, int size)
    {
        var bufferDesc = new D3D11_BUFFER_DESC
        {
            ByteWidth           = (uint)((size + 15) & ~15),
            Usage               = D3D11_USAGE.D3D11_USAGE_DEFAULT,
            BindFlags           = (uint)D3D11_BIND_FLAG.D3D11_BIND_CONSTANT_BUFFER,
            CPUAccessFlags      = 0,
            MiscFlags           = 0,
            StructureByteStride = 0,
        };

        ID3D11Buffer* uniformsBuffer;
        var subresData = new D3D11_SUBRESOURCE_DATA
        {
            pSysMem          = initialContents,
            SysMemPitch      = 0,
            SysMemSlicePitch = 0,
        };

        Marshal.ThrowExceptionForHR(CustomRenderManager.Instance.Device->CreateBuffer(&bufferDesc,
            initialContents is not null ? &subresData : null, &uniformsBuffer));

        return uniformsBuffer;
    }

    /// <summary> Creates a Direct3D constant buffer object. </summary>
    /// <param name="initialContents"> The initial contents of the buffer. Must be a multiple of 16 bytes in size. </param>
    /// <returns> The constant buffer object. </returns>
    public static unsafe ID3D11Buffer* Create(ReadOnlySpan<byte> initialContents)
    {
        if ((initialContents.Length & 15) != 0)
            throw new ArgumentException("Constant buffer initial contents must be a multiple of 16 bytes in size.");
        fixed (byte* pContents = initialContents)
        {
            return Create(pContents, initialContents.Length);
        }
    }

    /// <summary> Updates a Direct3D buffer object. </summary>
    /// <param name="deviceContext"> The device context to use to perform the update. </param>
    /// <param name="buffer"> The buffer object. </param>
    /// <param name="newContents"> The new contents of the buffer. </param>
    public static unsafe void Update(ID3D11DeviceContext* deviceContext, ID3D11Buffer* buffer, void* newContents)
        => deviceContext->UpdateSubresource((ID3D11Resource*)buffer, 0, null, newContents, 0, 0);

    /// <summary> Updates a Direct3D buffer object. </summary>
    /// <param name="deviceContext"> The device context to use to perform the update. </param>
    /// <param name="buffer"> The buffer object. </param>
    /// <param name="newContents"> The new contents of the buffer. </param>
    public unsafe void Update(ID3D11DeviceContext* deviceContext, ID3D11Buffer* buffer, ReadOnlySpan<byte> newContents)
    {
        GetDescription(buffer, out var desc);
        if (newContents.Length != desc.ByteWidth)
            throw new ArgumentException("The new contents must be of the same size as the buffer itself.");
        fixed (byte* pContents = newContents)
        {
            Update(deviceContext, buffer, pContents);
        }
    }
}
