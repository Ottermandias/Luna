namespace Luna;

/// <summary> Widgets to edit sets, and bit fields that are conceptually sets. </summary>
public static class SetButtons
{
    /// <summary> Draws an editor for a bit field that is conceptually a set, presented as a group of tag-like buttons, the non-member bits being folded into a combo-like button. </summary>
    /// <param name="id"> The ID of this editor. </param>
    /// <param name="value"> The bit field to edit. </param>
    /// <param name="universe"> A mask containing all the bits that may be added to the bit field. </param>
    /// <param name="toLabel"> A function that turns an individual bit into the corresponding label. </param>
    /// <param name="itemsDescription"> A description of several items of the set. Used in phrases such as "add all the {<paramref name="itemsDescription"/>}". </param>
    /// <param name="readOnly"> Whether this editor shall be read-only. </param>
    /// <typeparam name="T"> The backing primitive of the bit field. </typeparam>
    /// <returns> Whether the bit field was changed in any way in the current frame. </returns>
    public static bool DrawCombo<T>(Utf8LabelHandler id, ref T value, T universe, Func<T, StringU8> toLabel, StringU8 itemsDescription,
        bool readOnly = false)
        where T : unmanaged, IBinaryInteger<T>
    {
        var first    = true;
        var changed  = false;
        var nonEmpty = value != T.Zero;
        var control  = Im.Io.KeyControl;

        universe &= ~value;

        using var _     = Im.Id.Push(ref id);
        using var group = Im.Group();
        using var dis   = Im.Disabled(readOnly);

        var remainingBits = value;
        while (remainingBits != T.Zero)
        {
            // Extract the least significant bit.
            var bit   = unchecked(remainingBits & -remainingBits);
            var label = toLabel(bit);
            TrySameLine(Im.Font.CalculateButtonSize(label).X, ref first);
            Im.Button(label);
            var delete = !readOnly && control && Im.Item.RightClicked();
            if (!readOnly)
                Im.Tooltip.OnHover("Hold control and right-click to delete."u8);
            if (delete)
            {
                value   &= ~bit;
                changed =  true;
            }

            remainingBits &= ~bit;
        }

        if (nonEmpty && !readOnly)
        {
            TrySameLine(Im.Style.FrameHeight, ref first);
            if (ImEx.Icon.Button(LunaStyle.DeleteIcon, StringU8.Empty, !control))
            {
                value   = T.Zero;
                changed = true;
            }

            Im.Tooltip.OnHover($"Hold control and click to delete all {itemsDescription}.");
        }

        if (universe == T.Zero || readOnly)
            return changed;

        if (nonEmpty)
            TrySameLine(Im.Style.FrameHeight, ref first);
        if (ImEx.Icon.Button(LunaStyle.AddObjectIcon, StringU8.Empty))
            Im.Popup.Open("Add"u8);
        Im.Tooltip.OnHover($"Add {itemsDescription}");

        using var popup = Im.Popup.Begin("Add"u8);
        if (!popup)
            return changed;

        using (Im.Disabled(!control))
        {
            if (Im.Selectable("All"u8))
            {
                value   |= universe;
                changed =  true;
            }
        }

        Im.Tooltip.OnHover($"Hold control and click to add all {itemsDescription}.");
        Im.Separator();

        remainingBits = universe;
        while (remainingBits != T.Zero)
        {
            // Extract the least significant bit.
            var bit = unchecked(remainingBits & -remainingBits);
            if (Im.Selectable(toLabel(bit), flags: control && bit != universe ? SelectableFlags.NoAutoClosePopups : 0))
            {
                value   |= bit;
                changed =  true;
            }

            remainingBits &= ~bit;
        }

        return changed;
    }

