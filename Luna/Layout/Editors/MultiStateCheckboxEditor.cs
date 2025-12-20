namespace Luna;

/// <summary> An editor based on a multi state checkbox. </summary>
/// <typeparam name="T"> The type of values. </typeparam>
/// <param name="inner"> The base checkbox. </param>
internal sealed class MultiStateCheckboxEditor<T>(MultiStateCheckbox<T> inner) : IEditor<T> where T : unmanaged
{
    /// <inheritdoc/>
    public bool Draw(Span<T> values, bool disabled)
    {
        var helper = Editors.PrepareMultiComponent(values.Length);
        var ret    = false;

        for (var valueIdx = 0; valueIdx < values.Length; ++valueIdx)
        {
            helper.SetupComponent(valueIdx);

            if (disabled)
                inner.Draw(helper.Id, values[valueIdx], out _);
            else
            {
                if (inner.Draw(helper.Id, ref values[valueIdx]))
                    ret = true;
            }
        }

        return ret;
    }
}
