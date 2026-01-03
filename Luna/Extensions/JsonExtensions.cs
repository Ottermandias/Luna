namespace Luna;

/// <summary> Extensions for Newtonsoft JSON objects. </summary>
public static class JsonExtensions
{
    /// <param name="json"> The JSON object to parse. </param>
    extension(IEnumerable<JToken>? json)
    {
        /// <summary> Get a textual enumeration value from a JSON object. </summary>
        /// <typeparam name="TEnum"> The type of the enumeration. </typeparam>
        /// <returns> Null if the token is not a string or can not be parsed to the enumeration type, the corresponding value otherwise. </returns>
        public TEnum? TextEnum<TEnum>() where TEnum : struct, Enum
        {
            if (json?.Value<string>() is not { } value)
                return null;
            if (Enum.TryParse<TEnum>(value, true, out var ret))
                return ret;

            return null;
        }

        /// <summary> Get a textual enumeration value from a JSON object. </summary>
        /// <typeparam name="TEnum"> The type of the enumeration. </typeparam>
        /// <param name="defaultValue"> The value to return on failure. </param>
        /// <returns> The passed default value if the token is not a string or can not be parsed to the enumeration type, the corresponding value otherwise. </returns>
        public TEnum TextEnum<TEnum>(TEnum defaultValue) where TEnum : struct, Enum
        {
            if (json?.Value<string>() is not { } value)
                return defaultValue;
            if (Enum.TryParse<TEnum>(value, true, out var ret))
                return ret;

            return defaultValue;
        }

        /// <inheritdoc cref="Newtonsoft.Json.Linq.Extensions.Value{T}"/>
        [OverloadResolutionPriority(100)]
        public T ValueOr<T>(T defaultValue)
        {
            if (json is null)
                return defaultValue;

            return json.Value<T>() ?? defaultValue;
        }
    }
}
