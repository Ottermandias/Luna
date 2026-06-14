using TerraFX.Interop.DirectX;

namespace Luna.DirectX;

/// <summary> A Direct3D constant buffer. </summary>
/// <param name="contents"> The initial contents of the constant buffer. </param>
/// <typeparam name="TContents"> The type of the structure to store in the constant buffer. Must be a multiple of 16 bytes in size. </typeparam>
/// <exception cref="ArgumentException"> <typeparamref name="TContents"/> is not a multiple of 16 bytes in size. </exception>
public class ConstantBuffer<TContents>(in TContents contents = default) : Buffer 
    where TContents : unmanaged
{
    /// <summary> The contents of the constant buffer. </summary>
    public unsafe TContents Contents = (sizeof(TContents) & 15) is 0
        ? contents
        : throw new ArgumentException("The contents' size must be a multiple of 16 bytes.", nameof(TContents));

    /// <inheritdoc/>
    public override D3D11_BIND_FLAG BindFlags
        => D3D11_BIND_FLAG.D3D11_BIND_CONSTANT_BUFFER;

    /// <inheritdoc/>
    public override Span<byte> ContentsAsBytes
        => MemoryMarshal.AsBytes(new Span<TContents>(ref Contents));
}

/// <summary> A Direct3D constant buffer. </summary>
/// <param name="contents"> The initial contents of the constant buffer. </param>
/// <exception cref="ArgumentException"> <paramref name="contents"/> is not a multiple of 16 bytes in size. </exception>
public class ConstantBuffer(byte[] contents) : Buffer
{
    private byte[] _contents = (contents.Length & 15) is 0
        ? contents
        : throw new ArgumentException("The contents' size must be a multiple of 16 bytes.", nameof(contents));

    /// <summary> The contents of this constant buffer. </summary>
    /// <exception cref="ArgumentException"> <paramref name="value"/> is not a multiple of 16 bytes in size. </exception>
    /// <remarks>
    ///   Changing the size of the contents will actually cause the Direct3D constant buffer object to be discarded
    ///   and a new one of the desired size created instead.
    /// </remarks>
    public byte[] Contents
    {
        get => _contents;
        set
        {
            if ((value.Length & 15) is not 0)
                throw new ArgumentException("The new contents' size must be a multiple of 16 bytes.", nameof(value));

            _contents = value;
            SetDirty();
        }
    }

    /// <inheritdoc/>
    public override D3D11_BIND_FLAG BindFlags
        => D3D11_BIND_FLAG.D3D11_BIND_CONSTANT_BUFFER;

    /// <inheritdoc/>
    public override Span<byte> ContentsAsBytes
        => _contents.AsSpan();

    /// <summary> Gets a typed view of the contents of this constant buffer. </summary>
    /// <typeparam name="T"> The element type of the view. </typeparam>
    /// <returns> A typed view of the contents of this constant buffer. </returns>
    public Span<T> GetContentsAs<T>() where T : unmanaged
        => MemoryMarshal.Cast<byte, T>(_contents.AsSpan());
}