    /// <inheritdoc cref="DrawCombo{T}(Utf8LabelHandler,ref T,T,Func{T,StringU8},StringU8,bool)"/>
    /// <typeparam name="TEnum"> The enum of the bit field. </typeparam>
    /// <returns> Whether the bit field was changed in any way in the current frame. </returns>
    public static unsafe bool DrawComboEnum<TEnum>(Utf8LabelHandler id, ref TEnum value, TEnum universe, Func<TEnum, StringU8> toLabel,
        StringU8 itemsDescription, bool readOnly = false)
        where TEnum : unmanaged, Enum
    {
        if (sizeof(TEnum) is 1)
            return DrawCombo(id, ref Unsafe.As<TEnum, byte>(ref value), Unsafe.BitCast<TEnum, byte>(universe),
            value => toLabel(Unsafe.BitCast<byte, TEnum>(value)), itemsDescription, readOnly);
        if (sizeof(TEnum) is 2)
            return DrawCombo(id, ref Unsafe.As<TEnum, ushort>(ref value), Unsafe.BitCast<TEnum, ushort>(universe),
                value => toLabel(Unsafe.BitCast<ushort, TEnum>(value)), itemsDescription, readOnly);
        if (sizeof(TEnum) is 4)
            return DrawCombo(id, ref Unsafe.As<TEnum, uint>(ref value), Unsafe.BitCast<TEnum, uint>(universe),
                value => toLabel(Unsafe.BitCast<uint, TEnum>(value)), itemsDescription, readOnly);
        if (sizeof(TEnum) is 8)
            return DrawCombo(id, ref Unsafe.As<TEnum, ulong>(ref value), Unsafe.BitCast<TEnum, ulong>(universe),
                value => toLabel(Unsafe.BitCast<ulong, TEnum>(value)), itemsDescription, readOnly);

        throw new ArgumentException($"Invalid Enum type {typeof(TEnum)} has size {sizeof(TEnum)} which is not supported.");
    }

    /// <summary> Draws an editor for a set, presented as a group of tag-like buttons, the non-member values being folded into a combo-like button. </summary>
    /// <param name="id"> The ID of this editor. </param>
    /// <param name="value"> The set to edit. </param>
    /// <param name="universe"> A collection of the values that may be added to the set. It should not contain the values already in the set. </param>
    /// <param name="toLabel"> A function that turns a value into the corresponding label. </param>
    /// <param name="itemsDescription"> A description of several items of the set. Used in phrases such as "add all the {<paramref name="itemsDescription"/>}". </param>
    /// <param name="readOnly"> Whether this editor shall be read-only. </param>
    /// <typeparam name="T"> The element type of the set. </typeparam>
    /// <returns> Whether the set was changed in any way in the current frame. </returns>
    public static bool DrawCombo<T>(Utf8LabelHandler id, ISet<T> value, IEnumerable<T> universe, Func<T, StringU8> toLabel,
        StringU8 itemsDescription, bool readOnly = false)
    {
        var first    = true;
        var changed  = false;
        var nonEmpty = value.Count > 0;
        var control  = Im.Io.KeyControl;

        using var _     = Im.Id.Push(ref id);
        using var group = Im.Group();
        using var dis   = Im.Disabled(readOnly);

        var willRemove   = false;
        T   itemToRemove = default!;

        foreach (var item in value)
        {
            var label = toLabel(item);
            TrySameLine(Im.Font.CalculateButtonSize(label).X, ref first);
            Im.Button(label);
            var delete = !readOnly && control && Im.Item.RightClicked();
            if (!readOnly)
                Im.Tooltip.OnHover("Hold control and right-click to delete."u8);
            if (delete)
            {
                willRemove   = true;
                itemToRemove = item;
            }
        }

        if (willRemove)
        {
            value.Remove(itemToRemove);
            changed = true;
        }

        if (nonEmpty && !readOnly)
        {
            TrySameLine(Im.Style.FrameHeight, ref first);
            if (ImEx.Icon.Button(LunaStyle.DeleteIcon, StringU8.Empty, !control))
            {
                value.Clear();
                changed = true;
            }
            Im.Tooltip.OnHover($"Hold control and click to delete all {itemsDescription}.");
        }

        if (readOnly)
            return changed;

        if (!universe.TryGetNonEnumeratedCount(out var count))
            count = -1;

        if (count is 0)
            return changed;

        if (nonEmpty)
            TrySameLine(Im.Style.FrameHeight, ref first);
        if (ImEx.Icon.Button(LunaStyle.AddObjectIcon, StringU8.Empty))
            Im.Popup.Open("Add"u8);
        Im.Tooltip.OnHover($"Add {itemsDescription}");

        using var popup = Im.Popup.Begin("Add"u8);
        if (!popup)
            return changed;

        using (Im.Disabled(!control))
        {
            if (Im.Selectable("All"u8))
            {
                foreach (var item in universe)
                    value.Add(item);
                return true;
            }
        }

        Im.Tooltip.OnHover($"Hold control and click to add all {itemsDescription}.");
        Im.Separator();

        foreach (var item in universe)
        {
            if (!value.Contains(item) && Im.Selectable(toLabel(item), flags: control && count is 1 ? SelectableFlags.NoAutoClosePopups : 0))
            {
                value.Add(item);
                changed = true;
            }
        }

        return changed;
    }

