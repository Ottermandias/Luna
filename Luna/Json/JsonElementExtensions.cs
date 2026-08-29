using System.Text.Json;

namespace Luna;

public static partial class JsonFunctions
{
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

        /// <summary> Get a specific value from a given property, or a default value if it is not set or set to null. </summary>
        /// <param name="property"> The name of the property. </param>
        /// <param name="defaultValue"> The value to use if the property is not set or set to null. </param>
        /// <returns> The parsed or default value. </returns>
        /// <exception cref="JsonException"> If the value is set, but can not be parsed to the requested type. </exception>
        public string PropertyOrDefault(ReadOnlySpan<byte> property, string defaultValue)
        {
            if (parent.TryReadProperty(property, out string? v, true))
                return v ?? defaultValue;

            throw new JsonException($"Invalid property value for string-property {Encoding.UTF8.GetString(property)} encountered.");
        }

        /// <inheritdoc cref="PropertyOrDefault(in JsonElement,ReadOnlySpan{byte},string)"/>
        public bool PropertyOrDefault(ReadOnlySpan<byte> property, bool defaultValue)
        {
            if (parent.TryReadProperty(property, out bool? v, true))
                return v ?? defaultValue;

            throw new JsonException($"Invalid property value for bool-property {Encoding.UTF8.GetString(property)} encountered.");
        }

        /// <inheritdoc cref="PropertyOrDefault(in JsonElement,ReadOnlySpan{byte},string)"/>
        public T PropertyOrDefault<T>(ReadOnlySpan<byte> property, in T defaultValue) where T : unmanaged, INumber<T>
        {
            if (parent.TryReadProperty(property, out T? v, true))
                return v ?? defaultValue;

            throw new JsonException($"Invalid property value for {typeof(T).Name}-property {Encoding.UTF8.GetString(property)} encountered.");
        }

        /// <inheritdoc cref="PropertyOrDefault(in JsonElement,ReadOnlySpan{byte},string)"/>
        public Guid PropertyOrDefault(ReadOnlySpan<byte> property, in Guid defaultValue)
        {
            if (parent.TryReadProperty(property, out Guid? v, true))
                return v ?? defaultValue;

            throw new JsonException($"Invalid property value for GUID-property {Encoding.UTF8.GetString(property)} encountered.");
        }

        /// <inheritdoc cref="PropertyOrDefault(in JsonElement,ReadOnlySpan{byte},string)"/>
        public DateTime PropertyOrDefault(ReadOnlySpan<byte> property, in DateTime defaultValue)
        {
            if (parent.TryReadProperty(property, out DateTime? v, true))
                return v ?? defaultValue;

            throw new JsonException($"Invalid property value for DateTime-property {Encoding.UTF8.GetString(property)} encountered.");
        }

        /// <inheritdoc cref="PropertyOrDefault(in JsonElement,ReadOnlySpan{byte},string)"/>
        public DateTimeOffset PropertyOrDefault(ReadOnlySpan<byte> property, in DateTimeOffset defaultValue)
        {
            if (parent.TryReadProperty(property, out DateTimeOffset? v, true))
                return v ?? defaultValue;

            throw new JsonException($"Invalid property value for DateTimeOffset-property {Encoding.UTF8.GetString(property)} encountered.");
        }

        /// <inheritdoc cref="PropertyOrDefault(in JsonElement,ReadOnlySpan{byte},string)"/>
        public T EnumOrDefault<T>(ReadOnlySpan<byte> property, in T defaultValue) where T : unmanaged, Enum
        {
            if (parent.TryReadEnum(property, out T? v, true, true))
                return v ?? defaultValue;

            throw new JsonException($"Invalid property value for {typeof(T).Name}-property {Encoding.UTF8.GetString(property)} encountered.");
        }

