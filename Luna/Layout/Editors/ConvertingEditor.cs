namespace Luna;

/// <summary> Create the converting editor as described in <see cref="IEditor{T}.Converting"/>. </summary>
internal sealed class ConvertingEditor<TStored, TEditable>(
    IEditor<TEditable> inner,
    Func<TStored, TEditable> convert,
    Func<TEditable, TStored> convertBack) : IEditor<TStored>
    where TStored : unmanaged where TEditable : unmanaged
{
    public unsafe bool Draw(Span<TStored> values, bool disabled)
    {
        // Allocation strategy to prevent allocations for small sets of items.
        var converted = values.Length <= 2048 / sizeof(TEditable)
            ? stackalloc TEditable[values.Length]
            : new TEditable[values.Length];

        for (var i = 0; i < values.Length; ++i)
            converted[i] = convert(values[i]);

        if (!inner.Draw(converted, disabled))
            return false;

        for (var i = 0; i < values.Length; ++i)
            values[i] = convertBack(converted[i]);

        return true;
    }
}
