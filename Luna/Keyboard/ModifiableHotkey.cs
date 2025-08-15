using Dalamud.Game.ClientState.Keys;

namespace Luna;

/// <summary> A single arbitrary hotkey with up to two modifiers. </summary>
public struct ModifiableHotkey : IEquatable<ModifiableHotkey>
{
    /// <summary> The hotkey to press. </summary>
    public VirtualKey Hotkey { get; private set; } = VirtualKey.NO_KEY;

    /// <summary> The optional modifiers. </summary>
    public DoubleModifier Modifiers { get; private set; } = DoubleModifier.NoKey;

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
            Modifiers = new DoubleModifier(modifier1);
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
            Modifiers = new DoubleModifier(modifier1, modifier2);
    }

    /// <summary> Create a hotkey with up to two modifiers, optionally checking against a set of valid keys. </summary>
    /// <param name="hotkey"> They hotkey to create. </param>
    /// <param name="modifiers"> The modifiers. See <see cref="DoubleModifier"/> for behavior. </param>
    /// <param name="validKeys"> The valid keys for the hotkey to have. If this is set and <paramref name="hotkey"/> is not contained, it will be set to <see cref="VirtualKey.NO_KEY"/>. </param>
    public ModifiableHotkey(VirtualKey hotkey, DoubleModifier modifiers, VirtualKey[]? validKeys = null)
    {
        SetHotkey(hotkey, validKeys);
        if (hotkey is not VirtualKey.NO_KEY)
            Modifiers = modifiers;
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
            Modifiers = DoubleModifier.NoKey;

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

        return Modifiers.SetModifier1(modifier1);
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

        return Modifiers.SetModifier2(modifier2);
    }

    /// <inheritdoc/>
    public bool Equals(ModifiableHotkey other)
        => Hotkey == other.Hotkey
         && Modifiers == other.Modifiers;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is ModifiableHotkey other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine((int)Hotkey, Modifiers.GetHashCode());

    public static bool operator ==(ModifiableHotkey lhs, ModifiableHotkey rhs)
        => lhs.Equals(rhs);

    public static bool operator !=(ModifiableHotkey lhs, ModifiableHotkey rhs)
        => !lhs.Equals(rhs);

    /// <inheritdoc/>
    public override string ToString()
        => Hotkey is VirtualKey.NO_KEY
            ? "No Key"
            : Modifiers.Modifier1.Modifier is VirtualKey.NO_KEY
                ? Hotkey.GetFancyName()
                : Modifiers.Modifier2.Modifier is VirtualKey.NO_KEY
                    ? $"{Modifiers.Modifier1} + {Hotkey.GetFancyName()}"
                    : $"{Modifiers.Modifier1} + {Modifiers.Modifier2} + {Hotkey.GetFancyName()}";

    /// <summary> Check whether both required modifiers are currently held and the associated hotkey is pressed this frame according to ImGui. </summary>
    public bool IsPressed()
        => Modifiers.IsActive() && Im.Keyboard.IsPressed(Hotkey.ToImGuiKey());
}
