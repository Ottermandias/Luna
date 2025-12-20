namespace Luna;

/// <summary> An editor for all kinds of color types. </summary>
public sealed class ColorEditor : IEditor<float>, IEditor<Vector3>, IEditor<Vector4>
{
    /// <summary> A default editor for SDR colors. </summary>
    public static readonly ColorEditor StandardDynamicRange = new(false);

    /// <summary> A default editor for HDR colors. </summary>
    public static readonly ColorEditor HighDynamicRange     = new(true);

    /// <summary> Whether the editor is SDR or HDR. </summary>
    private readonly bool _hdr;

    /// <summary> Get the appropriate color editor for the given dynamic range. </summary>
    /// <param name="hdr"> Whether the editor is for HDR (true) or SDR (false). </param>
    /// <returns> The corresponding color editor. </returns>
    public static ColorEditor Get(bool hdr)
        => hdr ? HighDynamicRange : StandardDynamicRange;

    /// <inheritdoc/>
    public bool Draw(Span<float> values, bool disabled)
        => values.Length switch
        {
            3 => Draw(MemoryMarshal.Cast<float, Vector3>(values), disabled),
            4 => Draw(MemoryMarshal.Cast<float, Vector4>(values), disabled),
            _ => Editors.DefaultFloat.Draw(values, disabled),
        };

    /// <inheritdoc/>
    public bool Draw(Span<Vector3> values, bool disabled)
    {
        if (values.Length != 1)
            return Editors.DefaultFloat.Draw(MemoryMarshal.Cast<Vector3, float>(values), disabled);

        ref var value    = ref values[0];
        var     previous = value;
        if (!Im.Color.Editor("###color"u8, ref value, ColorEditorFlags.Float | (_hdr ? ColorEditorFlags.Hdr : 0)))
            return false;

        if (disabled)
        {
            value = previous;
            return false;
        }

        if (!_hdr)
            value = Vector3.Clamp(value, Vector3.Zero, Vector3.One);
        return true;
    }

    /// <inheritdoc/>
    public bool Draw(Span<Vector4> values, bool disabled)
    {
        if (values.Length != 1)
            return Editors.DefaultFloat.Draw(MemoryMarshal.Cast<Vector4, float>(values), disabled);

        ref var value    = ref values[0];
        var     previous = value;
        if (!Im.Color.Editor("###color"u8, ref value,
                ColorEditorFlags.Float | ColorEditorFlags.AlphaPreviewHalf | (_hdr ? ColorEditorFlags.Hdr : 0)))
            return false;

        if (disabled)
        {
            value = previous;
            return false;
        }

        if (!_hdr)
            value = Vector4.Clamp(value, Vector4.Zero, Vector4.One);
        return true;
    }

    /// <inheritdoc cref="IEditor{T}.Reinterpreting"/>
    public IEditor<TStored> Reinterpreting<TStored>() where TStored : unmanaged
    {
        if (typeof(TStored) == typeof(float) || typeof(TStored) == typeof(Vector3) || typeof(TStored) == typeof(Vector4))
            return (IEditor<TStored>)(object)this;

        return new ReinterpretingEditor<TStored, float>(this);
    }

    /// <summary> Create a new editor. </summary>
    private ColorEditor(bool hdr)
        => _hdr = hdr;
}
