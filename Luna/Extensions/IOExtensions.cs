namespace Luna;

/// <summary> Extension methods for Streams and StreamWriters. </summary>
public static class StreamExtensions
{
    /// <summary> Write the bytes making up an unmanaged object to a stream. </summary>
    /// <typeparam name="T"> The unmanaged type of the object. </typeparam>
    /// <param name="stream"> The stream to write the bytes to. </param>
    /// <param name="value"> The unmanaged object to write. </param>
    public static void Write<T>(this Stream stream, in T value) where T : unmanaged
        => stream.Write(MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in value)));

    /// <summary> Write the bytes making up an unmanaged object to a binary writer. </summary>
    /// <typeparam name="T"> The unmanaged type of the object. </typeparam>
    /// <param name="writer"> The binary writer to write the bytes to. </param>
    /// <param name="value"> The unmanaged object to write. </param>
    public static void Write<T>(this BinaryWriter writer, in T value) where T : unmanaged
        => writer.Write(MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in value)));
}
