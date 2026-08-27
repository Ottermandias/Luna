using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Luna;

/// <summary> Methods to convert between Newtonsoft JSON <see cref="JObject"/>s and System.Text.Json <see cref="JsonDocument"/>. </summary>
public static class JsonObjectConversion
{
    /// <summary> An interface for objects types that implement write methods for a JSON converter. </summary>
    /// <typeparam name="TSelf"> The own type. </typeparam>
    public interface IJsonWritable<TSelf> where TSelf : IJsonWritable<TSelf>
    {
        public abstract static void Write(Utf8JsonWriter writer, in TSelf value, JsonSerializerOptions options);
    }

    /// <summary> Create a System.Text.Json <see cref="JsonElement"/> from a Newtonsoft JSON <see cref="JObject"/>. </summary>
    /// <returns> The JsonDocument. </returns>
    public static JsonElement ToElement(this JObject j)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var jw = new Utf8JsonWriter(buffer))
        {
            WriteTo(j, jw);
        }

        return JsonElement.Parse(buffer.WrittenMemory.Span, JsonFunctions.DocumentOptions).Clone();
    }

    /// <summary> Create a Newtonsoft JSON <see cref="JObject"/> from System.Text.Json <see cref="JsonDocument"/>. </summary>
    /// <returns> The JToken. </returns>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public static JObject? ToObject(this JsonDocument j)
        => j.RootElement.ToToken() as JObject;

    /// <summary> Create a Newtonsoft JSON <see cref="JToken"/> from System.Text.Json <see cref="JsonElement"/>. </summary>
    /// <returns> The JToken. </returns>
    public static JToken ToToken(this JsonElement j)
        => j.ValueKind switch
        {
            JsonValueKind.Object => new JObject(j.EnumerateObject().Select(p => new JProperty(p.Name, ToToken(p.Value)))),
            JsonValueKind.Array => new JArray(j.EnumerateArray().Select(ToToken)),
            JsonValueKind.String => new JValue(j.GetString()),
            JsonValueKind.Number => NumberToJToken(j),
            JsonValueKind.True => new JValue(true),
            JsonValueKind.False => new JValue(false),
            JsonValueKind.Null or JsonValueKind.Undefined => JValue.CreateNull(),
            _ => throw new ArgumentOutOfRangeException(),
        };

    extension<T>(T value) where T : IJsonWritable<T>
    {
        /// <summary> Use the System.Text.Json implementation to serialize an object to a Newtonsoft JSON token. </summary>
        /// <returns> The JToken. </returns>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public JToken ToObject()
            => ToToken(ToElement(value));

        /// <summary> Use the <see cref="Utf8JsonWriter"/>-based serialization implementation to write an element. </summary>
        /// <returns> The JSON element. </returns>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public JsonElement ToElement()
            => JsonSerializer.SerializeToElement(value, JsonDocumentConverter<T>.Unformatted);

        /// <summary> Use the <see cref="Utf8JsonWriter"/>-based serialization implementation to serialize to UTF8 JSON. </summary>
        /// <param name="formatted"> Whether the JSON should be formatted or not. </param>
        /// <returns> The UTF8-encoded JSON. </returns>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public byte[] ToJson(bool formatted = false)
            => JsonSerializer.SerializeToUtf8Bytes(value, formatted ? JsonDocumentConverter<T>.Options : JsonDocumentConverter<T>.Unformatted);
    }

    [MethodImpl(ImSharpConfiguration.Inl)]
    private static JValue NumberToJToken(JsonElement element)
    {
        var raw = element.GetRawText();

        // JSON integer: no decimal point and no exponent.
        if (!raw.Contains('.') && !raw.Contains('e') && !raw.Contains('E'))
        {
            if (element.TryGetInt64(out var i64))
                return new JValue(i64);

            if (element.TryGetUInt64(out var u64))
                return new JValue(u64);

            return new JValue(BigInteger.Parse(raw, CultureInfo.InvariantCulture));
        }

        // Prefer decimal because it retains much more decimal precision than double.
        if (element.TryGetDecimal(out var dec))
            return new JValue(dec);

        // JSON.NET has no arbitrary-precision floating-point numeric type,
        // so double is the remaining normal numeric representation.
        return new JValue(element.GetDouble());
    }

    [MethodImpl(ImSharpConfiguration.Inl)]
    private static void WriteTo(JToken token, Utf8JsonWriter j)
    {
        switch (token)
        {
            case JObject obj:
                j.WriteStartObject();
                foreach (var property in obj.Properties())
                {
                    j.WritePropertyName(property.Name);
                    WriteTo(property.Value, j);
                }

                j.WriteEndObject();
                return;

            case JArray array:
                j.WriteStartArray();
                foreach (var item in array)
                    WriteTo(item, j);
                j.WriteEndArray();
                return;
            case JRaw raw:
                j.WriteRawValue(raw.Value?.ToString() ?? "null");
                return;
            case JValue v:
                WriteValue(v, j);
                return;

            default:
                throw new NotSupportedException(
                    $"JToken type {token.Type} is not supported.");
        }
    }

    [MethodImpl(ImSharpConfiguration.Inl)]
    private static void WriteValue(JValue value, Utf8JsonWriter writer)
    {
        switch (value.Type)
        {
            case JTokenType.Null:
                writer.WriteNullValue();
                return;
            case JTokenType.Boolean:
                writer.WriteBooleanValue(value.Value<bool>());
                return;
            case JTokenType.String:
                writer.WriteStringValue(value.Value<string>());
                return;
            case JTokenType.Integer:
                WriteInteger(value.Value!, writer);
                return;
            case JTokenType.Float:
                WriteFloat(value.Value!, writer);
                return;
            case JTokenType.Date:
                WriteDate(value.Value!, writer);
                return;
            case JTokenType.Bytes:
                writer.WriteBase64StringValue(value.Value<byte[]>());
                return;
            case JTokenType.Guid:
                writer.WriteStringValue(value.Value<Guid>());
                return;
            case JTokenType.Uri:
                writer.WriteStringValue(value.Value<Uri>()?.OriginalString);
                return;
            case JTokenType.TimeSpan:
                writer.WriteStringValue(value.Value<TimeSpan>().ToString("c", CultureInfo.InvariantCulture));
                return;
            case JTokenType.Undefined:
                writer.WriteNullValue();
                return;
            default:
                throw new NotSupportedException(
                    $"JValue type {value.Type} can not be represented by System.Text.Json.");
        }
    }

    [MethodImpl(ImSharpConfiguration.Inl)]
    private static void WriteInteger(object value, Utf8JsonWriter writer)
    {
        switch (value)
        {
            case sbyte v:
                writer.WriteNumberValue(v);
                return;
            case byte v:
                writer.WriteNumberValue(v);
                return;
            case short v:
                writer.WriteNumberValue(v);
                return;
            case ushort v:
                writer.WriteNumberValue(v);
                return;
            case int v:
                writer.WriteNumberValue(v);
                return;
            case uint v:
                writer.WriteNumberValue(v);
                return;
            case long v:
                writer.WriteNumberValue(v);
                return;
            case ulong v:
                writer.WriteNumberValue(v);
                return;
            case BigInteger v:
                writer.WriteRawValue(v.ToString(CultureInfo.InvariantCulture), true);
                return;
            default: throw new NotSupportedException($"Unsupported integer type {value.GetType()}.");
        }
    }

    [MethodImpl(ImSharpConfiguration.Inl)]
    private static void WriteFloat(object value, Utf8JsonWriter writer)
    {
        switch (value)
        {
            case float v:
                writer.WriteNumberValue(v);
                return;
            case double v:
                writer.WriteNumberValue(v);
                return;
            case decimal v:
                writer.WriteNumberValue(v);
                return;
            default: throw new NotSupportedException($"Unsupported floating-point type {value.GetType()}.");
        }
    }

    [MethodImpl(ImSharpConfiguration.Inl)]
    private static void WriteDate(object value, Utf8JsonWriter writer)
    {
        switch (value)
        {
            case DateTime dt:
                writer.WriteStringValue(dt);
                return;
            case DateTimeOffset dto:
                writer.WriteStringValue(dto);
                return;
            default: throw new NotSupportedException($"Unexpected Date value type {value.GetType()}.");
        }
    }

    internal sealed class JsonDocumentConverter<T> : JsonConverter<T> where T : IJsonWritable<T>
    {
        internal static readonly JsonDocumentConverter<T> Instance    = new();
        internal static readonly JsonSerializerOptions    Options     = GenerateOptions<T>(false);
        internal static readonly JsonSerializerOptions    Unformatted = GenerateOptions<T>(true);

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotImplementedException();

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            => T.Write(writer, value, options);
    }

    private static JsonSerializerOptions GenerateOptions<T>(bool unformatted) where T : IJsonWritable<T>
    {
        var options = new JsonSerializerOptions(unformatted ? JsonFunctions.UnformattedSerializerOptions : JsonFunctions.SerializerOptions);
        options.Converters.Add(JsonDocumentConverter<T>.Instance);
        return options;
    }
}
