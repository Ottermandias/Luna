using Dalamud.Game.ClientState.Keys;

namespace Luna;

/// <summary> Doubled <see cref="ModifierHotkey"/> to handle one or two required modifiers. </summary>
public struct DoubleModifier : IEquatable<DoubleModifier>
{
    /// <summary> A static instance representing no required modifiers. </summary>
    public static readonly DoubleModifier NoKey = new();

    /// <summary> The first required modifier. If this is <see cref="ModifierHotkey.NoKey"/>, <see cref="Modifier2"/>> can not be set either. They can not be equal otherwise. </summary>
    public ModifierHotkey Modifier1 { get; private set; } = ModifierHotkey.NoKey;

    /// <summary> The second required modifier. Can only be set if <see cref="Modifier1"/> is not <see cref="ModifierHotkey.NoKey"/> and can not be equal to it.  </summary>
    public ModifierHotkey Modifier2 { get; private set; } = ModifierHotkey.NoKey;

    /// <summary> Create an empty double modifier. </summary>
    public DoubleModifier()
    { }

    /// <summary> Create a modifier requiring at most one modifier. </summary>
    /// <param name="modifier1"> The single required modifier. </param>
    public DoubleModifier(ModifierHotkey modifier1)
    {
        SetModifier1(modifier1);
    }

    /// <summary> Create a modifier possibly requiring both modifiers. </summary>
    /// <param name="modifier1"> The first required modifier. </param>
    /// <param name="modifier2"> The second required modifier. </param>
    [JsonConstructor]
    public DoubleModifier(ModifierHotkey modifier1, ModifierHotkey modifier2)
    {
        SetModifier1(modifier1);
        SetModifier2(modifier2);
    }

    /// <summary> Use either this modifier or a default value if it is set to empty, so that it never checks no modifiers at all. </summary>
    public DoubleModifier ForcedModifier(DoubleModifier defaultValue)
        => Modifier1 == ModifierHotkey.NoKey ? defaultValue : this;

    /// <summary> Try to set the first modifier. If the modifier is empty, the second modifier will be reset. </summary>
    /// <param name="key"> The new first modifier. </param>
    /// <returns> True if any change took place. </returns>
    public bool SetModifier1(ModifierHotkey key)
    {
        if (Modifier1 == key)
            return false;

        if (key == VirtualKey.NO_KEY || key == Modifier2)
            Modifier2 = VirtualKey.NO_KEY;

        Modifier1 = key;
        return true;
    }

    /// <summary>
    ///   Try to set the second modifier. The first modifier can not be empty.
    ///   If the new value is equal to the first modifier, resets the second modifier instead. </summary>
    /// <param name="key"> The new first modifier. </param>
    /// <returns> True if any change took place. </returns>
    public bool SetModifier2(ModifierHotkey key)
    {
        if (Modifier2 == key || Modifier1 == ModifierHotkey.NoKey)
            return false;

        Modifier2 = Modifier1 == key ? VirtualKey.NO_KEY : key;
        return true;
    }

    /// <inheritdoc/>
    public bool Equals(DoubleModifier other)
        => Modifier1.Equals(other.Modifier1)
         && Modifier2.Equals(other.Modifier2);

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is DoubleModifier other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(Modifier1, Modifier2);

    public static bool operator ==(DoubleModifier lhs, DoubleModifier rhs)
        => lhs.Equals(rhs);

    public static bool operator !=(DoubleModifier lhs, DoubleModifier rhs)
        => !lhs.Equals(rhs);

    /// <inheritdoc/>
    public override string ToString()
        => Modifier2 != ModifierHotkey.NoKey
            ? $"{Modifier1} and {Modifier2}"
            : Modifier1.ToString();

    /// <summary> Check whether both required modifiers are currently held according to ImGui. </summary>
    public bool IsActive()
        => Modifier1.IsActive() && Modifier2.IsActive();
}
