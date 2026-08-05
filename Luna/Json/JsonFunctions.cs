using System.Text.Encodings.Web;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Luna;

/// <summary> Utility functions concerning JSON serialization and deserialization. </summary>
public static partial class JsonFunctions
{
    /// <inheritdoc cref="TemporaryJsonObject"/>
    public static TemporaryJsonObject TemporaryObject(this Utf8JsonWriter j, ReadOnlySpan<byte> objectName)
        => new(j, objectName);

    /// <summary> The default JSON serializer options we use. </summary>
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented       = true,
        AllowTrailingCommas = true,
        Encoder             = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary> The unformatted JSON serializer options we use for compressed data and similar. </summary>
    public static readonly JsonSerializerOptions UnformattedSerializerOptions = new()
    {
        WriteIndented       = false,
        AllowTrailingCommas = false,
        Encoder             = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary> The default JSON Writer options we use. </summary>
    public static readonly JsonWriterOptions WriterOptions = new()
    {
        Indented        = true,
        IndentCharacter = '\t',
        IndentSize      = 1,
        NewLine         = "\n",
        Encoder         = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary> The unformatted JSON Writer options we use for compressed data and similar. </summary>
    public static readonly JsonWriterOptions UnformattedOptions = new()
    {
        SkipValidation = true,
        Indented       = false,
        IndentSize     = 0,
        NewLine        = "\n",
        Encoder        = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary> The default JSON Reader options we use. </summary>
    public static readonly JsonReaderOptions ReaderOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling     = JsonCommentHandling.Skip,
    };

    /// <summary> The default JSON Document options we use. </summary>
    public static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas      = true,
        CommentHandling          = JsonCommentHandling.Skip,
        AllowDuplicateProperties = false,
    };

    /// <summary> Read the bytes from a given file but strip a potential UTF8-BOM. </summary>
    /// <param name="filePath"> The full path to the file. </param>
    /// <param name="bytes"> The full bytes of the file, without stripped BOM. </param>
    /// <returns> The full byte data of the file, unless it starts with a UTF8-BOM, which is stripped. </returns>
    public static ReadOnlyMemory<byte> ReadUtf8Bytes(string filePath, out byte[] bytes)
    {
        bytes = File.ReadAllBytes(filePath);
        if (bytes.Length < 3)
            return bytes;

        // Strip UTF8 BOM
        if (bytes[0] is 0xEF && bytes[1] is 0xBB && bytes[2] is 0xBF)
            return bytes.AsMemory(3);

        return bytes;
    }

    /// <summary> Recovers potentially invalid JSON data using <see cref="JsonRecoveryStream"/>. </summary>
    /// <param name="originalBytes"> The potentially invalid JSON data. </param>
    /// <param name="autoTranscodeToUtf8"> Whether to also strip the UTF-8 BOM and/or transcode from UTF-16 to UTF-8. </param>
    /// <param name="allowedRecoveries"> The cases that this operation is allowed to recover from. </param>
    /// <param name="crlfReplacement"> The token to replace any raw CR LF tokens with. </param>
    /// <returns>
    ///   <list>
    ///     <item> The corrected JSON data. </item>
    ///     <item> The original encoding, if a BOM was recognized and stripped, otherwise <c>null</c>. </item>
    ///     <item> The cases that this operation has recovered from. </item>
    ///   </list>
    /// </returns>
    /// <exception cref="InvalidDataException"> Some case of invalid JSON data was encountered that cannot be recovered from. </exception>
    public static (byte[] RecoveredBytes, Encoding? BomEncoding, JsonRecoveryFlags UsedRecoveries) RecoverBytes(byte[] originalBytes,
        bool autoTranscodeToUtf8, JsonRecoveryFlags allowedRecoveries, string crlfReplacement = "\\n")
    {
        using var memoryStream = new MemoryStream(originalBytes.Length);

        var recoveryStream    = new JsonRecoveryStream(allowedRecoveries, memoryStream, crlfReplacement, true);
        var transcodingStream = autoTranscodeToUtf8 ? new AutoUtf8TranscodingStream(recoveryStream) : null;
        var outputStream      = (Stream?)transcodingStream ?? recoveryStream;
        outputStream.Write(originalBytes, 0, originalBytes.Length);
        outputStream.Close();

        return (memoryStream.ToArray(), transcodingStream?.BomEncoding, recoveryStream.UsedRecoveries);
    }

    /// <summary> Recovers potentially invalid JSON data using <see cref="JsonRecoveryStream"/>. </summary>
    /// <param name="filePath"> The potentially invalid JSON file. It will be replaced by the corrected one, if any correction happens. </param>
    /// <param name="autoTranscodeToUtf8"> Whether to also strip the UTF-8 BOM and/or transcode from UTF-16 to UTF-8. </param>
    /// <param name="allowedRecoveries"> The cases that this operation is allowed to recover from. </param>
    /// <param name="crlfReplacement"> The token to replace any raw CR LF tokens with. </param>
    /// <returns>
    ///   <list>
    ///     <item> The read (and potentially recovered) byte data. </item>
    ///     <item> Whether this operation replaced the given file by a corrected one. </item>
    ///     <item> The original encoding, if a BOM was recognized and stripped, otherwise <c>null</c>. </item>
    ///     <item> The cases that this operation has recovered from. </item>
    ///   </list>
    /// </returns>
    /// <exception cref="InvalidDataException"> Some case of invalid JSON data was encountered that cannot be recovered from. </exception>
    public static (byte[] FileData, bool FileModified, Encoding? BomEncoding, JsonRecoveryFlags UsedRecoveries) RecoverFile(string filePath,
        bool autoTranscodeToUtf8, JsonRecoveryFlags allowedRecoveries, string crlfReplacement = "\\n")
    {
        var originalBytes = File.ReadAllBytes(filePath);
        var (recoveredBytes, bomEncoding, usedRecoveries) =
            RecoverBytes(originalBytes, autoTranscodeToUtf8, allowedRecoveries, crlfReplacement);
        if (originalBytes.SequenceEqual(recoveredBytes))
            return (recoveredBytes, false, bomEncoding, usedRecoveries);

        File.Move(filePath, filePath + ".bak");
        File.WriteAllBytes(filePath, recoveredBytes);
        return (recoveredBytes, true, bomEncoding, usedRecoveries);
    }

    /// <inheritdoc cref="RecoverFile"/>
    public static async Task<(byte[] FileData, bool FileModified, Encoding? BomEncoding, JsonRecoveryFlags UsedRecoveries)> RecoverFileAsync(
        string filePath,
        bool autoTranscodeToUtf8, JsonRecoveryFlags allowedRecoveries, string crlfReplacement = "\\n")
    {
        var originalBytes = await File.ReadAllBytesAsync(filePath);
        var (recoveredBytes, bomEncoding, usedRecoveries) =
            RecoverBytes(originalBytes, autoTranscodeToUtf8, allowedRecoveries, crlfReplacement);
        if (originalBytes.SequenceEqual(recoveredBytes))
            return (recoveredBytes, false, bomEncoding, usedRecoveries);

        File.Move(filePath, filePath + ".bak");
        await File.WriteAllBytesAsync(filePath, recoveredBytes);
        return (recoveredBytes, true, bomEncoding, usedRecoveries);
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
}
