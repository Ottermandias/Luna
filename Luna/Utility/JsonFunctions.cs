using System.Text.Json;
using ImSharp;
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

    /// <summary> The default JSON Writer options we use. </summary>
    public static readonly JsonWriterOptions UnformattedOptions = new()
    {
        SkipValidation = true,
        Indented        = false,
        IndentSize      = 0,
        NewLine         = "\n",
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
    /// <returns> The full byte data of the file, unless it starts with a UTF8-BOM, which is stripped. </returns>
    public static ReadOnlyMemory<byte> ReadUtf8Bytes(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
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
    /// <returns>
    ///   <list>
    ///     <item> The corrected JSON data. </item>
    ///     <item> The original encoding, if a BOM was recognized and stripped, otherwise <c>null</c>. </item>
    ///     <item> The cases that this operation has recovered from. </item>
    ///   </list>
    /// </returns>
    /// <exception cref="InvalidDataException"> Some case of invalid JSON data was encountered that cannot be recovered from. </exception>
    public static (byte[] RecoveredBytes, Encoding? BomEncoding, JsonRecoveryFlags UsedRecoveries) RecoverBytes(byte[] originalBytes,
        bool autoTranscodeToUtf8, JsonRecoveryFlags allowedRecoveries)
    {
        using var memoryStream = new MemoryStream(originalBytes.Length);

        var transcodingStream = autoTranscodeToUtf8 ? new AutoUtf8TranscodingStream(memoryStream, true) : null;
        var recoveryStream = transcodingStream is not null
            ? new JsonRecoveryStream(allowedRecoveries, transcodingStream)
            : new JsonRecoveryStream(allowedRecoveries, memoryStream, true);
        recoveryStream.Write(originalBytes, 0, originalBytes.Length);
        recoveryStream.Close();

        return (memoryStream.ToArray(), transcodingStream?.BomEncoding, recoveryStream.UsedRecoveries);
    }

    /// <summary> Recovers potentially invalid JSON data using <see cref="JsonRecoveryStream"/>. </summary>
    /// <param name="filePath"> The potentially invalid JSON file. It will be replaced by the corrected one, if any correction happens. </param>
    /// <param name="autoTranscodeToUtf8"> Whether to also strip the UTF-8 BOM and/or transcode from UTF-16 to UTF-8. </param>
    /// <param name="allowedRecoveries"> The cases that this operation is allowed to recover from. </param>
    /// <returns>
    ///   <list>
    ///     <item> Whether this operation replaced the given file by a corrected one. </item>
    ///     <item> The original encoding, if a BOM was recognized and stripped, otherwise <c>null</c>. </item>
    ///     <item> The cases that this operation has recovered from. </item>
    ///   </list>
    /// </returns>
    /// <exception cref="InvalidDataException"> Some case of invalid JSON data was encountered that cannot be recovered from. </exception>
    public static (bool FileModified, Encoding? BomEncoding, JsonRecoveryFlags UsedRecoveries) RecoverFile(string filePath,
        bool autoTranscodeToUtf8, JsonRecoveryFlags allowedRecoveries)
    {
        var originalBytes = File.ReadAllBytes(filePath);
        var (recoveredBytes, bomEncoding, usedRecoveries) = RecoverBytes(originalBytes, autoTranscodeToUtf8, allowedRecoveries);
        if (originalBytes.SequenceEqual(recoveredBytes))
            return (false, bomEncoding, usedRecoveries);

        File.Move(filePath, filePath + ".bak");
        File.WriteAllBytes(filePath, recoveredBytes);
        return (true, bomEncoding, usedRecoveries);
    }

    /// <inheritdoc cref="RecoverFile"/>
    public static async Task<(bool FileModified, Encoding? BomEncoding, JsonRecoveryFlags UsedRecoveries)> RecoverFileAsync(string filePath,
        bool autoTranscodeToUtf8, JsonRecoveryFlags allowedRecoveries)
    {
        var originalBytes = await File.ReadAllBytesAsync(filePath);
        var (recoveredBytes, bomEncoding, usedRecoveries) = RecoverBytes(originalBytes, autoTranscodeToUtf8, allowedRecoveries);
        if (originalBytes.SequenceEqual(recoveredBytes))
            return (false, bomEncoding, usedRecoveries);

        File.Move(filePath, filePath + ".bak");
        await File.WriteAllBytesAsync(filePath, recoveredBytes);
        return (true, bomEncoding, usedRecoveries);
    }

    /// <summary> Return values for peeking a property. </summary>
    public enum PeekError
    {
        /// <summary> Successfully parsed the requested property. </summary>
        Success,

        /// <summary> The requested property existed, but  could not be parsed. </summary>
        Invalid,

        /// <summary> The requested property did not exist. </summary>
        Missing,

        /// <summary> The JSON was malformed. </summary>
        Malformed,
    }

    /// <param name="reader"> The reader. If the requested property is the first encountered property, its position will be incremented, otherwise it will stay the same. </param>
    extension(ref Utf8JsonReader reader)
    {
        /// <summary> Read an enumeration property type from a single object regardless of property order in this object. </summary>
        /// <param name="property"> The name of the requested property. </param>
        /// <param name="value"> The parsed value for that property on success or default on failure. </param>
        /// <returns> The reason for failure or success. </returns>
        /// <remarks> Assumes a starting point on a StartObject. </remarks>
        public PeekError TryPeekEnumProperty<TEnum>(ReadOnlySpan<byte> property, out TEnum value)
            where TEnum : unmanaged, Enum
        {
            Debug.Assert(reader.TokenType is JsonTokenType.StartObject);

            // We create a copy of the reader to be independent of the order of properties.
            var copy = reader;
            value = default;
            var nonEnumPropertyEncountered = false;
            var success                    = false;
            // Read all tokens.
            while (copy.Read())
            {
                // If the token is a property, check if it is the type property.
                if (copy.TokenType is JsonTokenType.PropertyName)
                {
                    if (copy.ValueTextEquals(property))
                    {
                        // Type properties will be parsed, If this all succeeds, break out of the loop.
                        if (!copy.Read() || !copy.TryReadUtf8String(out var text))
                            return PeekError.Invalid;

                        if (!EnumExtensions.Parse(text.Value, out value))
                            return PeekError.Invalid;

                        success = true;
                        break;
                    }

                    // If we encounter a different property first, skip it and mark that.
                    copy.Skip();
                    nonEnumPropertyEncountered = true;
                }
                // If we encounter a different object, skip it and mark that. (This should be invalid JSON?)
                else if (copy.TokenType is JsonTokenType.StartObject)
                {
                    copy.Skip();
                    nonEnumPropertyEncountered = true;
                }
                // This is the end of the current object, so it has no type property.
                else if (copy.TokenType is JsonTokenType.EndObject)
                {
                    return PeekError.Missing;
                }
            }

            // We iterated all tokens without encountering a type property or an end.
            if (!success)
                return PeekError.Malformed;

            // If we did not skip any properties, we can use the copied readers position.
            if (!nonEnumPropertyEncountered)
                reader = copy;

            return PeekError.Success;
        }

        /// <summary> Read the UTF8 string at the current token, unescaped, into an UTF8 string without re-encoding. </summary>
        /// <param name="text"> On success, the UTF8 string. </param>
        /// <returns> True on success, false if the current token is not a string. </returns>
        public bool TryReadUtf8String([NotNullWhen(true)] out StringU8? text)
        {
            if (reader.TokenType is not JsonTokenType.String and not JsonTokenType.PropertyName)
            {
                text = null;
                return false;
            }

            if (!reader.HasValueSequence)
            {
                var array = new byte[reader.ValueSpan.Length + 1];
                array[reader.ValueSpan.Length] = 0;
                reader.ValueSpan.CopyTo(array);
                text = StringU8.CreateUnchecked(array.AsMemory(..^1));
                return true;
            }

            var seq    = reader.ValueSequence;
            var length = 1;
            foreach (var span in seq)
                length += span.Length;

            var ret = new byte[length];
            ret[^1] = 0;
            var tmp = ret.AsMemory();
            foreach (var span in seq)
            {
                span.CopyTo(tmp);
                tmp = tmp[span.Length..];
            }

            text = StringU8.CreateUnchecked(ret.AsMemory(..^1));
            return true;
        }

        /// <summary> Read the UTF8 string at the current token, unescaped, and parse it into an enumeration value. </summary>
        /// <typeparam name="TEnum"> The enumeration type. </typeparam>
        /// <param name="value"> On success, the parsed enumeration value. </param>
        /// <returns> True on success, false if the current token is not a string or the string does not correspond to a known enumeration value. </returns>
        public bool TryReadTextEnum<TEnum>(out TEnum value) where TEnum : unmanaged, Enum
        {
            if (!reader.TryReadUtf8String(out var text))
            {
                value = default;
                return false;
            }

            return EnumExtensions.Parse(text.Value, out value);
        }

        /// <summary> Skip to the end of the current object. </summary>
        public void SkipCurrentObject()
        {
            var currentDepth = reader.CurrentDepth;
            while (reader.Read())
            {
                if (currentDepth > reader.CurrentDepth)
                    return;
            }
        }

        /// <summary> Read the current token and parse it to a bool if possible. </summary>
        /// <param name="value"> On success, the parsed boolean value. </param>
        /// <returns> True on success, false if the current token is not a boolean token or a string that is equal to 'True', 'true', 'False' or 'false'. </returns>
        public bool TryReadBoolean(out bool value)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.True:
                    value = true;
                    return true;
                case JsonTokenType.False:
                    value = false;
                    return true;
                case JsonTokenType.String when reader.ValueTextEquals("true"u8) || reader.ValueTextEquals("True"):
                    value = true;
                    return true;
                case JsonTokenType.String when reader.ValueTextEquals("false"u8) || reader.ValueTextEquals("False"):
                    value = false;
                    return true;
            }

            value = false;
            return false;
        }
    }

    /// <param name="parent"> The parent JSON element. </param>
    extension(in JsonElement parent)
    {
        /// <summary> Try to read a property ensuring it is an array. </summary>
        /// <param name="property"> The name of the queried property. </param>
        /// <param name="array"> The queried property on success. </param>
        /// <returns> True if the property exists and is an array type. </returns>
        public bool TryReadArray(ReadOnlySpan<byte> property, out JsonElement array)
        {
            Debug.Assert(parent.ValueKind is JsonValueKind.Object, "JSON parent value is not an object.");
            if (!parent.TryGetProperty(property, out array))
                return false;

            return array.ValueKind is JsonValueKind.Array;
        }

        /// <summary> Try to read a property ensuring it is an object. </summary>
        /// <param name="property"> The name of the queried property. </param>
        /// <param name="object"> The queried property on success. </param>
        /// <returns> True if the property exists and is an object type. </returns>
        public bool TryReadObject(ReadOnlySpan<byte> property, out JsonElement @object)
        {
            Debug.Assert(parent.ValueKind is JsonValueKind.Object, "JSON parent value is not an object.");
            if (!parent.TryGetProperty(property, out @object))
                return false;

            return @object.ValueKind is JsonValueKind.Object;
        }

        /// <summary> Try to read a property's value by name. </summary>
        /// <param name="property"> The name of the queried property. </param>
        /// <param name="value"> The returned value on success, <c>default</c> otherwise. </param>
        /// <returns> True if the property exists and can be parsed to the requested value type. </returns>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public bool TryReadProperty(ReadOnlySpan<byte> property, [NotNullWhen(true)] out string? value)
        {
            Debug.Assert(parent.ValueKind is JsonValueKind.Object, "JSON parent value is not an object.");
            if (!parent.TryGetProperty(property, out var element) || element.GetString() is not { } text)
            {
                value = null;
                return false;
            }

            value = text;
            return true;
        }

        /// <inheritdoc cref="TryReadProperty(in JsonElement,ReadOnlySpan{byte},out string?)"/>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public bool TryReadProperty(ReadOnlySpan<byte> property, out sbyte value)
        {
            Debug.Assert(parent.ValueKind is JsonValueKind.Object, "JSON parent value is not an object.");
            if (parent.TryGetProperty(property, out var element) && !element.TryGetSByte(out value))
                return true;

            value = 0;
            return false;
        }

        /// <inheritdoc cref="TryReadProperty(in JsonElement,ReadOnlySpan{byte},out string?)"/>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public bool TryReadProperty(ReadOnlySpan<byte> property, out byte value)
        {
            Debug.Assert(parent.ValueKind is JsonValueKind.Object, "JSON parent value is not an object.");
            if (parent.TryGetProperty(property, out var element) && !element.TryGetByte(out value))
                return true;

            value = 0;
            return false;
        }

        /// <inheritdoc cref="TryReadProperty(in JsonElement,ReadOnlySpan{byte},out string?)"/>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public bool TryReadProperty(ReadOnlySpan<byte> property, out short value)
        {
            Debug.Assert(parent.ValueKind is JsonValueKind.Object, "JSON parent value is not an object.");
            if (parent.TryGetProperty(property, out var element) && !element.TryGetInt16(out value))
                return true;

            value = 0;
            return false;
        }

        /// <inheritdoc cref="TryReadProperty(in JsonElement,ReadOnlySpan{byte},out string?)"/>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public bool TryReadProperty(ReadOnlySpan<byte> property, out ushort value)
        {
            Debug.Assert(parent.ValueKind is JsonValueKind.Object, "JSON parent value is not an object.");
            if (parent.TryGetProperty(property, out var element) && !element.TryGetUInt16(out value))
                return true;

            value = 0;
            return false;
        }

        /// <inheritdoc cref="TryReadProperty(in JsonElement,ReadOnlySpan{byte},out string?)"/>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public bool TryReadProperty(ReadOnlySpan<byte> property, out int value)
        {
            Debug.Assert(parent.ValueKind is JsonValueKind.Object, "JSON parent value is not an object.");
            if (parent.TryGetProperty(property, out var element) && !element.TryGetInt32(out value))
                return true;

            value = 0;
            return false;
        }

        /// <inheritdoc cref="TryReadProperty(in JsonElement,ReadOnlySpan{byte},out string?)"/>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public bool TryReadProperty(ReadOnlySpan<byte> property, out uint value)
        {
            Debug.Assert(parent.ValueKind is JsonValueKind.Object, "JSON parent value is not an object.");
            if (parent.TryGetProperty(property, out var element) && !element.TryGetUInt32(out value))
                return true;

            value = 0;
            return false;
        }

        /// <inheritdoc cref="TryReadProperty(in JsonElement,ReadOnlySpan{byte},out string?)"/>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public bool TryReadProperty(ReadOnlySpan<byte> property, out long value)
        {
            Debug.Assert(parent.ValueKind is JsonValueKind.Object, "JSON parent value is not an object.");
            if (parent.TryGetProperty(property, out var element) && !element.TryGetInt64(out value))
                return true;

            value = 0;
            return false;
        }

        /// <inheritdoc cref="TryReadProperty(in JsonElement,ReadOnlySpan{byte},out string?)"/>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public bool TryReadProperty(ReadOnlySpan<byte> property, out ulong value)
        {
            Debug.Assert(parent.ValueKind is JsonValueKind.Object, "JSON parent value is not an object.");
            if (parent.TryGetProperty(property, out var element) && !element.TryGetUInt64(out value))
                return true;

            value = 0;
            return false;
        }

        /// <inheritdoc cref="TryReadProperty(in JsonElement,ReadOnlySpan{byte},out string?)"/>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public bool TryReadProperty(ReadOnlySpan<byte> property, out float value)
        {
            Debug.Assert(parent.ValueKind is JsonValueKind.Object, "JSON parent value is not an object.");
            if (parent.TryGetProperty(property, out var element) && !element.TryGetSingle(out value))
                return true;

            value = 0;
            return false;
        }

        /// <inheritdoc cref="TryReadProperty(in JsonElement,ReadOnlySpan{byte},out string?)"/>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public bool TryReadProperty(ReadOnlySpan<byte> property, out double value)
        {
            Debug.Assert(parent.ValueKind is JsonValueKind.Object, "JSON parent value is not an object.");
            if (parent.TryGetProperty(property, out var element) && !element.TryGetDouble(out value))
                return true;

            value = 0;
            return false;
        }

        /// <inheritdoc cref="TryReadProperty(in JsonElement,ReadOnlySpan{byte},out string?)"/>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public bool TryReadProperty(ReadOnlySpan<byte> property, out Guid value)
        {
            Debug.Assert(parent.ValueKind is JsonValueKind.Object, "JSON parent value is not an object.");
            if (parent.TryGetProperty(property, out var element) && !element.TryGetGuid(out value))
                return true;

            value = Guid.Empty;
            return false;
        }

        /// <inheritdoc cref="TryReadProperty(in JsonElement,ReadOnlySpan{byte},out string?)"/>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public bool TryReadProperty(ReadOnlySpan<byte> property, out DateTime value)
        {
            Debug.Assert(parent.ValueKind is JsonValueKind.Object, "JSON parent value is not an object.");
            if (parent.TryGetProperty(property, out var element) && !element.TryGetDateTime(out value))
                return true;

            value = DateTime.UnixEpoch;
            return false;
        }

        /// <inheritdoc cref="TryReadProperty(in JsonElement,ReadOnlySpan{byte},out string?)"/>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public bool TryReadProperty(ReadOnlySpan<byte> property, out DateTimeOffset value)
        {
            Debug.Assert(parent.ValueKind is JsonValueKind.Object, "JSON parent value is not an object.");
            if (parent.TryGetProperty(property, out var element) && !element.TryGetDateTimeOffset(out value))
                return true;

            value = DateTimeOffset.UnixEpoch;
            return false;
        }
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
