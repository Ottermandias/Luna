using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Luna;

/// <summary> Utility functions concerning JSON serialization and deserialization. </summary>
public static class JsonFunctions
{
    /// <inheritdoc cref="TemporaryJsonObject"/>
    public static TemporaryJsonObject TemporaryObject(this Utf8JsonWriter j, ReadOnlySpan<byte> objectName)
        => new(j, objectName);

    /// <summary> The default JSON serializer options we use. </summary>
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented       = true,
        AllowTrailingCommas = true,
    };

    /// <summary> The default JSON Writer options we use. </summary>
    public static readonly JsonWriterOptions WriterOptions = new()
    {
        Indented        = true,
        IndentCharacter = ' ',
        IndentSize      = 4,
        NewLine         = "\n",
    };

    /// <summary> The default JSON Reader options we use. </summary>
    public static readonly JsonReaderOptions ReaderOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling     = JsonCommentHandling.Skip,
    };

    /// <summary> Read the bytes from a given file but strip a potential UTF8-BOM. </summary>
    /// <param name="filePath"> The full path to the file. </param>
    /// <returns> The full byte data of the file, unless it starts with a UTF8-BOM, which is stripped. </returns>
    public static ReadOnlySpan<byte> ReadUtf8Bytes(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        if (bytes.Length < 3)
            return bytes;

        // Strip UTF8 BOM
        if (bytes[0] is 0xEF && bytes[1] is 0xBB && bytes[2] is 0xBF)
            return bytes.AsSpan(3);

        return bytes;
    }

    /// <summary> Only write a string property if the string is neither null nor empty. </summary>
    /// <param name="j"> The JSON writer. </param>
    /// <param name="property"> The property name. It gets omitted entirely if <paramref name="text"/> is null or empty. </param>
    /// <param name="text"> The text value. </param>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public static void WriteNonEmptyString(this Utf8JsonWriter j, ReadOnlySpan<byte> property, string? text)
    {
        if (!string.IsNullOrEmpty(text))
            j.WriteString(property, text);
    }

    /// <summary> Only write a boolean property if the value is not equal to the specified null value. </summary>
    /// <param name="j"> The JSON writer. </param>
    /// <param name="property"> The property name. It gets omitted entirely if <paramref name="value"/> equals <paramref name="nullValue"/>. </param>
    /// <param name="value"> The value. </param>
    /// <param name="nullValue"> The null value. </param>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public static void WriteBoolIf(this Utf8JsonWriter j, ReadOnlySpan<byte> property, bool value, bool nullValue)
    {
        if (value != nullValue)
            j.WriteBoolean(property, value);
    }

    /// <summary> Only write an unsigned number property if the value is not equal to the specified null value. </summary>
    /// <param name="j"> The JSON writer. </param>
    /// <param name="property"> The property name. It gets omitted entirely if <paramref name="value"/> equals <paramref name="nullValue"/>. </param>
    /// <param name="value"> The value. </param>
    /// <param name="nullValue"> The null value. </param>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public static unsafe void WriteUnsignedIfNot<T>(this Utf8JsonWriter j, ReadOnlySpan<byte> property, T value, T nullValue)
        where T : unmanaged
    {
        switch (sizeof(T))
        {
            case 1:
            {
                var v = Unsafe.As<T, byte>(ref value);
                if (v != Unsafe.As<T, byte>(ref nullValue))
                    j.WriteNumber(property, v);
                break;
            }
            case 2:
            {
                var v = Unsafe.As<T, ushort>(ref value);
                if (v != Unsafe.As<T, ushort>(ref nullValue))
                    j.WriteNumber(property, v);
                break;
            }
            case 4:
            {
                var v = Unsafe.As<T, uint>(ref value);
                if (v != Unsafe.As<T, uint>(ref nullValue))
                    j.WriteNumber(property, v);
                break;
            }
            case 8:
            {
                var v = Unsafe.As<T, ulong>(ref value);
                if (v != Unsafe.As<T, ulong>(ref nullValue))
                    j.WriteNumber(property, v);
                break;
            }
            default: throw new ArgumentException($"The type {typeof(T)} is not supported for {nameof(WriteUnsignedIfNot)}.");
        }
    }

    /// <summary> Only write a signed number property if the value is not equal to the specified null value. </summary>
    /// <param name="j"> The JSON writer. </param>
    /// <param name="property"> The property name. It gets omitted entirely if <paramref name="value"/> equals <paramref name="nullValue"/>. </param>
    /// <param name="value"> The value. </param>
    /// <param name="nullValue"> The null value. </param>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public static unsafe void WriteSignedIfNot<T>(this Utf8JsonWriter j, ReadOnlySpan<byte> property, T value, T nullValue) where T : unmanaged
    {
        switch (sizeof(T))
        {
            case 1:
            {
                var v = Unsafe.As<T, sbyte>(ref value);
                if (v != Unsafe.As<T, sbyte>(ref nullValue))
                    j.WriteNumber(property, v);
                break;
            }
            case 2:
            {
                var v = Unsafe.As<T, short>(ref value);
                if (v != Unsafe.As<T, short>(ref nullValue))
                    j.WriteNumber(property, v);
                break;
            }
            case 4:
            {
                var v = Unsafe.As<T, int>(ref value);
                if (v != Unsafe.As<T, int>(ref nullValue))
                    j.WriteNumber(property, v);
                break;
            }
            case 8:
            {
                var v = Unsafe.As<T, long>(ref value);
                if (v != Unsafe.As<T, long>(ref nullValue))
                    j.WriteNumber(property, v);
                break;
            }
            default: throw new ArgumentException($"The type {typeof(T)} is not supported for {nameof(WriteSignedIfNot)}.");
        }
    }

    /// <summary> Try to read a file to a given object. </summary>
    /// <typeparam name="T"> The type of object to read. </typeparam>
    /// <param name="path"> The full path of the file to read. </param>
    /// <param name="ret"> The output object on success, or <c>default</c> on failure. </param>
    /// <param name="formatOutput">
    ///   An optional function to handle an exception while parsing the file. The first parameter is the passed path, the second is the thrown exception.
    ///   If this returns true, the function does not rethrow but returns false instead. If this returns false, the function rethrows the exception.
    /// </param>
    /// <returns> True on success, false if the file does not exist or could not be read. </returns>
    public static bool TryReadFileAs<T>(string path, [NotNullWhen(true)] out T? ret, Func<string, Exception, bool>? formatOutput = null)
    {
        if (!File.Exists(path))
        {
            ret = default;
            return false;
        }

        try
        {
            var data = File.ReadAllBytes(path);
            ret = JsonSerializer.Deserialize<T>(data, SerializerOptions);
            return ret is not null;
        }
        catch (Exception e)
        {
            if (formatOutput?.Invoke(path, e) is true)
            {
                ret = default;
                return false;
            }

            throw;
        }
    }

    /// <summary> Format an object to JSON and write it to a StreamWriter. </summary>
    /// <typeparam name="T"> The type of object to write. </typeparam>
    /// <param name="writer"> The StreamWriter to write to. </param>
    /// <param name="data"> The object to write. </param>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public static void WriteJson<T>(this StreamWriter writer, in T data)
        => writer.BaseStream.WriteJson(data);

    /// <summary> Format an object to JSON and write it to a stream. </summary>
    /// <typeparam name="T"> The type of object to write. </typeparam>
    /// <param name="stream"> The stream to write to. </param>
    /// <param name="data"> The object to write. </param>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public static void WriteJson<T>(this Stream stream, in T data)
    {
        var text = JsonSerializer.SerializeToUtf8Bytes(data, SerializerOptions);
        stream.Write(text);
    }

    /// <summary> Create a temporary object that tracks whether it has written any properties to omit an object if it is empty. </summary>
    /// <param name="writer"> The JSON writer. </param>
    /// <param name="objectName"> The property name for the object that may or may not be written. </param>
    public ref struct TemporaryJsonObject(Utf8JsonWriter writer, ReadOnlySpan<byte> objectName) : IDisposable
    {
        private readonly ReadOnlySpan<byte> _objectName    = objectName;
        private          bool               _startedObject = false;

        public void Dispose()
        {
            if (!_startedObject)
                return;

            writer.WriteEndObject();
        }

        /// <summary> Only write a string property if the string is neither null nor empty. </summary>
        /// <param name="property"> The property name. It gets omitted entirely if <paramref name="text"/> is null or empty. </param>
        /// <param name="text"> The text value. </param>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public void WriteNonEmptyString(ReadOnlySpan<byte> property, string? text)
        {
            if (!string.IsNullOrEmpty(text))
                writer.WriteString(property, text);
        }

        private void StartObject()
        {
            if (_startedObject)
                return;

            _startedObject = true;
            writer.WritePropertyName(_objectName);
            writer.WriteStartObject();
        }

        /// <summary> Only write a boolean property if the value is not equal to the specified null value. </summary>
        /// <param name="property"> The property name. It gets omitted entirely if <paramref name="value"/> equals <paramref name="nullValue"/>. </param>
        /// <param name="value"> The value. </param>
        /// <param name="nullValue"> The null value. </param>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public void WriteBoolIf(ReadOnlySpan<byte> property, bool value, bool nullValue)
        {
            if (value == nullValue)
                return;

            StartObject();
            writer.WriteBoolean(property, value);
        }

        /// <summary> Only write an unsigned number property if the value is not equal to the specified null value. </summary>
        /// <param name="property"> The property name. It gets omitted entirely if <paramref name="value"/> equals <paramref name="nullValue"/>. </param>
        /// <param name="value"> The value. </param>
        /// <param name="nullValue"> The null value. </param>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public unsafe void WriteUnsignedIfNot<T>(ReadOnlySpan<byte> property, T value, T nullValue)
            where T : unmanaged
        {
            switch (sizeof(T))
            {
                case 1:
                {
                    var v = Unsafe.As<T, byte>(ref value);
                    if (v == Unsafe.As<T, byte>(ref nullValue))
                        return;

                    StartObject();
                    writer.WriteNumber(property, v);
                    break;
                }
                case 2:
                {
                    var v = Unsafe.As<T, ushort>(ref value);
                    if (v == Unsafe.As<T, ushort>(ref nullValue))
                        return;

                    StartObject();
                    writer.WriteNumber(property, v);
                    break;
                }
                case 4:
                {
                    var v = Unsafe.As<T, uint>(ref value);
                    if (v == Unsafe.As<T, uint>(ref nullValue))
                        return;

                    StartObject();
                    writer.WriteNumber(property, v);
                    break;
                }
                case 8:
                {
                    var v = Unsafe.As<T, ulong>(ref value);
                    if (v == Unsafe.As<T, ulong>(ref nullValue))
                        return;

                    StartObject();
                    writer.WriteNumber(property, v);
                    break;
                }
                default: throw new ArgumentException($"The type {typeof(T)} is not supported for {nameof(WriteUnsignedIfNot)}.");
            }
        }

        /// <summary> Only write a signed number property if the value is not equal to the specified null value. </summary>
        /// <param name="property"> The property name. It gets omitted entirely if <paramref name="value"/> equals <paramref name="nullValue"/>. </param>
        /// <param name="value"> The value. </param>
        /// <param name="nullValue"> The null value. </param>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public unsafe void WriteSignedIfNot<T>(ReadOnlySpan<byte> property, T value, T nullValue) where T : unmanaged
        {
            switch (sizeof(T))
            {
                case 1:
                {
                    var v = Unsafe.As<T, sbyte>(ref value);
                    if (v == Unsafe.As<T, sbyte>(ref nullValue))
                        return;

                    StartObject();
                    writer.WriteNumber(property, v);
                    break;
                }
                case 2:
                {
                    var v = Unsafe.As<T, short>(ref value);
                    if (v == Unsafe.As<T, short>(ref nullValue))
                        return;

                    StartObject();
                    writer.WriteNumber(property, v);
                    break;
                }
                case 4:
                {
                    var v = Unsafe.As<T, int>(ref value);
                    if (v == Unsafe.As<T, int>(ref nullValue))
                        return;

                    StartObject();
                    writer.WriteNumber(property, v);
                    break;
                }
                case 8:
                {
                    var v = Unsafe.As<T, long>(ref value);
                    if (v == Unsafe.As<T, long>(ref nullValue))
                        return;

                    StartObject();
                    writer.WriteNumber(property, v);
                    break;
                }
                default: throw new ArgumentException($"The type {typeof(T)} is not supported for {nameof(WriteSignedIfNot)}.");
            }
        }
    }
}
