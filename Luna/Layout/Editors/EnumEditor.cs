namespace Luna;

/// <summary> An editor for enum-like values with a fixed set of named options. </summary>
/// <typeparam name="T"> The enum type. </typeparam>
/// <param name="domain"> The list of available options. </param>
public sealed class EnumEditor<T>(IReadOnlyList<(StringU8 Label, T Value, StringU8 Description)> domain) : IEditor<T>
    where T : unmanaged, IUtf8SpanFormattable, IEqualityOperators<T, T, bool>
{
    /// <inheritdoc/>
    public bool Draw(Span<T> values, bool disabled)
    {
        var helper = Editors.PrepareMultiComponent(values.Length);
        var ret    = false;

        for (var valueIdx = 0; valueIdx < values.Length; ++valueIdx)
        {
            using var id = Im.Id.Push(valueIdx);
            helper.SetupComponent(valueIdx);

            var currentValue = values[valueIdx];
            var labelLength  = 0;
            var valueFound   = false;
            foreach (var v in domain)
            {
                if (v.Value == currentValue)
                {
                    v.Label.Span.CopyInto<TextStringHandlerBuffer>();
                    labelLength = v.Label.Length;
                    valueFound  = true;
                }
            }
            if (!valueFound)
            {
                var writer = new SpanTextWriter(TextStringHandlerBuffer.Span);
                writer.Append(currentValue, default, CultureInfo.CurrentCulture);
                writer.EnsureNullTerminated();
                labelLength = writer.Position;
            }
            ret = disabled
                ? Im.Input.Text(""u8, TextStringHandlerBuffer.Span[..labelLength], out ulong _, flags: InputTextFlags.ReadOnly)
                : DrawCombo(TextStringHandlerBuffer.Span[..labelLength], ref values[valueIdx]);
        }

        return ret;
    }

    /// <summary> Draw the combo of possible values. </summary>
    private bool DrawCombo(ReadOnlySpan<byte> preview, ref T currentValue)
    {
        using var c = Im.Combo.Begin(""u8, preview);
        if (!c)
            return false;

        var ret = false;
        foreach (var (valueLabel, value, valueDescription) in domain)
        {
            if (Im.Selectable(valueLabel.Span, value == currentValue))
            {
                currentValue = value;
                ret          = true;
            }

            if (valueDescription.Length > 0)
                LunaStyle.DrawRightAlignedHelpMarker(valueDescription.Span);
        }

        return ret;
    }
}
