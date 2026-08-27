using System.Text.Json;

namespace Luna;

/// <summary> Different ways to compress the version byte in compression functions. </summary>
public enum CompressionVersionMode : byte
{
    /// <summary> Do not add or parse a version byte. </summary>
    None = 0b00,

    /// <summary> Prepend a version byte to the data before compressing it, or parse the first decompressed byte as version. </summary>
    Compressed = 0b01,

    /// <summary> Prepend a version byte to the compressed data, or parse the first byte after decoding as a version. </summary>
    Uncompressed = 0b10,

    /// <summary> Do both <see cref="Compressed"/> and <see cref="Uncompressed"/> and ensure the byte is the same for both. </summary>
    Both = 0b11,
}

public static class CompressionFunctions
{
    /// <summary> Compress byte data to a base64 encoding of its compressed JSON representation, prepended with a version byte. </summary>
    /// <param name="data"> The byte-wise data to serialize to JSON and compress. </param>
    /// <param name="version"> The version byte to prepend to the data. </param>
    /// <param name="mode"> The mode of prepending the version byte. </param>
    /// <returns> An empty array on failure, otherwise the compressed, versioned data converted to Base64. </returns>
    /// <remarks> See <see cref="FromCompressedBase64(ReadOnlySpan{byte},out Memory{byte},CompressionVersionMode)"/> for the decompression steps. </remarks>
    public static byte[] ToCompressedBase64(ReadOnlySpan<byte> data, byte version,
        CompressionVersionMode mode = CompressionVersionMode.Compressed)
    {
        using var compressedStream = Compress(data, version, mode);
        return Encode(compressedStream, version, mode);
    }

    /// <summary> Compress any type to a base64 encoding of its compressed JSON representation, prepended with a version byte. </summary>
    /// <typeparam name="T"> The data type to compress via JSON. </typeparam>
    /// <param name="data"> The data to serialize to JSON and compress. </param>
    /// <param name="version"> The version byte to prepend to the UTF8 JSON data. </param>
    /// <param name="mode"> The mode of prepending the version byte. </param>
    /// <returns> An empty string on failure, otherwise the compressed, versioned data converted to Base64. </returns>
    /// <remarks> See <see cref="FromCompressedBase64{T}"/> for the decompression steps. </remarks>
    public static byte[] ToCompressedBase64<T>(T data, byte version, CompressionVersionMode mode = CompressionVersionMode.Compressed)
    {
        try
        {
            var json = JsonSerializer.SerializeToUtf8Bytes(data, JsonFunctions.UnformattedSerializerOptions);
            return ToCompressedBase64(json, version);
        }
        catch
        {
            return [];
        }
    }

    /// <summary> Decompress an UTF8 Base64 encoded string to the uncompressed original data and a prepended version byte if possible. </summary>
    /// <param name="base64"> The Base64-encoded compressed JSON serialization of data with a prepended version byte. </param>
    /// <param name="data"> The decompressed data on success or empty data otherwise. </param>
    /// <param name="mode"> The mode of parsing the prepended version bytes. </param>
    /// <returns> The version byte that was prepended or <see cref="byte.MaxValue"/> on failure. </returns>
    /// <remarks> See <see cref="ToCompressedBase64"/> for the compression steps. </remarks>
    public static byte FromCompressedBase64(ReadOnlySpan<byte> base64, out Memory<byte> data,
        CompressionVersionMode mode = CompressionVersionMode.Compressed)
    {
        if (Decode(base64, out var compressed))
            return Decompress(compressed, CompressionVersionMode.Compressed, out data);

        data = Memory<byte>.Empty;
        return byte.MaxValue;
    }

    /// <summary> Decompress an UTF16 Base64 encoded string to the uncompressed original data and a prepended version byte if possible. </summary>
    /// <param name="base64"> The Base64-encoded compressed JSON serialization of data with a prepended version byte. </param>
    /// <param name="data"> The decompressed data on success or empty data otherwise. </param>
    /// <param name="mode"> The mode of parsing the prepended version bytes. </param>
    /// <returns> The version byte that was prepended or <see cref="byte.MaxValue"/> on failure. </returns>
    /// <remarks> See <see cref="ToCompressedBase64"/> for the compression steps. </remarks>
    public static byte FromCompressedBase64(ReadOnlySpan<char> base64, out Memory<byte> data,
        CompressionVersionMode mode = CompressionVersionMode.Compressed)
    {
        if (Decode(base64, out var compressed))
            return Decompress(compressed, CompressionVersionMode.Compressed, out data);

        data = Memory<byte>.Empty;
        return byte.MaxValue;
    }