    /// <summary> Draws an editor for a bit field that is conceptually a set, presented as a group of checkable tag-like buttons, all bits being always visible. </summary>
    /// <param name="id"> The ID of this editor. </param>
    /// <param name="value"> The bit field to edit. </param>
    /// <param name="universe"> A mask containing all the bits that may be added to the bit field. </param>
    /// <param name="toLabel"> A function that turns an individual bit into the corresponding label. </param>
    /// <param name="itemsDescription"> A description of several items of the set. Used in phrases such as "add all the {<paramref name="itemsDescription"/>}". </param>
    /// <param name="readOnly"> Whether this editor shall be read-only. </param>
    /// <param name="memberBackground"> A background color for member bits. </param>
    /// <param name="nonMemberBackground"> A background color for non-member bits. </param>
    /// <typeparam name="T"> The backing primitive of the bit field. </typeparam>
    /// <returns> Whether the bit field was changed in any way in the current frame. </returns>
    public static bool DrawCheckables<T>(Utf8LabelHandler id, ref T value, T universe, Func<T, StringU8> toLabel, StringU8 itemsDescription,
        bool readOnly = false, in ColorParameter memberBackground = default, in ColorParameter nonMemberBackground = default)
        where T : unmanaged, IBinaryInteger<T>
    {
        var first   = true;
        var changed = false;

        using var _     = Im.Id.Push(ref id);
        using var group = Im.Group();
        using var dis   = Im.Disabled(readOnly);

        var remainingBits = universe | value;
        while (remainingBits != T.Zero)
        {
            // Extract the least significant bit.
            var bit   = unchecked(remainingBits & -remainingBits);
            var label = toLabel(bit);
            TrySameLine(ImEx.Icon.CalculateLabeledButtonSize(LunaStyle.TrueIcon, label).X, ref first);
            using var color = ImGuiColor.Button.Push((value & bit) == bit ? memberBackground : nonMemberBackground);
            if (ImEx.Icon.LabeledButton(LunaStyle.TrueIcon, label, iconFlags: (value & bit) == bit ? 0 : ImEx.Icon.IconFlags.HideIcon)
             && !readOnly)
            {
                value   ^= bit;
                changed =  true;
            }

            remainingBits &= ~bit;
        }

        if (((value & universe) != universe || value != T.Zero) && !readOnly)
        {
            TrySameLine(Im.Style.FrameHeight, ref first);
            using var color = ImGuiColor.Button.Push((value & universe) == universe ? memberBackground : nonMemberBackground);
            if (ImEx.Icon.Button(LunaStyle.ToggleBulkIcon, StringU8.Empty, !Im.Io.KeyControl))
            {
                if ((value & universe) == universe)
                    value = T.Zero;
                else
                    value |= universe;
                changed = true;
            }
            Im.Tooltip.OnHover($"Hold control and click to {((value & universe) == universe ? "delete" : "add")} all {itemsDescription}.");
        }

        return changed;
    }

    /// <inheritdoc cref="DrawCheckables{T}(Utf8LabelHandler,ref T,T,Func{T,StringU8},StringU8,bool,in ColorParameter,in ColorParameter)"/>
    /// <typeparam name="TEnum"> The enum of the bit field. </typeparam>
    /// <returns> Whether the bit field was changed in any way in the current frame. </returns>
    public static unsafe bool DrawCheckablesEnum<TEnum>(Utf8LabelHandler id, ref TEnum value, TEnum universe, Func<TEnum, StringU8> toLabel,
        StringU8 itemsDescription, bool readOnly = false, in ColorParameter memberBackground = default,
        in ColorParameter nonMemberBackground = default)
        where TEnum : unmanaged, Enum
    {
        if (sizeof(TEnum) is 1)
            return DrawCheckables(id, ref Unsafe.As<TEnum, byte>(ref value), Unsafe.BitCast<TEnum, byte>(universe), value => toLabel(Unsafe.BitCast<byte, TEnum>(value)), itemsDescription, readOnly, in memberBackground, in nonMemberBackground);
        if (sizeof(TEnum) is 2)
            return DrawCheckables(id, ref Unsafe.As<TEnum, ushort>(ref value), Unsafe.BitCast<TEnum, ushort>(universe), value => toLabel(Unsafe.BitCast<ushort, TEnum>(value)), itemsDescription, readOnly, in memberBackground, in nonMemberBackground);
        if (sizeof(TEnum) is 4)
            return DrawCheckables(id, ref Unsafe.As<TEnum, uint>(ref value), Unsafe.BitCast<TEnum, uint>(universe), value => toLabel(Unsafe.BitCast<uint, TEnum>(value)), itemsDescription, readOnly, in memberBackground, in nonMemberBackground);
        if (sizeof(TEnum) is 8)
            return DrawCheckables(id, ref Unsafe.As<TEnum, ulong>(ref value), Unsafe.BitCast<TEnum, ulong>(universe), value => toLabel(Unsafe.BitCast<ulong, TEnum>(value)), itemsDescription, readOnly, in memberBackground, in nonMemberBackground);

        throw new ArgumentException($"Invalid Enum type {typeof(TEnum)} has size {sizeof(TEnum)} which is not supported.");
    }

