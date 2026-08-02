using System.Text.Json;
using Dalamud.Game.ClientState.Keys;
using Newtonsoft.Json;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Luna;

/// <summary> A single arbitrary hotkey with up to two modifiers. </summary>
[JsonConverter(typeof(Converter))]
public struct ModifiableHotkey : IEquatable<ModifiableHotkey>
{
    /// <summary> The hotkey to press. </summary>
    public VirtualKey Hotkey { get; private set; } = VirtualKey.NO_KEY;

    private DoubleModifier _modifiers = DoubleModifier.NoKey;

    /// <summary> The optional modifiers. </summary>
    public DoubleModifier Modifiers
        => _modifiers;

    /// <summary> An empty hotkey representing no keys. </summary>
    public ModifiableHotkey()
    { }

    /// <summary> Create a hotkey without modifiers, optionally checking against a set of valid keys. </summary>
    /// <param name="hotkey"> They hotkey to create. </param>
    /// <param name="validKeys"> The valid keys for the hotkey to have. If this is set and <paramref name="hotkey"/> is not contained, it will be set to <see cref="VirtualKey.NO_KEY"/>. </param>
    public ModifiableHotkey(VirtualKey hotkey, VirtualKey[]? validKeys = null)
    {
        SetHotkey(hotkey, validKeys);
    }

    /// <summary> Create a hotkey with up to one modifier, optionally checking against a set of valid keys. </summary>
    /// <param name="hotkey"> They hotkey to create. </param>
    /// <param name="modifier1"> The modifier. Can be <see cref="ModifierHotkey.NoKey"/>. </param>
    /// <param name="validKeys"> The valid keys for the hotkey to have. If this is set and <paramref name="hotkey"/> is not contained, it will be set to <see cref="VirtualKey.NO_KEY"/>. </param>
    public ModifiableHotkey(VirtualKey hotkey, ModifierHotkey modifier1, VirtualKey[]? validKeys = null)
    {
        SetHotkey(hotkey, validKeys);
        if (hotkey is not VirtualKey.NO_KEY)
            _modifiers = new DoubleModifier(modifier1);
    }

    /// <summary> Create a hotkey with up to two modifiers, optionally checking against a set of valid keys. </summary>
    /// <param name="hotkey"> They hotkey to create. </param>
    /// <param name="modifier1"> The first modifier. Can be <see cref="ModifierHotkey.NoKey"/>. See <see cref="DoubleModifier"/> for behavior. </param>
    /// <param name="modifier2"> The second modifier. Can be <see cref="ModifierHotkey.NoKey"/>. See <see cref="DoubleModifier"/> for behavior.  </param>
    /// <param name="validKeys"> The valid keys for the hotkey to have. If this is set and <paramref name="hotkey"/> is not contained, it will be set to <see cref="VirtualKey.NO_KEY"/>. </param>
    [JsonConstructor]
    public ModifiableHotkey(VirtualKey hotkey, ModifierHotkey modifier1, ModifierHotkey modifier2, VirtualKey[]? validKeys = null)
    {
        SetHotkey(hotkey, validKeys);
        if (hotkey is not VirtualKey.NO_KEY)
            _modifiers = new DoubleModifier(modifier1, modifier2);
    }

    /// <summary> Create a hotkey with up to two modifiers, optionally checking against a set of valid keys. </summary>
    /// <param name="hotkey"> They hotkey to create. </param>
    /// <param name="modifiers"> The modifiers. See <see cref="DoubleModifier"/> for behavior. </param>
    /// <param name="validKeys"> The valid keys for the hotkey to have. If this is set and <paramref name="hotkey"/> is not contained, it will be set to <see cref="VirtualKey.NO_KEY"/>. </param>
    public ModifiableHotkey(VirtualKey hotkey, DoubleModifier modifiers, VirtualKey[]? validKeys = null)
    {
        SetHotkey(hotkey, validKeys);
        if (hotkey is not VirtualKey.NO_KEY)
            _modifiers = modifiers;
    }

    /// <summary>
    ///   Try to set the given hotkey.
    ///   If validKeys is given, the hotkey has to be contained in it.
    ///   If the key is empty, both modifiers will be reset.
    /// </summary>
    /// <param name="hotkey"> The new hotkey. </param>
    /// <param name="validKeys"> The valid keys for the hotkey to have. If this is set and <paramref name="hotkey"/> is not contained, it will be set to <see cref="VirtualKey.NO_KEY"/>. </param>
    /// <returns> True if any change took place. </returns>
    public bool SetHotkey(VirtualKey hotkey, IReadOnlyList<VirtualKey>? validKeys = null)
    {
        if (Hotkey == hotkey || validKeys != null && !validKeys.Contains(hotkey))
            return false;

        if (hotkey == VirtualKey.NO_KEY)
            _modifiers = DoubleModifier.NoKey;

        Hotkey = hotkey;
        return true;
    }

    /// <summary>
    ///   Try to set the first modifier.
    ///   If the modifier is empty, the second modifier will be reset. 
    /// </summary>
    /// <param name="modifier1"> The new modifier. </param>
    /// <returns> True if any change took place. </returns>
    public bool SetModifier1(ModifierHotkey modifier1)
    {
        if (Hotkey is VirtualKey.NO_KEY)
            return false;

        return _modifiers.SetModifier1(modifier1);
    }