    /// <summary> Decompress a Base64 encoded string to the given type and a prepended version byte if possible. </summary>
    /// <typeparam name="T"> The data type to decompress the string into. </typeparam>
    /// <param name="base64"> The Base64-encoded compressed JSON serialization of data with a prepended version byte. </param>
    /// <param name="data"> The decompressed and parsed data on success or defaulted data otherwise. </param>
    /// <param name="mode"> The mode of parsing the prepended version bytes. </param>
    /// <returns> The version byte that was prepended or <see cref="byte.MaxValue"/> on failure. </returns>
    /// <remarks> See <see cref="ToCompressedBase64{T}"/> for the compression steps. </remarks>
    public static byte FromCompressedBase64<T>(ReadOnlySpan<byte> base64, out T? data,
        CompressionVersionMode mode = CompressionVersionMode.Compressed)
    {
        var version = byte.MaxValue;
        data = default;
        if (!Decode(base64, out var compressed))
            return version;

        using var decompressed = Decompress(compressed, mode, out int offset);
        if (decompressed is null)
            return version;

        if (offset is 1)
        {
            version = (byte)decompressed.ReadByte();
            if (mode.HasFlag(CompressionVersionMode.Uncompressed) && version != compressed.Span[0])
                return byte.MaxValue;
        }
        else if (mode.HasFlag(CompressionVersionMode.Uncompressed))
        {
            version = compressed.Span[0];
        }
        else
        {
            version = 0;
        }

        data = JsonSerializer.Deserialize<T>(decompressed, JsonFunctions.SerializerOptions);

        return version;
    }

