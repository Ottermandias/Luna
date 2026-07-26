using Dalamud.Interface.ImGuiNotification;
using FFXIVClientStructs.STD.Helper;
using ImSharp.Internal;

namespace Luna;

/// <summary> Draw a fully fledged settings panel for a custom color dictionary. </summary>
public static class ColorSettingsDrawer
{
    /// <summary> Draw the settings panel including buttons to import, export and reset. </summary>
    /// <typeparam name="TColorId"> The color ID type. </typeparam>
    /// <typeparam name="TColorData"> The associated color data. </typeparam>
    /// <param name="messages"> A messager for failures when importing from clipboard. </param>
    /// <param name="dict"> The color dictionary. </param>
    /// <param name="cache"> The cache with current colors. </param>
    /// <returns> True if the color dictionary was changed in this frame, in which case its own event will also be triggered. </returns>
    public static bool Draw<TColorId, TColorData>(MessageService messages, ColorDictionary<TColorId, TColorData> dict,
        ColorCache<TColorId, TColorData> cache)
        where TColorId : unmanaged, Enum
        where TColorData : IColorData<TColorId>
    {
        if (Im.Button("Copy to Clipboard"u8))
            Im.Clipboard.Set(dict.Sharable(true));

        Im.Line.Same();
        var ret = DrawImportButtons(messages, dict);
        Im.Line.Same();
        if (ImEx.Button("Reset All to Default"u8, default, "Reset all color values to their default colors."u8,
                !LunaStyle.Modifier.Destructive))
            ret |= dict.ResetToDefault();
        LunaStyle.Modifier.Destructive.TooltipLineBreak("reset"u8);
        LunaStyle.DrawSeparator();
        return ret | DrawSettings(dict, cache);
    }

    private static bool DrawSettings<TColorId, TColorData>(ColorDictionary<TColorId, TColorData> dict,
        ColorCache<TColorId, TColorData> cache)
        where TColorId : unmanaged, Enum
        where TColorData : IColorData<TColorId>
    {
        var drawCache = CacheManager.Instance.GetOrCreateCache(Im.Id.Current, () => new Cache<TColorId, TColorData>(dict));
        var ret       = false;
        foreach (var (index, (category, colors)) in drawCache.Sections.Index())
        {
            using var id   = Im.Id.Push(index);
            using var tree = Im.Tree.Node(category.IsEmpty ? "General"u8 : category, TreeNodeFlags.DefaultOpen);
            if (!tree)
                continue;

            using var clip = new Im.ListClipper(colors.Count, Im.Style.FrameHeightWithSpacing);
            foreach (var (colorId, colorData) in clip.Iterate(colors))
                ret |= ColorPicker(drawCache, colorId, colorData, dict, cache);
        }

        return ret;
    }

    private static unsafe bool ColorPicker<TColorId, TColorData>(Cache<TColorId, TColorData> drawCache, TColorId id,
        in ColorData<TColorId> data, ColorDictionary<TColorId, TColorData> dict, ColorCache<TColorId, TColorData> cache)
        where TColorId : unmanaged, Enum
        where TColorData : IColorData<TColorId>
    {
        var       ret   = false;
        using var _     = Im.Id.Push(*(int*)&id);
        using var group = Im.Group();

        // Draw the regular color picker with no label.
        var currentActualColor = cache[id, true];
        var setValue           = dict[id];
        if (Im.Color.Editor("##P"u8, ref currentActualColor, ColorEditorFlags.AlphaPreviewHalf | ColorEditorFlags.NoInputs))
        {
            dict[id] = new ColorDataUnion(currentActualColor);
            ret      = true;
        }

        Im.Line.SameInner();

        if (drawCache.Draw(id, setValue, out var newValue))
        {
            dict[id] = newValue;
            ret      = true;
        }

        // Draw a button to return to default.
        Im.Line.SameInner();
        if (ImEx.Button("Default"u8, Vector2.Zero, StringU8.Empty, setValue.IsDefault))
        {
            dict.Remove(id);
            ret = true;
        }

        if (Im.Item.Hovered(HoveredFlags.AllowWhenDisabled))
            DrawTooltip(cache, data);

        // Draw the actual label as well as a potential tooltip.
        Im.Line.SameInner();
        Im.Text(data.Label);
        Im.Tooltip.OnHover(data.Description);
        if (setValue.Type is not ColorDataUnion.TypeEnum.Const and not ColorDataUnion.TypeEnum.Default)
        {
            Im.Line.SameInner();
            Im.TextDisabled($"(Custom Reference to {setValue.ToStringU8<TColorId>(TColorData.Parent)})");
        }
        else if (setValue.Type is ColorDataUnion.TypeEnum.Default && data.Default.Type is not ColorDataUnion.TypeEnum.Const)
        {
            Im.Line.SameInner();
            Im.TextDisabled($"(Default Reference to {data.Default.ToStringU8<TColorId>(TColorData.Parent)})");
        }

        return ret;
    }

