#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
namespace Luna;

/// <summary> An editor for numeric input fields with optional minimum or maximum and step sizes for button presses. </summary>
/// <typeparam name="T"> The type of the numeric values. </typeparam>
/// <param name="minimum"> The optional minimum value. </param>
/// <param name="maximum"> The optional maximum value. </param>
/// <param name="step"> The step when clicking the plus or minus button. If this is 0, no buttons are provided. </param>
/// <param name="stepFast"> The step when holding the plus or minus buttons for a while. </param>
/// <param name="format"> The format of the displayed number. </param>
/// <param name="flags"> Additional flags to control the editors behavior. </param>
public sealed class InputEditor<T>(T? minimum, T? maximum, T step, T stepFast, Editors.FormatBuffer format, InputTextFlags flags) : IEditor<T>
    where T : unmanaged, INumber<T>
{
    /// <inheritdoc cref="InputEditor{T}"/>
    /// <remarks> Specialized format from a default integer format together with the given unit. The unit should not contain more than 20 bytes of data.  </remarks>
    public static InputEditor<T> CreateInteger(T? minimum, T? maximum, T step, T stepFast, bool hex, Utf8TextHandler unit,
        InputTextFlags flags)
        => new(minimum, maximum, step, stepFast, Editors.GenerateIntegerFormat<T>(hex, ref unit),
            flags | (hex ? InputTextFlags.CharsHexadecimal : 0));

    /// <inheritdoc cref="InputEditor{T}"/>
    /// <param name="precision"> The displayed precision of the floating point number. </param>
    /// <remarks> Specialized format from a default integer format together with the given unit. The unit should not contain more than 20 bytes of data.  </remarks>
    public static InputEditor<T> CreateFloat(T? minimum, T? maximum, T step, T stepFast, byte precision, Utf8TextHandler unit,
        InputTextFlags flags)
        => new(minimum, maximum, step, stepFast, Editors.GenerateFloatFormat<T>(precision, ref unit), flags);

    /// <inheritdoc/>
    public bool Draw(Span<T> values, bool disabled)
    {
        var helper = Editors.PrepareMultiComponent(values.Length);
        var ret    = false;

        for (var valueIdx = 0; valueIdx < values.Length; ++valueIdx)
        {
            helper.SetupComponent(valueIdx);

            if (disabled)
            {
                var value = values[valueIdx];
                Im.Input.Scalar(helper.Id, ref value, format, step, stepFast, flags | InputTextFlags.ReadOnly);
            }
            else
            {
                if (Im.Input.Scalar(helper.Id, ref values[valueIdx], format, step, stepFast, flags))
                {
                    if (minimum.HasValue && values[valueIdx] < minimum.Value)
                        values[valueIdx] = minimum.Value;
                    else if (maximum.HasValue && values[valueIdx] > maximum.Value)
                        values[valueIdx] = maximum.Value;
                    ret = true;
                }
            }
        }

        return ret;
    }
}
