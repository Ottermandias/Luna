using System.Text.Json;
using System.Text.Json.Serialization;

namespace Luna;

public static partial class JsonFunctions
{
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
        /// <param name="allowUnsignedNegative"> Whether to allow reading a negative number for unsigned values. </param>
        /// <returns> True if the property names correspond, false otherwise. </returns>
        /// <exception cref="JsonException"> If the property matches but has no following value token to read, or if the value token is not a number. </exception>
        [MethodImpl(ImSharpConfiguration.OptInl)]
        public bool NumberProperty<TNumber>(ReadOnlySpan<byte> propertyName, out TNumber value, bool allowUnsignedNegative = false)
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

            return reader.TryReadNumber(out value, default, allowUnsignedNegative)
                ? true
                : throw new JsonException(
                    $"Unexpected {reader.TokenType} value for numeric property {Encoding.UTF8.GetString(propertyName)}.");
        }

        /// <summary> Check whether the current property name token corresponds to the given property and has numerical value. </summary>
        /// <param name="propertyName"> The property name to check for. </param>
        /// <param name="value"> The parsed number on success. </param>
        /// <param name="allowUnsignedNegative"> Whether to allow reading a negative number for unsigned values. </param>
        /// <returns> True if the property names correspond, false otherwise. </returns>
        /// <exception cref="JsonException"> If the property matches but has no following value token to read, or if the value token is not a number. </exception>
        [MethodImpl(ImSharpConfiguration.OptInl)]
        public bool NumberProperty<TNumber>(ReadOnlySpan<byte> propertyName, out TNumber? value, bool allowUnsignedNegative = false)
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

            if (reader.TryReadNumber(out TNumber number, default, allowUnsignedNegative))
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
        /// <param name="ignoreUnknownValues"> Whether entries not matching any enum entries should be ignored or throw an error. </param>
        /// <returns> Null if the array is null, the bitwise OR'd flags otherwise. </returns>
        /// <exception cref="JsonException"> Throws if the JSON is not an array of strings that each represent a value of the flag enumeration. </exception>
        public TEnum? ReadFlagEnumArray<TEnum>(bool ignoreUnknownValues = false) where TEnum : unmanaged, Enum
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

                if (reader.TryReadTextEnum(out TEnum value))
                    ret = ret.Or(value);
                else if (!ignoreUnknownValues)
                    throw new Exception($"Expected string representing a flag of {typeof(TEnum).Name}.");
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
        /// <param name="allowUnsignedNegative"> Whether to allow reading a negative number for unsigned values. </param>
        /// <returns> True if the number was successfully read, false otherwise. </returns>
        /// <exception cref="ArgumentException"> If <typeparamref name="TNumber"/> is not one of the built-in integers or floats. </exception>
        public bool TryReadNumber<TNumber>(out TNumber number, TNumber @default = default, bool allowUnsignedNegative = false)
            where TNumber : unmanaged, INumber<TNumber>
        {
            // Read the actual number according to type.
            if (reader.TokenType is JsonTokenType.Number)
            {
                number = @default;
                if (typeof(TNumber) == typeof(byte))
                {
                    if (reader.TryGetByte(out var b))
                    {
                        number = Unsafe.As<byte, TNumber>(ref b);
                        return true;
                    }

                    if (!allowUnsignedNegative || !reader.TryGetSByte(out var sb))
                        return false;

                    number = Unsafe.As<sbyte, TNumber>(ref sb);
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
                    if (reader.TryGetUInt16(out var b))
                    {
                        number = Unsafe.As<ushort, TNumber>(ref b);
                        return true;
                    }

                    if (!allowUnsignedNegative || !reader.TryGetInt16(out var sb))
                        return false;

                    number = Unsafe.As<short, TNumber>(ref sb);
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
                    if (reader.TryGetUInt32(out var b))
                    {
                        number = Unsafe.As<uint, TNumber>(ref b);
                        return true;
                    }

                    if (!allowUnsignedNegative || !reader.TryGetInt32(out var sb))
                        return false;

                    number = Unsafe.As<int, TNumber>(ref sb);
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
                    if (reader.TryGetUInt64(out var b))
                    {
                        number = Unsafe.As<ulong, TNumber>(ref b);
                        return true;
                    }

                    if (!allowUnsignedNegative || !reader.TryGetInt64(out var sb))
                        return false;

                    number = Unsafe.As<long, TNumber>(ref sb);
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
}