    private static void DrawTooltip<TColorId, TColorData>(ColorCache<TColorId, TColorData> cache, ColorData<TColorId> colorData)
        where TColorId : unmanaged, Enum
        where TColorData : IColorData<TColorId>
    {
        using var tt = Im.Tooltip.Begin();
        Vector4   current;
        switch (colorData.Default.Type)
        {
            case ColorDataUnion.TypeEnum.Const:
                current = colorData.Default.ConstantValue.ToVector();
                ImEx.TextFrameAligned($"Reset this color to {colorData.Default.ConstantValue}.");
                break;
            case ColorDataUnion.TypeEnum.Self:
                var parent      = TColorData.Data(colorData.Default.SelfValue<TColorId>());
                var parentValue = cache[colorData.Default.SelfValue<TColorId>()];
                current = cache[colorData.Default.SelfValue<TColorId>(), true];
                ImEx.TextFrameAligned(
                    $"Reset this color to a reference to {TColorData.Parent} color <{parent.Label}> (currently {parentValue}).");
                break;
            case ColorDataUnion.TypeEnum.ImGui:
                current = cache[colorData.Default.ImGuiValue, true];
                ImEx.TextFrameAligned(
                    $"Reset this color to a reference to ImGui color <{Im.Color.GetNameOwned(colorData.Default.ImGuiValue)}> (currently {cache[colorData.Default.ImGuiValue]}).");
                break;
            case ColorDataUnion.TypeEnum.Dalamud:
                current = cache[colorData.Default.DalamudValue, true];
                ImEx.TextFrameAligned(
                    $"Reset this color to a reference to Dalamud color <{colorData.Default.DalamudValue.StringU8}> (currently {cache[colorData.Default.DalamudValue]}).");
                break;
            default: throw new Exception("Unknown Color Type");
        }

        Im.Line.SameInner();
        Im.Color.Editor(StringU8.Empty, ref current, ColorEditorFlags.AlphaPreviewHalf | ColorEditorFlags.NoInputs);
    }

    private static bool DrawImportButtons<TColorId, TColorData>(MessageService messages, ColorDictionary<TColorId, TColorData> dict)
        where TColorId : unmanaged, Enum
        where TColorData : IColorData<TColorId>
    {
        var ignoreDefaults = ImEx.Button("Import From Clipboard (Ignore Defaults)"u8,
            default,
            "Try to import exported color values from your clipboard, but do not reset any values you have already set if the import contains their default values."u8,
            !LunaStyle.Modifier.Misclick);
        LunaStyle.Modifier.Misclick.TooltipLineBreak("import"u8);

        Im.Line.Same();
        var applyDefaults = ImEx.Button("Import From Clipboard (Write Defaults)"u8, default,
            "Try to import exported color values from your clipboard, overwriting everything."u8, !LunaStyle.Modifier.Misclick);
        LunaStyle.Modifier.Misclick.TooltipLineBreak("import"u8);

        if (!ignoreDefaults && !applyDefaults)
            return false;

        try
        {
            if (ColorDictionary<TColorId, TColorData>.FromSharable(Im.Clipboard.Get(), true) is { } parsedDict)
                return dict.Apply(parsedDict, applyDefaults);

            throw new Exception("Unable to parse color dictionary from clipboard.");
        }
        catch (Exception ex)
        {
            messages.NotificationMessage(ex, "Failed to import color dictionary", NotificationType.Error, false);
        }

        return false;
    }

