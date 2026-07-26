using ImSharp.ImNodes;

namespace Luna;

/// <summary> A combo to select between different supported color reference types. </summary>
/// <typeparam name="TColorId"> The custom color ID type. </typeparam>
/// <typeparam name="TColorData"> The data for the custom color ID type. </typeparam>
public class ColorTypeCombo<TColorId, TColorData>(ColorDictionary<TColorId, TColorData> dictionary)
    : SimpleFilterCombo<ColorDataUnion.TypeEnum>(SimpleFilterType.Text)
    where TColorId : unmanaged, Enum
    where TColorData : IColorData<TColorId>
{
    private TColorId _parentId;
    private bool     _enableCustom;

    /// <summary> Draw the combo for the current type and a specific parent ID. Custom Values are only available to select here when there are actual options for this. </summary>
    /// <param name="parentId"> The color ID for which this combo is drawn. </param>
    /// <param name="value"> The current type of the associated value for the color ID. </param>
    /// <param name="newValue"> On selection, a new associated value for the color ID that may be a defaulted reference. </param>
    /// <param name="width"> The width of the selector. </param>
    /// <returns> True if a type was selected this frame. </returns>
    public bool Draw(TColorId parentId, ColorDataUnion.TypeEnum value, out ColorDataUnion newValue, float width)
    {
        _parentId = parentId;
        if (!Draw("##Type"u8, ref value, "Choose a reference to another color type."u8, width))
        {
            newValue = ColorDataUnion.Default;
            return false;
        }

        newValue = value switch
        {
            ColorDataUnion.TypeEnum.Default => ColorDataUnion.Default,
            ColorDataUnion.TypeEnum.Const   => ColorDataUnion.Default,
            ColorDataUnion.TypeEnum.Self => ColorDataUnion.FromSelf(EnumExtensions.get_Values<TColorId>()
                .First(i => !dictionary.CheckForCycles(_parentId, ColorDataUnion.FromSelf(i)))),
            ColorDataUnion.TypeEnum.ImGui   => new ColorDataUnion(ImGuiColor.Text),
            ColorDataUnion.TypeEnum.ImNodes => new ColorDataUnion(ImNodesColor.Pin),
            ColorDataUnion.TypeEnum.Dalamud => new ColorDataUnion(DalamudColor.SuccessForeground),
            ColorDataUnion.TypeEnum.Luna    => new ColorDataUnion(default(LunaColor)),
            _                               => ColorDataUnion.Default,
        };
        return true;
    }

    /// <summary> The supported color types. </summary>
    private static readonly (ColorDataUnion.TypeEnum, StringPair)[] Data =
    [
        (ColorDataUnion.TypeEnum.Default, new StringPair("Defaulted")),
        (ColorDataUnion.TypeEnum.Const, new StringPair("Constant Color")),
        (ColorDataUnion.TypeEnum.ImGui, new StringPair("ImGui")),
        (ColorDataUnion.TypeEnum.ImNodes, new StringPair("ImNodes")),
        (ColorDataUnion.TypeEnum.Dalamud, new StringPair("Dalamud")),
        // (ColorDataUnion.TypeEnum.Luna, new StringPair("Luna")), // Not supported yet
    ];

    /// <inheritdoc/>
    public override StringU8 DisplayString(in ColorDataUnion.TypeEnum value)
        => value switch
        {
            ColorDataUnion.TypeEnum.Default => Data[0].Item2,
            ColorDataUnion.TypeEnum.Const   => Data[1].Item2,
            ColorDataUnion.TypeEnum.Self    => TColorData.Parent,
            ColorDataUnion.TypeEnum.ImGui   => Data[2].Item2,
            ColorDataUnion.TypeEnum.ImNodes => Data[3].Item2,
            ColorDataUnion.TypeEnum.Dalamud => Data[4].Item2,
            ColorDataUnion.TypeEnum.Luna    => Data[5].Item2,
            _                               => new StringU8("Unknown"u8),
        };

    /// <inheritdoc/>
    public override string FilterString(in ColorDataUnion.TypeEnum value)
        => value switch
        {
            ColorDataUnion.TypeEnum.Default => Data[0].Item2,
            ColorDataUnion.TypeEnum.Const   => Data[1].Item2,
            ColorDataUnion.TypeEnum.Self    => TColorData.Parent.ToString(),
            ColorDataUnion.TypeEnum.ImGui   => Data[2].Item2,
            ColorDataUnion.TypeEnum.ImNodes => Data[3].Item2,
            ColorDataUnion.TypeEnum.Dalamud => Data[4].Item2,
            ColorDataUnion.TypeEnum.Luna    => Data[5].Item2,
            _                               => "Unknown",
        };

    /// <inheritdoc/>
    public override IEnumerable<ColorDataUnion.TypeEnum> GetBaseItems()
    {
        _enableCustom = EnumExtensions.get_Values<TColorId>().Any(i => !dictionary.CheckForCycles(_parentId, ColorDataUnion.FromSelf(i)));
        return Data.Select(d => d.Item1).Append(ColorDataUnion.TypeEnum.Self);
    }

    protected internal override bool DrawItem(in SimpleCacheItem<ColorDataUnion.TypeEnum> item, int globalIndex, bool selected)
    {
        bool ret;
        var  disabled = item.Item is ColorDataUnion.TypeEnum.Self && !_enableCustom;
        using (Im.Disabled(disabled))
        {
            ret = base.DrawItem(item, globalIndex, selected);
        }

        if (disabled)
            Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled,
                $"There is no available reference to {TColorData.Parent} colors that would not cause a cyclic dependency.");
        return ret;
    }
}

