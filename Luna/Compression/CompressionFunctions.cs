using System.Text.Json;

namespace Luna;

public static class CompressionFunctions
{
    /// <summary> Compress byte data to a base64 encoding of its compressed JSON representation, prepended with a version byte. </summary>
    /// <param name="data"> The byte-wise data to serialize to JSON and compress. </param>
    /// <param name="version"> The version byte to prepend to the UTF8 JSON data. </param>
    /// <returns> An empty string on failure, otherwise the compressed, versioned data converted to Base64. </returns>
    /// <remarks> See <see cref="FromCompressedBase64{T}"/> for the decompression steps. </remarks>
    public static unsafe byte[] ToCompressedBase64(ReadOnlySpan<byte> data, byte version)
    {
        try
        {
            using var compressedStream = new MemoryStream();
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(new ReadOnlySpan<byte>(&version, 1));
                zipStream.Write(data);
            }

            var ret    = new byte[System.Buffers.Text.Base64.GetMaxEncodedToUtf8Length((int)compressedStream.Length)];
            var length = compressedStream.Read(ret);
            if (System.Buffers.Text.Base64.EncodeToUtf8InPlace(ret, length, out var newLength) is not OperationStatus.Done)
                return [];

            Array.Resize(ref ret, newLength);
            return ret;
        }
        catch
        {
            return [];
        }
    }

    /// <summary> Compress any type to a base64 encoding of its compressed JSON representation, prepended with a version byte. </summary>
    /// <typeparam name="T"> The data type to compress via JSON. </typeparam>
    /// <param name="data"> The data to serialize to JSON and compress. </param>
    /// <param name="version"> The version byte to prepend to the UTF8 JSON data. </param>
    /// <returns> An empty string on failure, otherwise the compressed, versioned data converted to Base64. </returns>
    /// <remarks> See <see cref="FromCompressedBase64{T}"/> for the decompression steps. </remarks>
    public static unsafe byte[] ToCompressedBase64<T>(T data, byte version)
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

    /// <summary> Decompress a Base64 encoded string to the uncompressed original data and a prepended version byte if possible. </summary>
    /// <param name="base64"> The Base64-encoded compressed JSON serialization of data with a prepended version byte. </param>
    /// <param name="data"> The decompressed data on success or empty data otherwise. </param>
    /// <returns> The version byte that was prepended or <see cref="byte.MaxValue"/> on failure. </returns>
    /// <remarks> See <see cref="ToCompressedBase64{T}"/> for the compression steps. </remarks>
    public static byte FromCompressedBase64(ReadOnlySpan<byte> base64, out Memory<byte> data)
    {
        var version = byte.MaxValue;
        try
        {
            var bytes = new byte[System.Buffers.Text.Base64.GetMaxDecodedFromUtf8Length(base64.Length)];
            if (System.Buffers.Text.Base64.DecodeFromUtf8(base64, bytes, out _, out var length) is not OperationStatus.Done)
            {
                data = null;
                return version;
            }

            using var compressedStream = new MemoryStream(bytes);
            using var zipStream        = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var resultStream     = new MemoryStream();
            zipStream.CopyTo(resultStream);
            var result = resultStream.ToArray();
            version = result[0];
            data    = result.AsMemory(1);
        }
        catch
        {
            data = Memory<byte>.Empty;
        }

        return version;
    }

    /// <summary> Decompress a Base64 encoded string to the given type and a prepended version byte if possible. </summary>
    /// <typeparam name="T"> The data type to decompress the string into. </typeparam>
    /// <param name="base64"> The Base64-encoded compressed JSON serialization of data with a prepended version byte. </param>
    /// <param name="data"> The decompressed and parsed data on success or defaulted data otherwise. </param>
    /// <returns> The version byte that was prepended or <see cref="byte.MaxValue"/> on failure. </returns>
    /// <remarks> See <see cref="ToCompressedBase64{T}"/> for the compression steps. </remarks>
    public static byte FromCompressedBase64<T>(ReadOnlySpan<byte> base64, out T? data)
    {
        var version = byte.MaxValue;
        try
        {
            var bytes = new byte[System.Buffers.Text.Base64.GetMaxDecodedFromUtf8Length(base64.Length)];
            if (System.Buffers.Text.Base64.DecodeFromUtf8(base64, bytes, out _, out var length) is not OperationStatus.Done)
            {
                data = default;
                return version;
            }

            using var compressedStream = new MemoryStream(bytes);
            using var zipStream        = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var resultStream     = new MemoryStream();
            zipStream.CopyTo(resultStream);
            resultStream.Position = 0;
            version               = (byte)resultStream.ReadByte();
            data                  = JsonSerializer.Deserialize<T>(resultStream, JsonFunctions.SerializerOptions);
        }
        catch
        {
            data = default;
        }

        return version;
    }
}
