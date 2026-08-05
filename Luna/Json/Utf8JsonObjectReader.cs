using System.Text.Json;

namespace Luna;

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
