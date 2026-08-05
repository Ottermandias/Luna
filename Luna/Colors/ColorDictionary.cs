using System.Text.Json;
using System.Text.Json.Serialization;
using ImSharp.ImNodes;

namespace Luna;

/// <summary> A dictionary containing custom colors. </summary>
/// <typeparam name="TColorId"> A contiguous enumeration of color IDs. </typeparam>
/// <typeparam name="TColorData"> A provider to emulate the color ID implementing an interface to get default data and information. </typeparam>
[GenericJsonConverter(typeof(Converter<,>))]
public sealed class ColorDictionary<TColorId, TColorData> : IReadOnlyCollection<KeyValuePair<TColorId, ColorDataUnion>>
    where TColorId : unmanaged, Enum
    where TColorData : IColorData<TColorId>
{
    /// <summary> Invoked whenever a value has been changed in this dictionary. </summary>
    public event Action Change;

    private readonly Dictionary<TColorId, ColorDataUnion> _colors = new();

    /// <summary> Get the actual value for a color ID. </summary>
    /// <param name="id"> The color ID. </param>
    /// <returns> The current value associated with the ColorID, either through user settings, or the default reference for this color. </returns>
    /// <exception cref="ArgumentOutOfRangeException"> When the color ID has no default reference data. </exception>
    public Rgba32 GetColor(TColorId id)
    {
        if (!_colors.TryGetValue(id, out var color) || color.IsDefault)
            color = TColorData.Data(id).Default;

        return color.Type switch
        {
            ColorDataUnion.TypeEnum.Const   => color.ConstantValue,
            ColorDataUnion.TypeEnum.Self    => GetColor(color.SelfValue<TColorId>()),
            ColorDataUnion.TypeEnum.ImGui   => color.ImGuiValue.Get(),
            ColorDataUnion.TypeEnum.ImNodes => ImNodes.Style[color.ImNodesValue],
            ColorDataUnion.TypeEnum.Dalamud => color.DalamudValue.Value,
            ColorDataUnion.TypeEnum.Luna    => color.LunaValue.Value,
            _                               => throw new ArgumentOutOfRangeException($"No default data for TColorId.{id.String} exists."),
        };
    }

    /// <summary> Get or set the actual value for a color ID. </summary>
    /// <param name="id"> The color ID. </param>
    /// <returns> The current value set for the ID. </returns>
    public ColorDataUnion this[TColorId id]
    {
        get => _colors.GetValueOrDefault(id, ColorDataUnion.Default);
        set
        {
            if (_colors.TryGetValue(id, out var currentValue))
            {
                if (currentValue == value)
                    return;

                _colors[id] = value;
                Change.Invoke();
            }
            else
            {
                _colors[id] = value;
                if (!value.IsDefault)
                    Change.Invoke();
            }
        }
    }

    /// <summary> Remove a set value for a color ID. </summary>
    /// <param name="id"> The color ID. </param>
    /// <returns> Whether the ID was set to non-default before. </returns>
    public bool Remove(TColorId id)
    {
        if (_colors.Remove(id, out var value) && !value.IsDefault)
        {
            Change.Invoke();
            return true;
        }

        return false;
    }

    /// <summary> Reset all existing colors to their default values. </summary>
    /// <returns> True if anything changed. </returns>
    public bool ResetToDefault()
    {
        if (_colors.Count is 0 || _colors.Values.All(c => c.IsDefault))
            return false;

        _colors.Clear();
        Change.Invoke();
        return true;
    }

    /// <summary> Apply the values from the other color dictionary to this dictionary. </summary>
    /// <param name="other"> The values to apply. </param>
    /// <param name="applyDefaults"> Whether default values in the other dictionary should overwrite existing values in this, or be ignored. </param>
    /// <returns> If any values were changed. </returns>
    /// <remarks> Any unknown color IDs in the other dictionary are ignored. </remarks>
    public bool Apply(ColorDictionary<TColorId, TColorData> other, bool applyDefaults)
    {
        var changes = false;
        foreach (var (id, value) in other)
        {
            if (!id.Defined)
                continue;

            var currentValue = _colors.GetValueOrDefault(id, ColorDataUnion.Default);
            if (currentValue == value)
                continue;

            if (value.IsDefault && !applyDefaults)
                continue;

            _colors[id] = value;
            changes     = true;
        }

        if (changes)
            Change?.Invoke();
        return changes;
    }

    /// <summary> Create a sharable UTF8 Base64 array of the color dictionary. </summary>
    /// <param name="withDefaults"> Whether to include default values or not. </param>
    /// <returns> A UTF8 Base64-encoded array to share. </returns>
    public byte[] Sharable(bool withDefaults)
    {
        using var memory = new MemoryStream();
        using (var j = new Utf8JsonWriter(memory, JsonFunctions.UnformattedOptions))
        {
            Serialize(j, withDefaults);
        }

        memory.Flush();
        var bytes = memory.GetBuffer().AsSpan(0, (int)memory.Length);
        return CompressionFunctions.ToCompressedBase64(bytes, 1);
    }

    /// <summary> Read a color dictionary from a UTF8 Base64 string as produced by <see cref="Sharable"/>. </summary>
    /// <param name="base64"> The UTF8 Base64-encoded data. </param>
    /// <param name="withDefaults"> Whether to include default values from the passed data or ignore them. </param>
    /// <returns> A new color dictionary containing the shared data or null on failure. </returns>
    public static ColorDictionary<TColorId, TColorData>? FromSharable(ReadOnlySpan<byte> base64, bool withDefaults)
    {
        var version = CompressionFunctions.FromCompressedBase64(base64, out var data);
        if (version is not 1)
            return null;

        var reader = new Utf8JsonReader(data.Span);
        try
        {
            if (!reader.Read())
                return null;

            return Deserialize(null, ref reader, withDefaults, false, true);
        }
        catch
        {
            return null;
        }
    }

    /// <summary> Write a color dictionary JSON object (including the object start and end). </summary>
    /// <param name="writer"> The JSON writer. </param>
    /// <param name="withDefaults"> Whether to write all default values as 'null's or not write them at all. </param>
    /// <returns> The JSON writer for method chaining. </returns>
    public unsafe Utf8JsonWriter Serialize(Utf8JsonWriter writer, bool withDefaults)
    {
        writer.WriteStartObject();
        Span<byte> buffer = stackalloc byte[128];
        if (withDefaults)
            foreach (var (name, id) in EnumExtensions.get_NamesAndValuesU8<TColorId>())
            {
                writer.WritePropertyName(name);
                if (!_colors.TryGetValue(id, out var color))
                    color = ColorDataUnion.Default;

                if (color.IsDefault)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    if (!color.Write<TColorId>(buffer, TColorData.Parent, out var written))
                        throw new JsonException($"Could not write value for {id}: {color}.");

                    writer.WriteStringValue(buffer[..written]);
                }
            }
        else
            foreach (var (id, value) in _colors)
            {
                if (value.IsDefault)
                    continue;

                writer.WritePropertyName(id.StringU8);
                if (!value.Write<TColorId>(buffer, TColorData.Parent, out var written))
                    throw new JsonException($"Could not write value for {id}: {value}.");

                writer.WriteStringValue(buffer[..written]);
            }

        writer.WriteEndObject();

        return writer;
    }

    /// <summary> Create a dictionary initialized with all values set to default. </summary>
    public static ColorDictionary<TColorId, TColorData> InitializeWithDefaults()
    {
        var ret = new ColorDictionary<TColorId, TColorData>();
        ret._colors.EnsureCapacity(EnumExtensions.get_Values<TColorId>().Count);
        foreach (var id in EnumExtensions.get_Values<TColorId>())
            ret._colors.Add(id, ColorDataUnion.Default);
        return ret;
    }

    /// <summary> Default constructor. </summary>
    public ColorDictionary()
        => Change += () => CacheManager.Instance.SetColorsDirty();

    /// <summary> Construct a ColorDictionary from an existing old-style color dictionary. </summary>
    /// <param name="oldColors"> The old colors for migration. </param>
    /// <param name="isDefault"> A function that returns whether a given color is default. If this is null, only colors with constant default value can be checked against default and be ignored. </param>
    public ColorDictionary(Dictionary<TColorId, uint> oldColors, Func<TColorId, Rgba32, bool>? isDefault)
        : this()
    {
        _colors.EnsureCapacity(oldColors.Count);
        foreach (var (id, color) in oldColors)
        {
            if (!id.Defined)
                continue;

            if (isDefault is not null)
            {
                if (!isDefault(id, color))
                    _colors.Add(id, new ColorDataUnion((Rgba32)color));
            }
            else
            {
                var data = TColorData.Data(id);
                if (data.Default.Type is not ColorDataUnion.TypeEnum.Const || data.Default.ConstantValue != color)
                    _colors.Add(id, new ColorDataUnion((Rgba32)color));
            }
        }
    }

    /// <summary> Read a color dictionary from JSON. </summary>
    /// <param name="messager"> The messager to inform about errors parsing while retaining valid data. </param>
    /// <param name="j"> The JSON reader. </param>
    /// <param name="withDefaults"> Whether to keep all parsed default values, or discard them. </param>
    /// <param name="allDefaults"> Whether to fill all default values from the start. </param>
    /// <param name="ignoreUnknowns"> Whether to throw when encountering unknown color IDs, or ignore them. </param>
    /// <returns> The parsed color dictionary. </returns>
    /// <exception cref="JsonException"> When the dictionary could not be parsed or an unknown ID was encountered while not being allowed. </exception>
    public static ColorDictionary<TColorId, TColorData> Deserialize(MessageService? messager, ref Utf8JsonReader j, bool withDefaults,
        bool allDefaults, bool ignoreUnknowns)
    {
        var ret         = allDefaults ? InitializeWithDefaults() : new ColorDictionary<TColorId, TColorData>();
        var failures    = new HashSet<(TColorId, string)>();
        var objectScope = j.CreateObjectLimit();
        while (objectScope.Read(ref j))
        {
            if (j.TokenType is not JsonTokenType.PropertyName)
                throw new JsonException("Invalid JSON reading ColorDictionary: Start of object is not a property name.");

            if (!j.TryReadTextEnum<TColorId>(out var id))
            {
                if (!ignoreUnknowns)
                    throw new JsonException("Unknown ColorID encountered reading ColorDictionary.");

                j.Skip();
            }
            else if (!objectScope.Read(ref j))
            {
                throw new JsonException("Invalid JSON reading ColorDictionary: Early object termination.");
            }


            switch (j.TokenType)
            {
                case JsonTokenType.Null:
                {
                    if (withDefaults)
                        ret[id] = ColorDataUnion.Default;
                    else
                        ret.Remove(id);
                    break;
                }
                case JsonTokenType.String:
                {
                    if (!j.TryReadUtf8String(out var text) || !ColorDataUnion.TryParse<TColorId>(text, TColorData.Parent, out var value))
                        failures.Add((id, "Color String could not be parsed."));
                    else
                        ret[id] = value;
                    break;
                }
                case JsonTokenType.Number:
                {
                    if (j.TryGetUInt32(out var color))
                        ret[id] = new ColorDataUnion((Rgba32)color);
                    else if (j.TryGetInt32(out var color2))
                        ret[id] = new ColorDataUnion((Rgba32)(uint)color2);
                    else
                        failures.Add((id, "Number was not a valid color."));

                    break;
                }
                default: failures.Add((id, "Invalid Data Type.")); break;
            }

            if (ret.CheckForCycles(id, ret[id]))
            {
                failures.Add((id, "Would have caused circular dependencies."));
                ret.Remove(id);
            }
        }

        if (messager is not null && failures.Count > 0)
            messager.NotificationMessage(
                $"Kept {failures.Count} provided ColorDictionary values on default:\n   {string.Join("\n    ", failures.Select(p => $"{p.Item1.StringU8}: {p.Item2}"))}");

        return ret;
    }

    /// <summary> Check whether setting the color ID to a given value would cause cyclic dependencies. </summary>
    /// <param name="id"> The start ID to check. </param>
    /// <param name="data"> The value to check against. </param>
    /// <returns> True if the value would cause a cyclic dependency, false otherwise. </returns>
    public bool CheckForCycles(TColorId id, ColorDataUnion data)
    {
        while (data.Type is ColorDataUnion.TypeEnum.Self)
        {
            var parent = data.SelfValue<TColorId>();
            if (parent.Equals(id))
                return true;

            if (!_colors.TryGetValue(parent, out data) || data.IsDefault)
                data = TColorData.Data(parent).Default;
        }

        return false;
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TColorId, ColorDataUnion
    >> GetEnumerator()
        => _colors.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public int Count
        => _colors.Count;
}

/// <summary> A default converter for serialization and deserialization.  </summary>
public sealed class ColorDictionaryConverter<TColorId, TColorData>(
    MessageService? messager,
    bool withDefaults,
    bool allDefaults,
    bool ignoreUnknowns) : JsonConverter<ColorDictionary<TColorId, TColorData>>
    where TColorId : unmanaged, Enum
    where TColorData : IColorData<TColorId>
{
    public override ColorDictionary<TColorId, TColorData>? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
        => ColorDictionary<TColorId, TColorData>.Deserialize(messager, ref reader, withDefaults, allDefaults, ignoreUnknowns);


    public override void Write(Utf8JsonWriter writer, ColorDictionary<TColorId, TColorData> value, JsonSerializerOptions options)
        => value.Serialize(writer, withDefaults);
}
