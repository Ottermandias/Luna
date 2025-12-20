namespace Luna;

/// <summary> An editor that reinterprets between two unmanaged types. </summary>
/// <typeparam name="TStored"> The provided type of the values. </typeparam>
/// <typeparam name="TEditable"> The type given to the editor. </typeparam>
/// <param name="inner"> The actual editor that obtains the transformed values. </param>
internal sealed class ReinterpretingEditor<TStored, TEditable>(IEditor<TEditable> inner) : IEditor<TStored>
    where TStored : unmanaged where TEditable : unmanaged
{
    /// <inheritdoc/>
    public bool Draw(Span<TStored> values, bool disabled)
        => inner.Draw(MemoryMarshal.Cast<TStored, TEditable>(values), disabled);

    /// <inheritdoc/>
    public IEditor<TStoredNew> Reinterpreting<TStoredNew>() where TStoredNew : unmanaged
        => inner.Reinterpreting<TStoredNew>();
}