    /// <summary> Compress the given data, optionally prepending a version byte before compression. </summary>
    /// <param name="data"> The data to compress. </param>
    /// <param name="version"> The version byte to prepend before compression if <paramref name="mode"/> has the <see cref="CompressionVersionMode.Compressed"/> flag. </param>
    /// <param name="mode"> The mode, only the <see cref="CompressionVersionMode.Compressed"/> flag is relevant. </param>
    /// <returns> A memory stream containing the compressed data. </returns>
    /// <remarks> Take care that if <paramref name="mode"/> has both flags, the version passed to <see cref="Encode"/> should agree with <paramref name="version"/>. </remarks>
    public static unsafe MemoryStream Compress(ReadOnlySpan<byte> data, byte version = 0,
        CompressionVersionMode mode = CompressionVersionMode.None)
    {
        var compressedStream = new MemoryStream();
        using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress, true))
        {
            if (mode.HasFlag(CompressionVersionMode.Compressed))
                zipStream.Write(new ReadOnlySpan<byte>(&version, 1));
            zipStream.Write(data);
        }

        compressedStream.Flush();
        compressedStream.Position = 0;
        return compressedStream;
    }

    /// <summary> Encode an already compressed stream's data to UTF8 Base64, optionally prepending an uncompressed version byte. </summary>
    /// <param name="compressedStream"> The data to encode. </param>
    /// <param name="version"> The version byte to prepend before encoding if <paramref name="mode"/> has the <see cref="CompressionVersionMode.Uncompressed"/> flag.</param>
    /// <param name="mode"> The mode, only the <see cref="CompressionVersionMode.Uncompressed"/> flag is relevant. </param>
    /// <returns> A byte array of an UTF8 Base64 string. </returns>
    /// <remarks> Take care that if <paramref name="mode"/> has both flags, the version passed to <see cref="Compress"/> should agree with <paramref name="version"/>. </remarks>
    public static byte[] Encode(MemoryStream compressedStream, byte version, CompressionVersionMode mode)
    {
        int    length;
        byte[] ret;
        if (mode.HasFlag(CompressionVersionMode.Uncompressed))
        {
            ret    = new byte[System.Buffers.Text.Base64.GetMaxEncodedToUtf8Length((int)compressedStream.Length + 1)];
            ret[0] = version;
            length = compressedStream.Read(ret.AsSpan(1));
        }
        else
        {
            ret    = new byte[System.Buffers.Text.Base64.GetMaxEncodedToUtf8Length((int)compressedStream.Length)];
            length = compressedStream.Read(ret);
        }

        if (System.Buffers.Text.Base64.EncodeToUtf8InPlace(ret, length, out var newLength) is not OperationStatus.Done)
            return [];

        Array.Resize(ref ret, newLength);
        return ret;
    }

    /// <summary> Decode a UTF16 Base64 string to its raw byte data. </summary>
    /// <param name="base64"> The UTF16 Base64 string. </param>
    /// <param name="compressed"> The generally compressed byte data, or an empty array on failure. </param>
    /// <returns> True on success, false if the passed data was not valid Base64. </returns>
    public static bool Decode(ReadOnlySpan<char> base64, out Memory<byte> compressed)
    {
        try
        {
            compressed = new byte[Encoding.UTF8.GetByteCount(base64)];
            var count     = Encoding.UTF8.GetBytes(base64, compressed.Span);
            var operation = System.Buffers.Text.Base64.DecodeFromUtf8InPlace(compressed.Span[..count], out var newLength);
            if (operation is not OperationStatus.Done)
                return false;

            compressed = compressed[..newLength];
            return true;
        }
        catch
        {
            compressed = Memory<byte>.Empty;
            return false;
        }
    }

    /// <summary> Decode a UTF8 Base64 string to its raw byte data. </summary>
    /// <param name="base64"> The UTF8 Base64 string. </param>
    /// <param name="compressed"> The generally compressed byte data, or an empty array on failure. </param>
    /// <returns> True on success, false if the passed data was not valid Base64. </returns>
    public static bool Decode(ReadOnlySpan<byte> base64, out Memory<byte> compressed)
    {
        compressed = new byte[System.Buffers.Text.Base64.GetMaxDecodedFromUtf8Length(base64.Length)];
        var operation = System.Buffers.Text.Base64.DecodeFromUtf8(base64, compressed.Span, out _, out var length);
        if (operation is not OperationStatus.Done)
            return false;

        compressed = compressed[..length];
        return true;
    }

    /// <summary> Decompress the given data and return the version byte. </summary>
    /// <param name="data"> The data to decompress. </param>
    /// <param name="mode"> The mode which is used to calculate the correct offsets for both the uncompressed and the compressed data. </param>
    /// <param name="uncompressed"> The decompressed data on success. </param>
    /// <returns> The parsed version on success or <see cref="uint.MaxValue"/> on failure. </returns>
    public static byte Decompress(Memory<byte> data, CompressionVersionMode mode, out Memory<byte> uncompressed)
    {
        using var resultStream = Decompress(data, mode, out int offset);
        if (resultStream is null)
        {
            uncompressed = Memory<byte>.Empty;
            return byte.MaxValue;
        }

        var result = resultStream.ToArray();
        uncompressed = result.AsMemory(offset);
        return mode switch
        {
            CompressionVersionMode.Compressed => result[0],
            CompressionVersionMode.Uncompressed => data.Span[0],
            CompressionVersionMode.Both when result[0] == data.Span[0] => result[0],
            CompressionVersionMode.Both => throw new Exception("Inner version {result[0]} does not  match outer version {data[0]}."),
            CompressionVersionMode.None => 0,
            _ => byte.MaxValue,
        };
    }

    /// <summary> Decompress the given data into a stream, handling the version byte. </summary>
    /// <param name="data"> The data to decompress. </param>
    /// <param name="mode"> The mode which is used to calculate the correct offsets for both the stream and the compressed data. </param>
    /// <param name="uncompressedOffset"> The offset into the uncompressed data to skip the potential version byte (either 0 or 1). </param>
    /// <returns> The uncompressed stream on success, null on failure. </returns>
    public static MemoryStream? Decompress(Memory<byte> data, CompressionVersionMode mode, out int uncompressedOffset)
    {
        try
        {
            if (!MemoryMarshal.TryGetArray<byte>(data, out var segment) || segment.Array is not { } array)
                throw new Exception("Invalid array passed.");

            (var compressedOffset, var compressedLength, uncompressedOffset) = mode switch
            {
                CompressionVersionMode.Compressed   => (segment.Offset, segment.Count, 1),
                CompressionVersionMode.Uncompressed => (segment.Offset + 1, segment.Count - 1, 0),
                CompressionVersionMode.Both         => (segment.Offset + 1, segment.Count - 1, 1),
                _                                   => (segment.Offset, segment.Count, 0),
            };

            using var compressedStream = new MemoryStream(array, compressedOffset, compressedLength);
            using var zipStream        = new GZipStream(compressedStream, CompressionMode.Decompress);
            var       resultStream     = new MemoryStream();
            zipStream.CopyTo(resultStream);
            return resultStream;
        }
        catch
        {
            uncompressedOffset = 0;
            return null;
        }
    }
}
