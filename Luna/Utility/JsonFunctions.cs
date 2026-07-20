using System.Text.Encodings.Web;
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

    /// <summary> The default JSON Writer options we use. </summary>
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
    extension(scoped ref Utf8JsonReader reader)
    {
        /// <summary> Create a sub-reader utility on the current value token, copying the reader at the current location. </summary>
        /// <remarks>
        ///   Use <see cref="Utf8JsonObjectReader.Read"/> for reading within the current value.<br/>
        ///   If the current value is an object or an array, this will return true until the end of this object is reached.<br/>
        ///   If it is a value type (null, a number, a string, or true or false), it will never return true.<br/>
        ///   If it is any other token type, it will throw on construction.<br/>
        ///   After returning false, if it started on an object or array, the cursor will be located on the <see cref="JsonTokenType.EndObject"/> or <see cref="JsonTokenType.EndArray"/> token.<br/>
        ///   It will also throw if it drops out of 
        /// </remarks>
        public Utf8JsonObjectReader CreateObjectReader()
            => new(reader);

        /// <summary> Create a sub-reader utility on the current value token. </summary>
        /// <remarks>
        ///   Use <see cref="Utf8JsonObjectReader.Read"/> for reading within the current value.<br/>
        ///   If the current value is an object or an array, this will return true until the end of this object is reached.<br/>
        ///   If it is a value type (null, a number, a string, or true or false), it will never return true.<br/>
        ///   If it is any other token type, it will throw on construction. <br/>
        ///   After returning false, if it started on an object or array, the cursor will be located on the <see cref="JsonTokenType.EndObject"/> or <see cref="JsonTokenType.EndArray"/> token.
        /// </remarks>
        public Utf8JsonObjectLimit CreateObjectLimit()
            => new(reader);

        /// <summary> Check whether the current property name token corresponds to the given property and has a following value. </summary>
        /// <param name="propertyName"> The property name to check for. </param>
        /// <returns> True if the property names correspond, false otherwise. </returns>
        /// <exception cref="JsonException"> If the property matches but has no following value token to read. </exception>
        [MethodImpl(ImSharpConfiguration.OptInl)]
        public bool CheckProperty(ReadOnlySpan<byte> propertyName)
        {
            Debug.Assert(reader.TokenType is JsonTokenType.PropertyName);
            if (!reader.ValueTextEquals(propertyName))
                return false;

            if (!reader.Read())
                throw new JsonException($"Unexpected end after property {Encoding.UTF8.GetString(propertyName)}.");

            return true;
        }

        /// <summary> Check whether the current property name token corresponds to the given property and has a following value. </summary>
        /// <param name="propertyName"> The property name to check for. </param>
        /// <param name="objectReader"> An object reader for the current object. </param>
        /// <param name="allowNull"> Whether null is allowed for the object value. </param>
        /// <returns> True if the property names correspond, false otherwise. </returns>
        /// <exception cref="JsonException"> If the property matches but has no following value token to read or is not the start of an object. </exception>
        [MethodImpl(ImSharpConfiguration.OptInl)]
        public bool ObjectProperty(ReadOnlySpan<byte> propertyName, out Utf8JsonObjectLimit objectReader, bool allowNull = false)
        {
            Debug.Assert(reader.TokenType is JsonTokenType.PropertyName);
            if (!reader.ValueTextEquals(propertyName))
            {
                objectReader = default;
                return false;
            }

            if (!reader.Read())
                throw new JsonException($"Unexpected end after property {Encoding.UTF8.GetString(propertyName)}.");

            if (reader.TokenType is JsonTokenType.StartObject)
            {
                objectReader = new Utf8JsonObjectLimit(reader);
                return true;
            }

            if (allowNull && reader.TokenType is JsonTokenType.Null)
            {
                objectReader = new Utf8JsonObjectLimit(reader);
                return true;
            }

            throw new JsonException($"Unexpected token {reader.TokenType} for expected object.");
        }

        /// <summary> Check whether the current property name token corresponds to the given property and has a following value. </summary>
        /// <param name="propertyName"> The property name to check for. </param>
        /// <param name="arrayReader"> An object reader for the current array. </param>
        /// <param name="allowNull"> Whether null is allowed for the array value. </param>
        /// <returns> True if the property names correspond, false otherwise. </returns>
        /// <exception cref="JsonException"> If the property matches but has no following value token to read or is not the start of an object or null if allowed. </exception>
        [MethodImpl(ImSharpConfiguration.OptInl)]
        public bool ArrayProperty(ReadOnlySpan<byte> propertyName, out Utf8JsonObjectLimit arrayReader, bool allowNull = false)
        {
            Debug.Assert(reader.TokenType is JsonTokenType.PropertyName);
            if (!reader.ValueTextEquals(propertyName))
            {
                arrayReader = default;
                return false;
            }

            if (!reader.Read())
                throw new JsonException($"Unexpected end after property {Encoding.UTF8.GetString(propertyName)}.");

            if (reader.TokenType is JsonTokenType.StartArray)
            {
                arrayReader = new Utf8JsonObjectLimit(reader);
                return true;
            }

            if (allowNull && reader.TokenType is JsonTokenType.Null)
            {
                arrayReader = new Utf8JsonObjectLimit(reader);
                return true;
            }

            throw new JsonException($"Unexpected token {reader.TokenType} for expected object.");
        }

        /// <summary> Check whether the current property name token corresponds to the given property and has boolean value. </summary>
        /// <param name="propertyName"> The property name to check for. </param>
        /// <param name="value"> The parsed boolean on success. </param>
        /// <returns> True if the property names correspond, false otherwise. </returns>
        /// <exception cref="JsonException"> If the property matches but has no following value token to read, or if the value token is not a boolean. </exception>
        [MethodImpl(ImSharpConfiguration.OptInl)]
        public bool BoolProperty(ReadOnlySpan<byte> propertyName, out bool value)
        {
            Debug.Assert(reader.TokenType is JsonTokenType.PropertyName);
            if (!reader.ValueTextEquals(propertyName))
                return value = false;

            if (!reader.Read())
                throw new JsonException($"Unexpected end after boolean property {Encoding.UTF8.GetString(propertyName)}.");

            if (!reader.TryReadBoolean(out value))
                throw new JsonException(
                    $"Unexpected {reader.TokenType} value for boolean property {Encoding.UTF8.GetString(propertyName)}.");

            return true;
        }

        /// <summary> Check whether the current property name token corresponds to the given property and has numerical value. </summary>
        /// <param name="propertyName"> The property name to check for. </param>
        /// <param name="value"> The parsed number on success. </param>
        /// <returns> True if the property names correspond, false otherwise. </returns>
        /// <exception cref="JsonException"> If the property matches but has no following value token to read, or if the value token is not a number. </exception>
        [MethodImpl(ImSharpConfiguration.OptInl)]
        public bool NumberProperty<TNumber>(ReadOnlySpan<byte> propertyName, out TNumber value)
            where TNumber : unmanaged, INumber<TNumber>
        {
            Debug.Assert(reader.TokenType is JsonTokenType.PropertyName);
            if (!reader.ValueTextEquals(propertyName))
            {
                value = default!;
                return false;
            }

            if (!reader.Read())
                throw new JsonException($"Unexpected end after numeric property {Encoding.UTF8.GetString(propertyName)}.");

            return reader.TryReadNumber(out value)
                ? true
                : throw new JsonException(
                    $"Unexpected {reader.TokenType} value for numeric property {Encoding.UTF8.GetString(propertyName)}.");
        }

        /// <summary> Check whether the current property name token corresponds to the given property and has numerical value. </summary>
        /// <param name="propertyName"> The property name to check for. </param>
        /// <param name="value"> The parsed number on success. </param>
        /// <returns> True if the property names correspond, false otherwise. </returns>
        /// <exception cref="JsonException"> If the property matches but has no following value token to read, or if the value token is not a number. </exception>
        [MethodImpl(ImSharpConfiguration.OptInl)]
        public bool NumberProperty<TNumber>(ReadOnlySpan<byte> propertyName, out TNumber? value)
            where TNumber : unmanaged, INumber<TNumber>
        {
            Debug.Assert(reader.TokenType is JsonTokenType.PropertyName);
            if (!reader.ValueTextEquals(propertyName))
            {
                value = null;
                return false;
            }

            if (!reader.Read())
                throw new JsonException($"Unexpected end after numeric property {Encoding.UTF8.GetString(propertyName)}.");

            if (reader.TokenType is JsonTokenType.Null)
            {
                value = null;
                return true;
            }

            if (reader.TryReadNumber(out TNumber number))
            {
                value = number;
                return true;
            }

            throw new JsonException(
                $"Unexpected {reader.TokenType} value for numeric property {Encoding.UTF8.GetString(propertyName)}.");
        }

        /// <summary> Check whether the current property name token corresponds to the given property and has a textual enumeration value. </summary>
        /// <param name="propertyName"> The property name to check for. </param>
        /// <param name="value"> The parsed enumeration value on success. </param>
        /// <returns> True if the property names correspond, false otherwise. </returns>
        /// <exception cref="JsonException"> If the property matches but has no following value token to read, or if the value token is not parsable as the given enumeration. </exception>
        [MethodImpl(ImSharpConfiguration.OptInl)]
        public bool EnumProperty<TEnum>(ReadOnlySpan<byte> propertyName, out TEnum value)
            where TEnum : unmanaged, Enum
        {
            Debug.Assert(reader.TokenType is JsonTokenType.PropertyName);
            if (!reader.ValueTextEquals(propertyName))
            {
                value = default!;
                return false;
            }

            if (!reader.Read())
                throw new JsonException($"Unexpected end after enum property {Encoding.UTF8.GetString(propertyName)}.");

            return reader.TryReadTextEnum(out value)
                ? true
                : throw new JsonException(
                    $"Unexpected {reader.TokenType} value for enum property {Encoding.UTF8.GetString(propertyName)}.");
        }

        /// <summary> Check whether the current property name token corresponds to the given property and has a textual enumeration value. </summary>
        /// <param name="propertyName"> The property name to check for. </param>
        /// <param name="value"> The parsed enumeration value on success. </param>
        /// <param name="allowNull"> Whether null is allowed for the string value. </param>
        /// <returns> True if the property names correspond, false otherwise. </returns>
        /// <exception cref="JsonException"> If the property matches but has no following value token to read, or if the value token is not parsable as the given enumeration. </exception>
        [MethodImpl(ImSharpConfiguration.OptInl)]
        public bool StringProperty(ReadOnlySpan<byte> propertyName, out string? value, bool allowNull = false)
        {
            Debug.Assert(reader.TokenType is JsonTokenType.PropertyName);
            if (!reader.ValueTextEquals(propertyName))
            {
                value = null;
                return false;
            }

            if (!reader.Read())
                throw new JsonException($"Unexpected end after string property {Encoding.UTF8.GetString(propertyName)}.");

            return reader.TryReadString(out value, allowNull: allowNull)
                ? true
                : throw new JsonException(
                    $"Unexpected {reader.TokenType} value for string property {Encoding.UTF8.GetString(propertyName)}.");
        }

        /// <summary> Check whether the current property name token corresponds to the given property and has a textual enumeration value. </summary>
        /// <param name="propertyName"> The property name to check for. </param>
        /// <param name="value"> The parsed enumeration value on success. </param>
        /// <param name="allowNull"> Whether null is allowed for the string value. </param>
        /// <returns> True if the property names correspond, false otherwise. </returns>
        /// <exception cref="JsonException"> If the property matches but has no following value token to read, or if the value token is not parsable as the given enumeration. </exception>
        [MethodImpl(ImSharpConfiguration.OptInl)]
        public bool StringProperty(ReadOnlySpan<byte> propertyName, out StringU8 value, bool allowNull = false)
        {
            Debug.Assert(reader.TokenType is JsonTokenType.PropertyName);
            if (!reader.ValueTextEquals(propertyName))
            {
                value = StringU8.Null;
                return false;
            }

            if (!reader.Read())
                throw new JsonException($"Unexpected end after string property {Encoding.UTF8.GetString(propertyName)}.");

            return reader.TryReadUtf8String(out value, allowNull)
                ? true
                : throw new JsonException(
                    $"Unexpected {reader.TokenType} value for string property {Encoding.UTF8.GetString(propertyName)}.");
        }

        /// <summary> Check whether the current property name token corresponds to the given property and is a valid GUID value. </summary>
        /// <param name="propertyName"> The property name to check for. </param>
        /// <param name="value"> The parsed GUID value on success. </param>
        /// <returns> True if the property names correspond, false otherwise. </returns>
        /// <exception cref="JsonException"> If the property matches but has no following value token to read, or if the value token is not parsable to a GUID. </exception>
        [MethodImpl(ImSharpConfiguration.OptInl)]
        public bool GuidProperty(ReadOnlySpan<byte> propertyName, out Guid value)
        {
            Debug.Assert(reader.TokenType is JsonTokenType.PropertyName);
            if (!reader.ValueTextEquals(propertyName))
            {
                value = Guid.Empty;
                return false;
            }

            if (!reader.Read())
                throw new JsonException($"Unexpected end after GUID property {Encoding.UTF8.GetString(propertyName)}.");

            return reader.TryGetGuid(out value)
                ? true
                : throw new JsonException(
                    $"Unexpected {reader.TokenType} value for string property {Encoding.UTF8.GetString(propertyName)}.");
        }

        /// <summary> Read an enumeration property type from a single object regardless of property order in this object. </summary>
        /// <param name="property"> The name of the requested property. </param>
        /// <param name="value"> The parsed value for that property on success or default on failure. </param>
        /// <returns> The reason for failure or success. </returns>
        /// <remarks> Assumes a starting point on a StartObject. </remarks>
        public PeekError TryPeekEnumProperty<TEnum>(ReadOnlySpan<byte> property, out TEnum value)
            where TEnum : unmanaged, Enum
        {
            var peek = reader.TryPeekStringProperty(property, out var text);
            if (peek is not PeekError.Success)
            {
                value = default;
                return peek;
            }

            return EnumExtensions.Parse(text, out value) ? PeekError.Success : PeekError.Invalid;
        }


        /// <summary> Read a string property from a single object regardless of property order in this object. </summary>
        /// <param name="property"> The name of the requested property. </param>
        /// <param name="value"> The parsed value for that property on success or default on failure. </param>
        /// <returns> The reason for failure or success. </returns>
        /// <remarks> Assumes a starting point on a StartObject. </remarks>
        public PeekError TryPeekStringProperty(ReadOnlySpan<byte> property, out StringU8 value)
        {
            // We create a copy of the reader to be independent of the order of properties.
            var objectReader = reader.CreateObjectReader();
            value = StringU8.Empty;
            var nonEnumPropertyEncountered = false;
            var success                    = false;
            // Read all tokens.
            while (objectReader.Read())
            {
                // If the token is a property, check if it is the type property.
                if (objectReader.Reader.TokenType is JsonTokenType.PropertyName)
                {
                    if (objectReader.Reader.ValueTextEquals(property))
                    {
                        // Type properties will be parsed, If this all succeeds, break out of the loop.
                        if (!objectReader.Read() || !objectReader.Reader.TryReadUtf8String(out value))
                            return PeekError.Invalid;

                        success = true;
                        break;
                    }

                    // If we encounter a different property first, skip it and mark that.
                    objectReader.Reader.Skip();
                    nonEnumPropertyEncountered = true;
                }
                // If we encounter a different object, skip it and mark that. (This should be invalid JSON?)
                else if (objectReader.Reader.TokenType is JsonTokenType.StartObject)
                {
                    objectReader.Reader.Skip();
                    nonEnumPropertyEncountered = true;
                }
            }

            // We iterated all tokens without encountering a type property or an end.
            if (!success)
                return objectReader.Reader.TokenType is JsonTokenType.EndObject ? PeekError.Missing : PeekError.Malformed;

            // If we did not skip any properties, we can use the copied readers position.
            if (!nonEnumPropertyEncountered)
                reader = objectReader.Reader;

            return PeekError.Success;
        }

        /// <summary> Read the UTF8 string at the current token, unescaped, into an UTF8 string without re-encoding. </summary>
        /// <param name="text"> On success, the UTF8 string. </param>
        /// <param name="allowNull"> Whether a null token is allowed to be parsed into null, or is a failure to parse. </param>
        /// <returns> True on success, false if the current token is not a string. </returns>
        public bool TryReadUtf8String(out StringU8 text, bool allowNull = false)
        {
            if (reader.TokenType is not JsonTokenType.String and not JsonTokenType.PropertyName)
            {
                text = StringU8.Null;
                return allowNull && reader.TokenType is JsonTokenType.Null;
            }

            var array = new byte[reader.ValueSpan.Length + 1];
            var bytes = reader.CopyString(array);
            array[bytes] = 0;
            text         = StringU8.CreateUnchecked(array.AsMemory(0, bytes));
            return true;
        }

        /// <summary> Read the string at the current token and return it as a UTF16 string. </summary>
        /// <param name="text"> Returns the parsed text, <paramref name="default"/> on failure to parse, or <c>null</c> if <paramref name="allowNull"/> and the token is null.</param>
        /// <param name="default"> The default text to set on failure to parse. </param>
        /// <param name="allowNull"> Whether a null token is allowed to be parsed into null, or is a failure to parse. </param>
        /// <returns> True if the string was successfully parsed or was a null token with <paramref name="allowNull"/>. </returns>
        public bool TryReadString(out string? text, string @default = "", bool allowNull = false)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                case JsonTokenType.String:
                    text = reader.GetString();
                    if (text is null)
                    {
                        text = @default;
                        return false;
                    }

                    return true;
                case JsonTokenType.Null:
                    if (!allowNull)
                    {
                        text = @default;
                        return false;
                    }

                    text = null;
                    return true;
                case JsonTokenType.True:
                    text = "True";
                    return true;
                case JsonTokenType.False:
                    text = "False";
                    return true;
                default:
                    text = @default;
                    return false;
            }
        }

        /// <summary> Read an array of strings. </summary>
        /// <param name="allowsNullArray"> Whether the array itself may be null or not. </param>
        /// <param name="allowsNullEntries"> Whether the string entries inside the array may be null or not. </param>
        /// <returns> Null if the array is null and this is allowed, a list of string values (maybe null if allowed) otherwise. </returns>
        /// <exception cref="JsonException"> Throws if the array is null and this is not allowed, if a string value is null and this is not allowed, or if the JSON is malformed or not an array of strings. </exception>
        public List<string?>? ReadStringArray(bool allowsNullArray = true, bool allowsNullEntries = false)
        {
            if (allowsNullArray && reader.TokenType is JsonTokenType.Null)
                return null;

            if (reader.TokenType is not JsonTokenType.StartArray)
                throw new JsonException($"Expected string array but got {reader.TokenType}.");

            var ret   = new List<string?>();
            var array = reader.CreateObjectLimit();
            while (array.Read(ref reader))
            {
                if (allowsNullEntries && reader.TokenType is JsonTokenType.Null)
                {
                    ret.Add(null);
                    continue;
                }

                if (reader.TokenType is not JsonTokenType.String)
                    throw new JsonException($"Found non-string token of type {reader.TokenType} in string array.");

                ret.Add(reader.GetString());
            }

            if (reader.TokenType is not JsonTokenType.EndArray)
                throw new JsonException("Unexpected end without terminating string array.");

            return ret;
        }

        /// <summary> Read an array of UTF8 strings. </summary>
        /// <param name="allowsNullArray"> Whether the array itself may be null or not. </param>
        /// <param name="allowsNullEntries"> Whether the string entries inside the array may be null or not. </param>
        /// <returns> Null if the array is null and this is allowed, a list of string values (maybe null if allowed) otherwise. </returns>
        /// <exception cref="JsonException"> Throws if the array is null and this is not allowed, if a string value is null and this is not allowed, or if the JSON is malformed or not an array of strings. </exception>
        public List<StringU8>? ReadStringUtf8Array(bool allowsNullArray = true, bool allowsNullEntries = false)
        {
            if (allowsNullArray && reader.TokenType is JsonTokenType.Null)
                return null;

            if (reader.TokenType is not JsonTokenType.StartArray)
                throw new JsonException($"Expected string array but got {reader.TokenType}.");

            var ret   = new List<StringU8>();
            var array = reader.CreateObjectLimit();
            while (array.Read(ref reader))
            {
                if (reader.TokenType is JsonTokenType.EndArray)
                    return ret;

                if (allowsNullEntries && reader.TokenType is JsonTokenType.Null)
                {
                    ret.Add(StringU8.Null);
                    continue;
                }

                if (!reader.TryReadUtf8String(out var text, allowsNullEntries))
                    throw new JsonException($"Found non-string token of type {reader.TokenType} in string array.");

                ret.Add(text);
            }

            if (reader.TokenType is not JsonTokenType.EndArray)
                throw new JsonException("Unexpected end without terminating string array.");

            return ret;
        }

        /// <summary> Read an array of GUIDs. </summary>
        /// <param name="allowsNullArray"> Whether the array itself may be null or not. </param>
        /// <returns> Null if the array is null and this is allowed, a list of GUID values otherwise. </returns>
        /// <exception cref="JsonException"> Throws if the array is null and this is not allowed, or if the JSON is malformed or not an array of strings. </exception>
        public List<Guid>? ReadGuidArray(bool allowsNullArray = true)
        {
            if (allowsNullArray && reader.TokenType is JsonTokenType.Null)
                return null;

            if (reader.TokenType is not JsonTokenType.StartArray)
                throw new JsonException($"Expected GUID array but got {reader.TokenType}.");

            var ret   = new List<Guid>();
            var array = reader.CreateObjectLimit();
            while (array.Read(ref reader))
            {
                if (reader.TokenType is JsonTokenType.EndArray)
                    return ret;

                if (!reader.TryGetGuid(out var guid))
                    throw new JsonException($"Found invalid GUID token of type {reader.TokenType} in GUID array.");

                ret.Add(guid);
            }

            if (reader.TokenType is not JsonTokenType.EndArray)
                throw new JsonException("Unexpected end without terminating string array.");

            return ret;
        }


        /// <summary> Read an array of numeric values. </summary>
        /// <param name="allowsNullArray"> Whether the array itself may be null or not. </param>
        /// <returns> Null if the array is null and this is allowed, a list of parsed numbers otherwise. </returns>
        /// <exception cref="JsonException"> Throws if the array is null and this is not allowed, or if the JSON is malformed or not an array of numbers. </exception>
        public List<TNumber>? ReadNumberArray<TNumber>(bool allowsNullArray = true) where TNumber : unmanaged, INumber<TNumber>
        {
            if (allowsNullArray && reader.TokenType is JsonTokenType.Null)
                return null;

            if (reader.TokenType is not JsonTokenType.StartArray)
                throw new JsonException($"Expected number array but got {reader.TokenType}.");

            var ret   = new List<TNumber>();
            var array = reader.CreateObjectLimit();
            while (array.Read(ref reader))
            {
                if (reader.TokenType is JsonTokenType.EndArray)
                    return ret;

                ret.Add(reader.ReadNumber<TNumber>());
            }

            if (reader.TokenType is not JsonTokenType.EndArray)
                throw new JsonException("Unexpected end without terminating number array.");

            return ret;
        }

        /// <summary> Read an array of string values that represent flags of an enum. </summary>
        /// <returns> Null if the array is null, the bitwise OR'd flags otherwise. </returns>
        /// <exception cref="JsonException"> Throws if the JSON is not an array of strings that each represent a value of the flag enumeration. </exception>
        public TEnum? ReadFlagEnumArray<TEnum>() where TEnum : unmanaged, Enum
        {
            if (reader.TokenType is JsonTokenType.Null)
                return null;

            if (reader.TokenType is not JsonTokenType.StartArray)
                throw new JsonException($"Expected string array of {typeof(TEnum).Name} values but got {reader.TokenType}.");

            TEnum ret   = default;
            var   array = reader.CreateObjectLimit();
            while (array.Read(ref reader))
            {
                if (reader.TokenType is JsonTokenType.EndArray)
                    return ret;

                if (!reader.TryReadTextEnum(out TEnum value))
                    throw new Exception($"Expected string representing a flag of {typeof(TEnum).Name}.");

                ret = ret.Or(value);
            }

            if (reader.TokenType is not JsonTokenType.EndArray)
                throw new JsonException($"Unexpected end without terminating string array of {typeof(TEnum).Name} values.");

            return ret;
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

            return EnumExtensions.Parse(text, out value);
        }

        /// <summary> Try to read the current token as a number of the given type. </summary>
        /// <typeparam name="TNumber"> The type of number to read. </typeparam>
        /// <param name="number"> The return value on success, <paramref name="default"/> on failure. </param>
        /// <param name="default"> The default value to return if the number can not be read. </param>
        /// <returns> True if the number was successfully read, false otherwise. </returns>
        /// <exception cref="ArgumentException"> If <typeparamref name="TNumber"/> is not one of the built-in integers or floats. </exception>
        public bool TryReadNumber<TNumber>(out TNumber number, TNumber @default = default) where TNumber : unmanaged, INumber<TNumber>
        {
            // Read the actual number according to type.
            if (reader.TokenType is JsonTokenType.Number)
            {
                number = @default;
                if (typeof(TNumber) == typeof(byte))
                {
                    if (!reader.TryGetByte(out var b))
                        return false;

                    number = Unsafe.As<byte, TNumber>(ref b);
                    return true;
                }

                if (typeof(TNumber) == typeof(sbyte))
                {
                    if (!reader.TryGetSByte(out var sb))
                        return false;

                    number = Unsafe.As<sbyte, TNumber>(ref sb);
                    return true;
                }

                if (typeof(TNumber) == typeof(ushort))
                {
                    if (!reader.TryGetUInt16(out var us))
                        return false;

                    number = Unsafe.As<ushort, TNumber>(ref us);
                    return true;
                }

                if (typeof(TNumber) == typeof(short))
                {
                    if (!reader.TryGetInt16(out var s))
                        return false;

                    number = Unsafe.As<short, TNumber>(ref s);
                    return true;
                }

                if (typeof(TNumber) == typeof(uint))
                {
                    if (!reader.TryGetUInt32(out var ui))
                        return false;

                    number = Unsafe.As<uint, TNumber>(ref ui);
                    return true;
                }

                if (typeof(TNumber) == typeof(int))
                {
                    if (!reader.TryGetInt32(out var i))
                        return false;

                    number = Unsafe.As<int, TNumber>(ref i);
                    return true;
                }

                if (typeof(TNumber) == typeof(ulong))
                {
                    if (!reader.TryGetUInt64(out var ul))
                        return false;

                    number = Unsafe.As<ulong, TNumber>(ref ul);
                    return true;
                }

                if (typeof(TNumber) == typeof(long))
                {
                    if (!reader.TryGetInt64(out var l))
                        return false;

                    number = Unsafe.As<long, TNumber>(ref l);
                    return true;
                }

                if (typeof(TNumber) == typeof(float))
                {
                    if (!reader.TryGetSingle(out var f))
                        return false;

                    number = Unsafe.As<float, TNumber>(ref f);
                    return true;
                }

                if (typeof(TNumber) == typeof(double))
                {
                    if (!reader.TryGetDouble(out var d))
                        return false;

                    number = Unsafe.As<double, TNumber>(ref d);
                    return true;
                }

                throw new ArgumentException($"{typeof(TNumber)} is not supported.");
            }

            // Read the number as string and try to parse it.
            // TryReadUtf8String checks the token type itself.
            if (reader.TryReadUtf8String(out var text) && TNumber.TryParse(text, null, out number))
                return true;

            // All other cases are not valid numbers.
            number = @default;
            return false;
        }

        /// <summary> Read the current token as a number of the given type. </summary>
        /// <typeparam name="TNumber"> The type of number to read. </typeparam>
        /// <returns> The parsed number. </returns>
        /// <exception cref="ArgumentException"> If <typeparamref name="TNumber"/> is not one of the built-in integers or floats. </exception>
        /// <exception cref="JsonException"> If the number could not be read. </exception>
        public TNumber ReadNumber<TNumber>() where TNumber : unmanaged, INumber<TNumber>
        {
            // Read the actual number according to type.
            if (reader.TokenType is JsonTokenType.Number)
            {
                if (typeof(TNumber) == typeof(byte))
                {
                    var number = reader.GetByte();
                    return Unsafe.As<byte, TNumber>(ref number);
                }

                if (typeof(TNumber) == typeof(sbyte))
                {
                    var number = reader.GetSByte();
                    return Unsafe.As<sbyte, TNumber>(ref number);
                }

                if (typeof(TNumber) == typeof(ushort))
                {
                    var number = reader.GetUInt16();
                    return Unsafe.As<ushort, TNumber>(ref number);
                }

                if (typeof(TNumber) == typeof(short))
                {
                    var number = reader.GetInt16();
                    return Unsafe.As<short, TNumber>(ref number);
                }

                if (typeof(TNumber) == typeof(uint))
                {
                    var number = reader.GetUInt32();
                    return Unsafe.As<uint, TNumber>(ref number);
                }

                if (typeof(TNumber) == typeof(int))
                {
                    var number = reader.GetInt32();
                    return Unsafe.As<int, TNumber>(ref number);
                }

                if (typeof(TNumber) == typeof(ulong))
                {
                    var number = reader.GetUInt64();
                    return Unsafe.As<ulong, TNumber>(ref number);
                }

                if (typeof(TNumber) == typeof(long))
                {
                    var number = reader.GetInt64();
                    return Unsafe.As<long, TNumber>(ref number);
                }

                if (typeof(TNumber) == typeof(float))
                {
                    var number = reader.GetSingle();
                    return Unsafe.As<float, TNumber>(ref number);
                }

                if (typeof(TNumber) == typeof(double))
                {
                    var number = reader.GetDouble();
                    return Unsafe.As<double, TNumber>(ref number);
                }

                throw new ArgumentException($"{typeof(TNumber)} is not supported.");
            }

            // Read the number as string and try to parse it.
            // TryReadUtf8String checks the token type itself.
            if (reader.TryReadUtf8String(out var text))
                return TNumber.Parse(text, null);

            throw new JsonException($"Invalid JSON token of type {reader.TokenType} could not be read as a number.");
        }

        /// <summary> Skip to the end of the current object. </summary>
        public void SkipCurrentObject()
        {
            var objectReader = reader.CreateObjectLimit();
            while (objectReader.Read(ref reader))
                ;
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

/// <summary> A helper struct to parse within a single property value or object. </summary>
/// <param name="reader"> The reader to base this on. </param>
/// <remarks> This is not automatically written back to the original reader. If you want the new position written back, you need to assign it yourself. </remarks>
public ref struct Utf8JsonObjectReader(scoped in Utf8JsonReader reader)
{
    /// <summary> The depth of the current object. </summary>
    public readonly int ObjectDepth = reader.CurrentDepth;

    ///  <summary> A copy of the reader at the current position. </summary>
    public Utf8JsonReader Reader = reader;

    /// <summary> The type of the end token we use as stop. </summary>
    public JsonTokenType Type { get; private set; } = reader.TokenType switch
    {
        JsonTokenType.StartArray  => JsonTokenType.EndArray,
        JsonTokenType.StartObject => JsonTokenType.EndObject,
        JsonTokenType.Null        => JsonTokenType.None,
        JsonTokenType.Number      => JsonTokenType.None,
        JsonTokenType.String      => JsonTokenType.None,
        JsonTokenType.True        => JsonTokenType.None,
        JsonTokenType.False       => JsonTokenType.None,
        _ => throw new JsonException(
            $"{nameof(Utf8JsonObjectReader)} needs to be initialized on a value token, {nameof(JsonTokenType.StartObject)} or a {nameof(JsonTokenType.StartArray)} token, but is at a {reader.TokenType}."),
    };

    /// <summary> Read until the end of the current value or object is reached. </summary>
    /// <returns> True if the current read still is inside the current object or array. </returns>
    public bool Read()
    {
        if (Type is JsonTokenType.None)
            return false;

        if (!Reader.Read())
            throw new JsonException("Invalid JSON: Object is not ended.");

        if (Reader.TokenType != Type || Reader.CurrentDepth > ObjectDepth)
            return true;

        Type = JsonTokenType.None;
        return false;
    }
}

/// <summary> A helper struct to parse within a single property value or object. </summary>
/// <param name="reader"> The reader to base this on. </param>
/// <remarks> This does not copy the reader at the current location and can only be used with a referenced reader. </remarks>
public ref struct Utf8JsonObjectLimit(scoped in Utf8JsonReader reader)
{
    /// <summary> The depth of the current object. </summary>
    public readonly int ObjectDepth = reader.CurrentDepth;

    /// <summary> The type of the end token we use as stop. </summary>
    public JsonTokenType Type { get; private set; } = reader.TokenType switch
    {
        JsonTokenType.StartArray  => JsonTokenType.EndArray,
        JsonTokenType.StartObject => JsonTokenType.EndObject,
        JsonTokenType.Null        => JsonTokenType.None,
        JsonTokenType.Number      => JsonTokenType.None,
        JsonTokenType.String      => JsonTokenType.None,
        JsonTokenType.True        => JsonTokenType.None,
        JsonTokenType.False       => JsonTokenType.None,
        _ => throw new JsonException(
            $"{nameof(Utf8JsonObjectLimit)} needs to be initialized on a value token, {nameof(JsonTokenType.StartObject)} or a {nameof(JsonTokenType.StartArray)} token, but is at a {reader.TokenType}."),
    };

    /// <summary> Read until the end of the current value or object is reached. </summary>
    /// <returns> True if the current read still is inside the current object or array. </returns>
    public bool Read(ref Utf8JsonReader reader)
    {
        if (Type is JsonTokenType.None)
            return false;

        if (reader.TokenType == Type && reader.CurrentDepth == ObjectDepth)
            return false;

        if (!reader.Read())
            throw new JsonException("Invalid JSON: Object is not ended.");

        // This should not be able to happen?
        if (reader.CurrentDepth < ObjectDepth)
            throw new JsonException("Invalid JSON: Left object depth without ending it.");

        if (reader.TokenType != Type || reader.CurrentDepth > ObjectDepth)
            return true;

        Type = JsonTokenType.None;
        return false;
    }
}