    private sealed class Cache<TColorId, TColorData>(ColorDictionary<TColorId, TColorData> dictionary) : BasicCache
        where TColorId : unmanaged, Enum
        where TColorData : IColorData<TColorId>
    {
        private readonly ColorTypeCombo<TColorId, TColorData>   _type        = new(dictionary);
        private readonly ImGuiColorCombo                        _imGui       = new();
        private readonly ImNodesColorCombo                      _imNodes     = new();
        private readonly DalamudColorCombo                      _dalamud     = new();
        private readonly LunaColorCombo                         _luna        = new();
        private readonly CustomColorCombo<TColorId, TColorData> _customCombo = new(dictionary);

        public bool Draw(TColorId id, ColorDataUnion input, out ColorDataUnion output)
        {
            var totalWidth = 320 * Im.Style.GlobalScale;
            var (typeWidth, comboWidth) = input.Type is ColorDataUnion.TypeEnum.Default or ColorDataUnion.TypeEnum.Const
                ? (totalWidth, 0)
                : (100 * Im.Style.GlobalScale, totalWidth - 100 * Im.Style.GlobalScale - Im.Style.ItemInnerSpacing.X);
            var ret = _type.Draw(id, input.Type, out output, typeWidth);
            Im.Line.SameInner();
            switch (input.Type)
            {
                case ColorDataUnion.TypeEnum.Self:
                    if (_customCombo.Draw(id, input.SelfValue<TColorId>(), out var newColor, comboWidth))
                    {
                        output = ColorDataUnion.FromSelf(newColor);
                        ret    = true;
                    }

                    break;
                case ColorDataUnion.TypeEnum.ImGui:
                    if (_imGui.Draw("##imgui"u8, input.ImGuiValue, "Choose a reference to an ImGui color."u8, comboWidth, out var newImGui))
                    {
                        output = new ColorDataUnion(newImGui);
                        ret    = true;
                    }

                    break;
                case ColorDataUnion.TypeEnum.ImNodes:
                    if (_imNodes.Draw("##imNodes"u8, input.ImNodesValue, "Choose a reference to an ImNodes color."u8, comboWidth,
                            out var newImNodes))
                    {
                        output = new ColorDataUnion(newImNodes);
                        ret    = true;
                    }

                    break;
                case ColorDataUnion.TypeEnum.Dalamud:
                    if (_dalamud.Draw("##dalamud"u8, input.DalamudValue, "Choose a reference to a Dalamud color."u8, comboWidth,
                            out var newDalamud))
                    {
                        output = new ColorDataUnion(newDalamud);
                        ret    = true;
                    }

                    break;
                case ColorDataUnion.TypeEnum.Luna:
                    if (_luna.Draw("##luna"u8, input.LunaValue, "Choose a reference to a Luna color."u8, comboWidth, out var newLuna))
                    {
                        output = new ColorDataUnion(newLuna);
                        ret    = true;
                    }

                    break;
            }

            return ret;
        }

        public readonly IReadOnlyList<(StringU8 Section, IReadOnlyList<(TColorId Id, ColorData<TColorId> Data)>)> Sections
            = EnumExtensions.get_Values<TColorId>()
                .Select(id => (id, TColorData.Data(id)))
                .GroupBy(p => p.Item2.Section)
                .Select(g => (g.Key, (IReadOnlyList<(TColorId Id, ColorData<TColorId> Data)>)g.Select(d => (d.id, d.Item2)).ToArray()))
                .ToArray();

        public override void Update()
        { }
    }
}
