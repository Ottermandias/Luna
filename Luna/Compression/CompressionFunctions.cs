namespace Luna;

public static class CompressionFunctions
{
    /// <summary> Compress any type to a base64 encoding of its compressed JSON representation, prepended with a version byte. </summary>
    /// <typeparam name="T"> The data type to compress via JSON. </typeparam>
    /// <param name="data"> The data to serialize to JSON and compress. </param>
    /// <param name="version"> The version byte to prepend to the UTF8 JSON data. </param>
    /// <returns> An empty string on failure, otherwise the compressed, versioned data converted to Base64. </returns>
    /// <remarks> See <see cref="FromCompressedBase64{T}"/> for the decompression steps. </remarks>
    public static unsafe string ToCompressedBase64<T>(T data, byte version)
    {
        try
        {
            var       json             = JsonConvert.SerializeObject(data, Formatting.None);
            var       bytes            = Encoding.UTF8.GetBytes(json);
            using var compressedStream = new MemoryStream();
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(new ReadOnlySpan<byte>(&version, 1));
                zipStream.Write(bytes, 0, bytes.Length);
            }

            return Convert.ToBase64String(compressedStream.ToArray());
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary> Decompress a Base64 encoded string to the given type and a prepended version byte if possible. </summary>
    /// <typeparam name="T"> The data type to decompress the string into. </typeparam>
    /// <param name="base64"> The Base64-encoded compressed JSON serialization of data with a prepended version byte. </param>
    /// <param name="data"> The decompressed and parsed data on success or defaulted data otherwise. </param>
    /// <returns> The version byte that was prepended or <see cref="byte.MaxValue"/> on failure. </returns>
    /// <remarks> See <see cref="ToCompressedBase64{T}"/> for the compression steps. </remarks>
    public static byte FromCompressedBase64<T>(string base64, out T? data)
    {
        var version = byte.MaxValue;
        try
        {
            var       bytes            = Convert.FromBase64String(base64);
            using var compressedStream = new MemoryStream(bytes);
            using var zipStream        = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var resultStream     = new MemoryStream();
            zipStream.CopyTo(resultStream);
            bytes   = resultStream.ToArray();
            version = bytes[0];
            var json = Encoding.UTF8.GetString(bytes, 1, bytes.Length - 1);
            data = JsonConvert.DeserializeObject<T>(json);
        }
        catch
        {
            data = default;
        }

        return version;
    }
}
