#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
namespace Luna;

/// <summary> Create a dragging editor for arbitrary numeric types. </summary>
/// <typeparam name="T"> The numeric type. </typeparam>
/// <param name="minimum"> An optional minimum value for the drag slider. </param>
/// <param name="maximum"> An optional maximum value for the drag slider. </param>
/// <param name="speed"> The speed for the drag slider. A pixel of dragged movement corresponds to a change of this value. </param>
/// <param name="relativeSpeed"> If this is positive, the dragging speed increases with higher values. </param>
/// <param name="format"> The display format for the drag slider. Formats can only contain 31 bytes of data. </param>
/// <param name="flags"> Additional flags to control the slider's behavior. </param>
public sealed class DragEditor<T>(T? minimum, T? maximum, float speed, float relativeSpeed, Editors.FormatBuffer format, SliderFlags flags)
    : IEditor<T>
    where T : unmanaged, INumber<T>
{
    /// <inheritdoc cref="DragEditor{T}"/>
    /// <remarks> Specialized format from a default integer format together with the given unit. The unit should not contain more than 20 bytes of data.  </remarks>
    public static DragEditor<T> CreateInteger(T? minimum, T? maximum, float speed, float relativeSpeed, Utf8TextHandler unit,
        SliderFlags flags)
        => new(minimum, maximum, speed, relativeSpeed, Editors.GenerateIntegerFormat<T>(false, ref unit), flags);

    /// <inheritdoc cref="DragEditor{T}"/>
    /// <param name="precision"> The displayed precision of the floating point number. </param>
    /// <remarks> Specialized format from a default floating point format together with the given unit. The unit should not contain more than 20 bytes of data.  </remarks>
    public static DragEditor<T> CreateFloat(T? minimum, T? maximum, float speed, float relativeSpeed, byte precision, Utf8TextHandler unit,
        SliderFlags flags)
        => new(minimum, maximum, speed, relativeSpeed, Editors.GenerateFloatFormat<T>(precision, ref unit), flags);

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
                Im.Drag(helper.Id, ref value, format, value, value, 0.0f, flags);
            }
            else
            {
                if (!Im.Drag(helper.Id, ref values[valueIdx], format, minimum, maximum, CalculateSpeed(values[valueIdx]), flags))
                    continue;

                if (minimum.HasValue && values[valueIdx] < minimum.Value)
                    values[valueIdx] = minimum.Value;
                else if (maximum.HasValue && values[valueIdx] > maximum.Value)
                    values[valueIdx] = maximum.Value;
                ret = true;
            }
        }

        return ret;
    }

    /// <summary> Calculate the drag speed from the general speed and the relative speed. </summary>
    private float CalculateSpeed(T value)
        => Math.Max(speed, Math.Abs(float.CreateSaturating(value)) * relativeSpeed);
}
