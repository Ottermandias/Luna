using System.Text.Json;

namespace Luna;

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
