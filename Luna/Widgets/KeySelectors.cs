using System.Collections.Frozen;
using Dalamud.Game.ClientState.Keys;

namespace Luna;

/// <summary> Utility for keybind selection. </summary>
public static class KeySelector
{
    private static FrozenDictionary<VirtualKey, StringU8>? _fancyNames = null;

    /// <summary> Get the fancy name of a key as a UTF8 string. </summary>
    public static FrozenDictionary<VirtualKey, StringU8> FancyNames
        => _fancyNames ??= Enum.GetValues<VirtualKey>().Distinct().ToFrozenDictionary(v => v, v => new StringU8(v.GetFancyName()));

    /// <summary> Regular combo to select a Dalamud virtual key from the given list of available keys. </summary>
    /// <param name="label"> The label for the combo as text. If this is a UTF8 string, it HAS to be null-terminated. </param>
    /// <param name="tooltip"> A tooltip shown on hover as text. Evaluated regardless of hover-state. </param>
    /// <param name="currentValue"> The current value of the key. </param>
    /// <param name="setter"> The setter invoked when a different key is selected. </param>
    /// <param name="keys"> The allowed keys for input. </param>
    /// <returns> True if a different key was selected and the setter was invoked in this frame. </returns>
    public static bool Combo(Utf8LabelHandler label, Utf8TextHandler tooltip, VirtualKey currentValue, Action<VirtualKey> setter,
        params IReadOnlyList<VirtualKey> keys)
    {
        using var id    = Im.Id.Push(label);
        using var combo = Im.Combo.Begin(label, FancyNames.TryGetValue(currentValue, out var preview) ? preview : "Unknown"u8);
        Im.Tooltip.OnHover(ref tooltip);
        if (!combo)
            return false;

        var ret = false;
        // Draw the actual combo values.
        foreach (var (key, name) in keys.SelectWhere(k => FancyNames.TryGetValue(k, out var n) ? (true, (k, n)) : (false, (k, StringU8.Empty))))
        {
            if (!Im.Selectable(name, currentValue == key) || currentValue == key)
                continue;

            setter(key);
            ret = true;
        }

        return ret;
    }

    /// <summary> Draw a combo that only allows valid modifier hotkeys as defined by <see cref="ModifierHotkey.ValidKeys"/>. </summary>
    /// <param name="label"> The label for the combo as text. If this is a UTF8 string, it HAS to be null-terminated. </param>
    /// <param name="tooltip"> A tooltip shown on hover as text. Evaluated regardless of hover-state. </param>
    /// <param name="currentValue"> The current value of the modifier key. </param>
    /// <param name="setter"> The setter invoked when a different modifier key is selected. </param>
    /// <returns> True if a different modifier key was selected and the setter was invoked in this frame. </returns>
    public static bool Modifier(Utf8LabelHandler label, Utf8TextHandler tooltip, ModifierHotkey currentValue, Action<ModifierHotkey> setter)
        => Combo(label, tooltip, currentValue, k => setter(k), ModifierHotkey.ValidKeys);

    /// <summary> Draw a combo for one or two modifiers at once. The second combo is only shown if the first modifier is set and indented. </summary>
    /// <param name="label"> The label for the combo as text. If this is a UTF8 string, it HAS to be null-terminated. </param>
    /// <param name="tooltip"> A tooltip shown on hover as text. Evaluated regardless of hover-state. </param>
    /// <param name="width"> The width of the first combo. </param>
    /// <param name="currentValue"> The current value of the modifier key. </param>
    /// <param name="setter"> The setter invoked when a different modifier key is selected in either combo. </param>
    /// <returns> True if a different modifier key was selected and the setter was invoked in this frame. </returns>
    /// <remarks> If the first modifier is set to <see cref="VirtualKey.NO_KEY"/>, the second is removed too. </remarks>
    public static bool DoubleModifier(Utf8LabelHandler label, Utf8TextHandler tooltip, float width, DoubleModifier currentValue,
        Action<DoubleModifier> setter)
    {
        var       changes = false;
        var       copy    = currentValue;
        using var id      = Im.Id.Push(label);
        Im.Item.SetNextWidth(width);
        changes |= Modifier(label, tooltip, currentValue.Modifier1, k => copy.SetModifier1(k));

        if (currentValue.Modifier1 != ModifierHotkey.NoKey)
        {
            using var indent = Im.Indent();
            Im.Item.SetNextWidth(width - indent.CurrentIndent);
            changes |= Modifier("Additional Modifier"u8,
                "Set another optional modifier key to be used in conjunction with the first modifier."u8,
                currentValue.Modifier2, k => copy.SetModifier2(k));
        }

        if (changes)
            setter(copy);
        return changes;
    }

    /// <summary>
    ///   A selector widget for a full modifiable key, i.e. one key and up to two modifier hotkeys.
    ///   The second modifier combo is only shown if the first modifier is set,
    ///   and the first modifier combo is only shown if the key is set.
    ///   Both modifiers are indented. </summary>
    /// <param name="label"> The label for the combo as text. If this is a UTF8 string, it HAS to be null-terminated. </param>
    /// <param name="tooltip"> A tooltip shown on hover as text. Evaluated regardless of hover-state. </param>
    /// <param name="width"> The width of the first combo. </param>
    /// <param name="currentValue"> The current value of the full hotkey. </param>
    /// <param name="setter"> The setter invoked when a different key is selected in any of the combos. </param>
    /// <param name="keys"> The list of allowed main keys to select. </param>
    /// <returns> True if a different key was selected in any of the combos and the setter was invoked in this frame. </returns>
    /// <remarks> If an earlier key is set to No Key, all subsequent keys are set to No Key, too. </remarks>
    public static bool ModifiableKeySelector(Utf8LabelHandler label, Utf8TextHandler tooltip, float width, ModifiableHotkey currentValue,
        Action<ModifiableHotkey> setter, params IReadOnlyList<VirtualKey> keys)
    {
        using var id   = Im.Id.Push(label);
        var       copy = currentValue;
        Im.Item.SetNextWidth(width);
        var changes = Combo(label, tooltip, currentValue.Hotkey, k => copy.SetHotkey(k), keys);

        if (currentValue.Hotkey is not VirtualKey.NO_KEY)
        {
            using var indent = Im.Indent();
            width -= indent.CurrentIndent;
            Im.Item.SetNextWidth(width);
            changes |= Modifier("Modifier"u8,     "Set an optional modifier key to be used in conjunction with the selected hotkey."u8,
                currentValue.Modifiers.Modifier1, k => copy.SetModifier1(k));

            if (currentValue.Modifiers.Modifier1 != ModifierHotkey.NoKey)
            {
                Im.Item.SetNextWidth(width);
                changes |= Modifier("Additional Modifier"u8,
                    "Set another optional modifier key to be used in conjunction with the selected hotkey and the first modifier."u8,
                    currentValue.Modifiers.Modifier2, k => copy.SetModifier2(k));
            }
        }

        if (changes)
            setter(copy);
        return changes;
    }
}
