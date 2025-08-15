using Dalamud.Game.ClientState.Keys;

namespace Luna;

/// <summary> A wrapper for classical modifier keys, Control, Alt and Shift. </summary>
/// <param name="modifier"> The actual modifier this object shall represent as <see cref="VirtualKey"/>. </param>
[method: JsonConstructor]
public readonly struct ModifierHotkey(VirtualKey modifier) : IEquatable<ModifierHotkey>
{
    /// <summary> No modifier key. </summary>
    public static readonly ModifierHotkey NoKey = new(VirtualKey.NO_KEY);

    /// <summary> The Shift modifier. </summary>
    public static readonly ModifierHotkey Shift = new(VirtualKey.SHIFT);

    /// <summary> The Control modifier. </summary>
    public static readonly ModifierHotkey Control = new(VirtualKey.CONTROL);

    /// <summary> The Alt modifier. </summary>
    public static readonly ModifierHotkey Alt = new(VirtualKey.MENU);

    /// <summary> All valid modifiers (None, Control, Shift and Alt). </summary>
    public static readonly ModifierHotkey[] ValidModifiers =
    [
        VirtualKey.NO_KEY,
        VirtualKey.CONTROL,
        VirtualKey.SHIFT,
        VirtualKey.MENU,
    ];

    /// <summary> All valid modifiers as <see cref="VirtualKey"/>, see <see cref="ValidModifiers"/>. </summary>
    public static readonly VirtualKey[] ValidKeys = ValidModifiers.Select(m => m.Modifier).ToArray();

    /// <summary> The actual modifier this object represents as <see cref="VirtualKey"/>. </summary>
    public readonly VirtualKey Modifier = modifier switch
    {
        VirtualKey.NO_KEY   => VirtualKey.NO_KEY,
        VirtualKey.CONTROL  => VirtualKey.CONTROL,
        VirtualKey.MENU     => VirtualKey.MENU,
        VirtualKey.SHIFT    => VirtualKey.SHIFT,
        VirtualKey.LCONTROL => VirtualKey.CONTROL,
        VirtualKey.RCONTROL => VirtualKey.CONTROL,
        VirtualKey.LMENU    => VirtualKey.MENU,
        VirtualKey.RMENU    => VirtualKey.MENU,
        VirtualKey.LSHIFT   => VirtualKey.SHIFT,
        VirtualKey.RSHIFT   => VirtualKey.SHIFT,
        _                   => VirtualKey.NO_KEY,
    };

    public static implicit operator VirtualKey(ModifierHotkey k)
        => k.Modifier;

    public static implicit operator ModifierHotkey(VirtualKey k)
        => new(k);

    /// <inheritdoc/>
    public bool Equals(ModifierHotkey other)
        => Modifier == other.Modifier;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is ModifierHotkey other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
        => (int)Modifier;

    public static bool operator ==(ModifierHotkey lhs, ModifierHotkey rhs)
        => lhs.Modifier == rhs.Modifier;

    public static bool operator !=(ModifierHotkey lhs, ModifierHotkey rhs)
        => lhs.Modifier != rhs.Modifier;

    /// <inheritdoc/>
    public override string ToString()
        => Modifier.GetFancyName();


    /// <summary> Check whether this modifier is currently held according to ImGui. </summary>
    public bool IsActive()
        => Modifier switch
        {
            VirtualKey.NO_KEY  => true,
            VirtualKey.CONTROL => Im.Io.KeyControl,
            VirtualKey.MENU    => Im.Io.KeyAlt,
            VirtualKey.SHIFT   => Im.Io.KeyShift,
            _                  => false,
        };
}