/// <summary> A basic color for enumeration types. </summary>
/// <typeparam name="TEnum"> The enumeration type. </typeparam>
public class EnumColorCombo<TEnum>() : SimpleFilterCombo<TEnum>(SimpleFilterType.Text)
    where TEnum : unmanaged, Enum
{
    /// <inheritdoc/>
    public override StringU8 DisplayString(in TEnum value)
        => value.StringU8;

    /// <inheritdoc/>
    public override string FilterString(in TEnum value)
        => value.String;

    /// <inheritdoc/>
    public override IEnumerable<TEnum> GetBaseItems()
        => EnumExtensions.get_Values<TEnum>();
}

/// <summary> A combo for ImGui color references. </summary>
public sealed class ImGuiColorCombo : EnumColorCombo<ImGuiColor>
{
    /// <inheritdoc/>
    public override StringU8 DisplayString(in ImGuiColor value)
        => Im.Color.GetNameOwned(value);

    /// <inheritdoc/>
    public override string FilterString(in ImGuiColor value)
        => Im.Color.GetNameUtf16(value);
}

/// <summary> A combo for ImNodes color references. </summary>
public sealed class ImNodesColorCombo : EnumColorCombo<ImNodesColor>;

/// <summary> A combo for Dalamud color references. </summary>
public sealed class DalamudColorCombo : EnumColorCombo<DalamudColor>;

/// <summary> A combo for Luna color references. </summary>
public sealed class LunaColorCombo : EnumColorCombo<LunaColor>;

/// <summary> A combo for custom color references that checks for cycles. </summary>
public class CustomColorCombo<TColorId, TColorData>(ColorDictionary<TColorId, TColorData> dictionary) : EnumColorCombo<TColorId>
    where TColorId : unmanaged, Enum
    where TColorData : IColorData<TColorId>
{
    private TColorId _parentId;

    /// <summary> Draw while setting the relevant color ID to check against cyclic dependencies. </summary>
    /// <param name="parentId"> The color ID that is checked for cycles. </param>
    /// <param name="value"> The current value for that color ID. </param>
    /// <param name="newValue"> On selection, the new value for that color ID. </param>
    /// <param name="width"> The width of the selector. </param>
    /// <returns> True when a new value was selected this frame. </returns>
    public bool Draw(TColorId parentId, TColorId value, out TColorId newValue, float width)
    {
        _parentId = parentId;
        return Draw("##custom"u8, value, $"Choose a reference to another {TColorData.Parent} color.", width,
            out newValue);
    }

    /// <inheritdoc/>
    protected internal override bool DrawItem(in SimpleCacheItem<TColorId> item, int globalIndex, bool selected)
    {
        if (dictionary.CheckForCycles(_parentId, ColorDataUnion.FromSelf(item.Item)))
        {
            using (Im.Disabled())
            {
                base.DrawItem(in item, globalIndex, selected);
            }

            Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled, "Selecting this option would cause a cyclic dependency."u8);
        }

        return base.DrawItem(in item, globalIndex, selected);
    }
}