        /// <summary> Try to read a property's value by name. </summary>
        /// <param name="property"> The name of the queried property. </param>
        /// <param name="value"> The returned value on success, <c>null</c> otherwise. </param>
        /// <param name="allowNull"> Whether an explicit null-value or unset property returns true or false. </param>
        /// <returns> True if the property exists and can be parsed to the requested value type. </returns>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public bool TryReadProperty(ReadOnlySpan<byte> property, out string? value, bool allowNull = false)
        {
            Debug.Assert(parent.ValueKind is JsonValueKind.Object, "JSON parent value is not an object.");
            if (!parent.TryGetProperty(property, out var element))
            {
                value = null;
                return allowNull;
            }

            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    value = element.GetString();
                    return true;
                case JsonValueKind.True:
                    value = "True";
                    return true;
                case JsonValueKind.False:
                    value = "False";
                    return true;
                case JsonValueKind.Null:
                    value = null;
                    return allowNull;
                default:
                    value = null;
                    return false;
            }
        }

        /// <inheritdoc cref="TryReadProperty(in JsonElement,ReadOnlySpan{byte},out string?,bool)"/>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public bool TryReadProperty(ReadOnlySpan<byte> property, out bool? value, bool allowNull = false)
        {
            Debug.Assert(parent.ValueKind is JsonValueKind.Object, "JSON parent value is not an object.");
            if (!parent.TryGetProperty(property, out var element))
            {
                value = null;
                return allowNull;
            }

            switch (element.ValueKind)
            {
                case JsonValueKind.String when bool.TryParse(element.GetString(), out var b):
                    value = b;
                    return true;
                case JsonValueKind.True:
                    value = true;
                    return true;
                case JsonValueKind.False:
                    value = false;
                    return true;
                case JsonValueKind.Null:
                    value = null;
                    return allowNull;
                default:
                    value = null;
                    return false;
            }
        }

        /// <inheritdoc cref="TryReadProperty(in JsonElement,ReadOnlySpan{byte},out string?,bool)"/>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public unsafe bool TryReadProperty<T>(ReadOnlySpan<byte> property, out T? value, bool allowNull = false)
            where T : unmanaged, INumber<T>
        {
            Debug.Assert(parent.ValueKind is JsonValueKind.Object, "JSON parent value is not an object.");
            if (!parent.TryGetProperty(property, out var element))
            {
                value = null;
                return allowNull;
            }

            switch (element.ValueKind)
            {
                case JsonValueKind.Number:
                    if (typeof(T) == typeof(sbyte))
                    {
                        if (element.TryGetSByte(out var v))
                        {
                            value = *(T*)&v;
                            return true;
                        }
                    }
                    else if (typeof(T) == typeof(byte))
                    {
                        if (element.TryGetByte(out var v))
                        {
                            value = *(T*)&v;
                            return true;
                        }
                    }
                    else if (typeof(T) == typeof(short))
                    {
                        if (element.TryGetInt16(out var v))
                        {
                            value = *(T*)&v;
                            return true;
                        }
                    }
                    else if (typeof(T) == typeof(ushort))
                    {
                        if (element.TryGetUInt16(out var v))
                        {
                            value = *(T*)&v;
                            return true;
                        }
                    }
                    else if (typeof(T) == typeof(int))
                    {
                        if (element.TryGetInt32(out var v))
                        {
                            value = *(T*)&v;
                            return true;
                        }
                    }
                    else if (typeof(T) == typeof(uint))
                    {
                        if (element.TryGetUInt32(out var v))
                        {
                            value = *(T*)&v;
                            return true;
                        }
                    }
                    else if (typeof(T) == typeof(long))
                    {
                        if (element.TryGetInt64(out var v))
                        {
                            value = *(T*)&v;
                            return true;
                        }
                    }
                    else if (typeof(T) == typeof(ulong))
                    {
                        if (element.TryGetUInt64(out var v))
                        {
                            value = *(T*)&v;
                            return true;
                        }
                    }
                    else if (typeof(T) == typeof(float))
                    {
                        if (element.TryGetSingle(out var v))
                        {
                            value = *(T*)&v;
                            return true;
                        }
                    }
                    else if (typeof(T) == typeof(double))
                    {
                        if (element.TryGetDouble(out var v))
                        {
                            value = *(T*)&v;
                            return true;
                        }
                    }

                    value = null;
                    return false;
                case JsonValueKind.String:
                {
                    var text = element.GetString()!;
                    if (T.TryParse(text, CultureInfo.InvariantCulture, out var v) || T.TryParse(text, CultureInfo.CurrentCulture, out v))
                    {
                        value = v;
                        return true;
                    }

                    value = null;
                    return false;
                }
                case JsonValueKind.Null:
                    value = null;
                    return allowNull;
                default:
                    value = null;
                    return false;
            }
        }

        /// <summary> Try to read a property's enumeration value by name, either as a string representing an enumeration value, or as a numeric value. </summary>
        /// <param name="property"> The name of the queried property. </param>
        /// <param name="value"> The returned value on success, <c>null</c> otherwise. </param>
        /// <param name="allowNull"> Whether an explicit null-value or unset property returns true or false. </param>
        /// <param name="ignoreUnknownNames"> Whether unknown names for strings are treated as null or as failure, and whether unnamed numeric values are accepted or not. This takes into account flag combinations for enums marked as Flags. </param>
        /// <returns> True if the property exists and can be parsed to the requested value type. </returns>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public unsafe bool TryReadEnum<T>(ReadOnlySpan<byte> property, out T? value, bool allowNull = false, bool ignoreUnknownNames = true)
            where T : unmanaged, Enum
        {
            Debug.Assert(parent.ValueKind is JsonValueKind.Object, "JSON parent value is not an object.");
            if (!parent.TryGetProperty(property, out var element))
            {
                value = null;
                return allowNull;
            }

            switch (element.ValueKind)
            {
                case JsonValueKind.Number:
                    var type = typeof(T).GetEnumUnderlyingType();
                    value = null;
                    if (type == typeof(sbyte))
                    {
                        if (element.TryGetSByte(out var v))
                            value = *(T*)&v;
                    }
                    else if (type == typeof(byte))
                    {
                        if (element.TryGetByte(out var v))
                            value = *(T*)&v;
                    }
                    else if (type == typeof(short))
                    {
                        if (element.TryGetInt16(out var v))
                            value = *(T*)&v;
                    }
                    else if (type == typeof(ushort))
                    {
                        if (element.TryGetUInt16(out var v))
                            value = *(T*)&v;
                    }
                    else if (type == typeof(int))
                    {
                        if (element.TryGetInt32(out var v))
                            value = *(T*)&v;
                    }
                    else if (type == typeof(uint))
                    {
                        if (element.TryGetUInt32(out var v))
                            value = *(T*)&v;
                    }
                    else if (type == typeof(long))
                    {
                        if (element.TryGetInt64(out var v))
                            value = *(T*)&v;
                    }
                    else if (type == typeof(ulong))
                    {
                        if (element.TryGetUInt64(out var v))
                            value = *(T*)&v;
                    }

                    if (value is null)
                        return false;

                    if (ignoreUnknownNames)
                        return true;

                    if (!EnumExtensions.get_FlagsDefined(value.Value))
                        value = null;

                    return true;

                case JsonValueKind.String when EnumExtensions.Parse<T>(element.GetString()!, out var v):
                    value = v;
                    return true;
                case JsonValueKind.String:
                    value = null;
                    return ignoreUnknownNames;
                case JsonValueKind.Null:
                    value = null;
                    return allowNull;
                default:
                    value = null;
                    return false;
            }
        }

        /// <inheritdoc cref="TryReadProperty(in JsonElement,ReadOnlySpan{byte},out string?,bool)"/>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public bool TryReadProperty(ReadOnlySpan<byte> property, out Guid? value, bool allowNull = false)
        {
            Debug.Assert(parent.ValueKind is JsonValueKind.Object, "JSON parent value is not an object.");
            if (!parent.TryGetProperty(property, out var element))
            {
                value = null;
                return allowNull;
            }

            switch (element.ValueKind)
            {
                case JsonValueKind.String when element.TryGetGuid(out var v):
                    value = v;
                    return true;
                case JsonValueKind.Null:
                    value = null;
                    return allowNull;
                default:
                    value = Guid.Empty;
                    return false;
            }
        }

        /// <inheritdoc cref="TryReadProperty(in JsonElement,ReadOnlySpan{byte},out string?,bool)"/>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public bool TryReadProperty(ReadOnlySpan<byte> property, out DateTime? value, bool allowNull = false)
        {
            Debug.Assert(parent.ValueKind is JsonValueKind.Object, "JSON parent value is not an object.");
            if (!parent.TryGetProperty(property, out var element))
            {
                value = null;
                return allowNull;
            }

            switch (element.ValueKind)
            {
                case JsonValueKind.Number when element.TryGetInt64(out var timeStamp):
                    value = DateTimeOffset.FromUnixTimeMilliseconds(timeStamp).DateTime;
                    return true;
                case JsonValueKind.String when element.TryGetDateTime(out var v):
                    value = v;
                    return true;
                case JsonValueKind.Null:
                    value = null;
                    return allowNull;
                default:
                    value = null;
                    return false;
            }
        }

        /// <inheritdoc cref="TryReadProperty(in JsonElement,ReadOnlySpan{byte},out string?,bool)"/>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public bool TryReadProperty(ReadOnlySpan<byte> property, out DateTimeOffset? value, bool allowNull = false)
        {
            Debug.Assert(parent.ValueKind is JsonValueKind.Object, "JSON parent value is not an object.");
            if (!parent.TryGetProperty(property, out var element))
            {
                value = null;
                return allowNull;
            }

            switch (element.ValueKind)
            {
                case JsonValueKind.Number when element.TryGetInt64(out var timeStamp):
                    value = DateTimeOffset.FromUnixTimeMilliseconds(timeStamp);
                    return true;
                case JsonValueKind.String when element.TryGetDateTime(out var v):
                    value = v;
                    return true;
                case JsonValueKind.Null:
                    value = null;
                    return allowNull;
                default:
                    value = null;
                    return false;
            }
        }
    }
}