    /// <summary>
    ///   Try to set the second modifier.
    ///   If the first modifier is already the given key, resets this one instead.
    /// </summary>
    /// <param name="modifier2"> The new modifier. </param>
    /// <returns> True if any change took place. </returns>
    public bool SetModifier2(ModifierHotkey modifier2)
    {
        if (Hotkey is VirtualKey.NO_KEY)
            return false;

        return _modifiers.SetModifier2(modifier2);
    }

    /// <inheritdoc/>
    public bool Equals(ModifiableHotkey other)
        => Hotkey == other.Hotkey
         && _modifiers == other._modifiers;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is ModifiableHotkey other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine((int)Hotkey, _modifiers.GetHashCode());

    public static bool operator ==(ModifiableHotkey lhs, ModifiableHotkey rhs)
        => lhs.Equals(rhs);

    public static bool operator !=(ModifiableHotkey lhs, ModifiableHotkey rhs)
        => !lhs.Equals(rhs);

    /// <inheritdoc/>
    public override string ToString()
        => Hotkey is VirtualKey.NO_KEY
            ? "No Key"
            : _modifiers.Modifier1.Modifier is VirtualKey.NO_KEY
                ? Hotkey.GetFancyName()
                : _modifiers.Modifier2.Modifier is VirtualKey.NO_KEY
                    ? $"{_modifiers.Modifier1} + {Hotkey.GetFancyName()}"
                    : $"{_modifiers.Modifier1} + {_modifiers.Modifier2} + {Hotkey.GetFancyName()}";

    /// <summary> Check whether both required modifiers are currently held and the associated hotkey is pressed this frame according to ImGui. </summary>
    public bool IsPressed()
        => _modifiers.IsActive() && Im.Keyboard.IsPressed(Hotkey.ToImGuiKey());

    /// <summary> Serialize this object to JSON. </summary>
    /// <param name="j"> The JSON writer. </param>
    /// <returns> The JSON writer for method chaining. </returns>
    public Utf8JsonWriter Serialize(Utf8JsonWriter j)
    {
        if (Modifiers == DoubleModifier.NoKey)
        {
            j.WriteNumberValue((ushort)Hotkey);
        }
        else
        {
            j.WriteStartObject();
            j.WriteNumber("Hotkey"u8, (ushort)Hotkey);
            j.WriteNumber("Modifier1"u8, (ushort)Modifiers.Modifier1.Modifier);
            j.WriteIfNot("Modifier2"u8, (ushort)Modifiers.Modifier2.Modifier, (ushort)ModifierHotkey.NoKey.Modifier);
            j.WriteEndObject();
        }

        return j;
    }

    /// <summary> Try deserializing a modifiable hotkey from the JsonReader. </summary>
    /// <param name="j"> The JSON reader. </param>
    /// <param name="ret"> The deserialized value. </param>
    /// <param name="allowNull"> Whether to parse a null-token as no key. </param>
    /// <returns> True if the object could be parsed, false otherwise. </returns>
    public static bool TryDeserialize(ref Utf8JsonReader j, out ModifiableHotkey ret, bool allowNull)
    {
        if (j.TokenType is JsonTokenType.Null)
        {
            ret = new ModifiableHotkey();
            return allowNull;
        }

        if (j.TokenType is JsonTokenType.Number)
        {
            if (j.TryGetUInt16(out var m))
            {
                ret = new ModifiableHotkey((VirtualKey)m);
                return true;
            }

            ret = new ModifiableHotkey();
            return false;
        }

        if (j.TokenType is not JsonTokenType.StartObject)
        {
            ret = new ModifiableHotkey();
            return false;
        }

        var limit = j.CreateObjectLimit();
        var hot   = VirtualKey.NO_KEY;
        var mod1  = ModifierHotkey.NoKey.Modifier;
        var mod2  = ModifierHotkey.NoKey.Modifier;
        while (limit.Read(ref j))
        {
            if (j.NumberProperty("Hotkey"u8, out ushort h))
                hot = (VirtualKey)h;
            else if (j.NumberProperty("Modifier1"u8, out ushort m1))
                mod1 = new ModifierHotkey((VirtualKey)m1);
            else if (j.NumberProperty("Modifier2"u8, out ushort m2))
                mod2 = new ModifierHotkey((VirtualKey)m2);
            else
                j.Skip();
        }

        ret = new ModifiableHotkey(hot, mod1, mod2);
        return true;
    }

    private sealed class Converter : JsonConverter<ModifiableHotkey>
    {
        public override void WriteJson(JsonWriter writer, ModifiableHotkey value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Hotkey");
            writer.WriteValue((ushort)value.Hotkey);
            if (value._modifiers.Modifier1 != ModifierHotkey.NoKey)
            {
                writer.WritePropertyName("Modifier1");
                writer.WriteValue((ushort)value._modifiers.Modifier1.Modifier);
                if (value._modifiers.Modifier2 != ModifierHotkey.NoKey)
                {
                    writer.WritePropertyName("Modifier2");
                    writer.WriteValue((ushort)value._modifiers.Modifier2.Modifier);
                }
            }

            writer.WriteEndObject();
        }

        public override ModifiableHotkey ReadJson(JsonReader reader, Type objectType, ModifiableHotkey existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var data = serializer.Deserialize<Data>(reader);
            return new ModifiableHotkey((VirtualKey)data.Hotkey, new ModifierHotkey((VirtualKey)(data.Modifier1 ?? 0)),
                new ModifierHotkey((VirtualKey)(data.Modifier2 ?? 0)));
        }

        private record struct Data(ushort Hotkey, ushort? Modifier1, ushort? Modifier2);
    }
}
