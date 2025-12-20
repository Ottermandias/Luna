#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
namespace Luna;

/// <summary> Create a slider editor for arbitrary numeric types. </summary>
/// <typeparam name="T"> The numeric type. </typeparam>
/// <param name="minimum"> The minimum value for the slider. </param>
/// <param name="maximum"> The maximum value for the slider. </param>
/// <param name="format"> The display format for the slider. Formats can only contain 31 bytes of data. </param>
/// <param name="flags"> Additional flags to control the slider's behavior. </param>
public sealed class SliderEditor<T>(T minimum, T maximum, Editors.FormatBuffer format, SliderFlags flags) : IEditor<T>
    where T : unmanaged, INumber<T>
{
    /// <inheritdoc cref="SliderEditor{T}"/>
    /// <remarks> Specialized format from a default integer format together with the given unit. The unit should not contain more than 20 bytes of data.  </remarks>
    public static SliderEditor<T> CreateInteger(T minimum, T maximum, Utf8TextHandler unit, SliderFlags flags)
        => new(minimum, maximum, Editors.GenerateIntegerFormat<T>(false, ref unit), flags);

    /// <inheritdoc cref="SliderEditor{T}"/>
    /// <param name="precision"> The displayed precision of the floating point number. </param>
    /// <remarks> Specialized format from a default floating point format together with the given unit. The unit should not contain more than 20 bytes of data.  </remarks>
    public static SliderEditor<T> CreateFloat(T minimum, T maximum, byte precision, Utf8TextHandler unit, SliderFlags flags)
        => new(minimum, maximum, Editors.GenerateFloatFormat<T>(precision, ref unit), flags);

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
                Im.Slider(helper.Id, ref value, format, minimum, maximum, flags | SliderFlags.AlwaysClamp);
            }
            else
            {
                if (!Im.Slider(helper.Id, ref values[valueIdx], format, minimum, maximum, flags | SliderFlags.AlwaysClamp))
                    continue;

                if (values[valueIdx] < minimum)
                    values[valueIdx] = minimum;
                else if (values[valueIdx] > maximum)
                    values[valueIdx] = maximum;
                ret = true;
            }
        }

        return ret;
    }
}