    /// <summary> Draws an editor for a set that is conceptually a set, presented as a group of checkable tag-like buttons, all values being always visible. </summary>
    /// <param name="id"> The ID of this editor. </param>
    /// <param name="value"> The set to edit. </param>
    /// <param name="universe"> A collection of the values that may be added to the set. It should contain the values already in the set. May be enumerated multiple times per call. </param>
    /// <param name="toLabel"> A function that turns a value into the corresponding label. </param>
    /// <param name="itemsDescription"> A description of several items of the set. Used in phrases such as "add all the {<paramref name="itemsDescription"/>}". </param>
    /// <param name="readOnly"> Whether this editor shall be read-only. </param>
    /// <param name="memberBackground"> A background color for member values. </param>
    /// <param name="nonMemberBackground"> A background color for non-member values. </param>
    /// <typeparam name="T"> The element type of the set. </typeparam>
    /// <returns> Whether the set was changed in any way in the current frame. </returns>
    public static bool DrawCheckables<T>(Utf8LabelHandler id, ISet<T> value, IEnumerable<T> universe, Func<T, StringU8> toLabel,
        StringU8 itemsDescription, bool readOnly = false, in ColorParameter memberBackground = default,
        in ColorParameter nonMemberBackground = default)
    {
        var first   = true;
        var changed = false;

        using var _     = Im.Id.Push(ref id);
        using var group = Im.Group();
        using var dis   = Im.Disabled(readOnly);

        var remaining      = new HashSet<T>(value);
        var nonMemberCount = 0;

        foreach (var item in universe)
        {
            var member = remaining.Remove(item);
            var label  = toLabel(item);
            TrySameLine(ImEx.Icon.CalculateLabeledButtonSize(LunaStyle.TrueIcon, label).X, ref first);
            using var color = ImGuiColor.Button.Push(member ? memberBackground : nonMemberBackground);
            if (ImEx.Icon.LabeledButton(LunaStyle.TrueIcon, label, iconFlags: member ? 0 : ImEx.Icon.IconFlags.HideIcon) && !readOnly)
            {
                if (member)
                    value.Remove(item);
                else
                    value.Add(item);
                changed = true;
            }

            if (!member)
                ++nonMemberCount;
        }

        foreach (var item in remaining)
        {
            var label = toLabel(item);
            TrySameLine(ImEx.Icon.CalculateLabeledButtonSize(LunaStyle.TrueIcon, label).X, ref first);
            using var color = ImGuiColor.Button.Push(memberBackground);
            if (ImEx.Icon.LabeledButton(LunaStyle.TrueIcon, label) && !readOnly)
            {
                value.Remove(item);
                changed = true;
            }
        }

        if ((nonMemberCount > 0 || value.Count > 0) && !readOnly)
        {
            TrySameLine(Im.Style.FrameHeight, ref first);
            using var color = ImGuiColor.Button.Push(nonMemberCount is 0 ? memberBackground : nonMemberBackground);
            if (ImEx.Icon.Button(LunaStyle.ToggleBulkIcon,
                    $"Hold control and click to {(nonMemberCount is 0 ? "delete" : "add")} all {itemsDescription}.",
                    !Im.Io.KeyControl))
            {
                if (nonMemberCount is 0)
                    value.Clear();
                else
                {
                    foreach (var item in universe)
                        value.Add(item);
                }

                changed = true;
            }
        }

        return changed;
    }

    private static void TrySameLine(float minWidth, ref bool first)
    {
        if (first)
        {
            first = false;
            return;
        }

        Im.Line.Same();
        if (Im.ContentRegion.Available.X < minWidth + Im.Style.ItemSpacing.X)
            Im.Line.New();
    }
}
