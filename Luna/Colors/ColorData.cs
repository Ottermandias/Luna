namespace Luna;

/// <summary> Since enumerations can not implement interfaces, we use this workaround to implement data for the color. </summary>
/// <typeparam name="TColorId"> The type of the colors ID space. This is assumed to be contiguous. </typeparam>
public interface IColorData<TColorId> where TColorId : unmanaged, Enum
{
    /// <summary> Get the associated data for a color ID. </summary>
    /// <param name="id"> The color ID. </param>
    /// <returns> Label, Description and default value for the given color. </returns>
    /// <remarks> The implementer is responsible to not make self-references circular. </remarks>
    public abstract static ColorData<TColorId> Data(in TColorId id);

    /// <summary> Get the name of the color ID space. </summary>
    public abstract static StringU8 Parent { get; }
}

/// <summary> Descriptive data about a color to store. </summary>
/// <typeparam name="TColorId"> The type of the colors ID space. </typeparam>
/// <param name="Label"> The label to show next to the color when drawing the configurator. </param>
/// <param name="Description"> The more detailed description of the color shown as a tooltip when hovering. </param>
/// <param name="Default"> The default value for the color, which may be a constant color, a reference to an ImGui color or a reference to another custom color. </param>
/// <param name="Section"> An optional section for ordering the colors. </param>
public readonly record struct ColorData<TColorId>(StringU8 Label, StringU8 Description, ColorDataUnion Default, StringU8 Section)
    where TColorId : unmanaged, Enum
{
    /// <summary> An invalid color data. </summary>
    public static readonly ColorData<TColorId> Invalid = new(Rgba32.Transparent, "Unknown Color"u8, "This color is not known."u8);

    [OverloadResolutionPriority(100)]
    public ColorData(Rgba32 constantDefault, ReadOnlySpan<byte> label, ReadOnlySpan<byte> description, ReadOnlySpan<byte> section = default)
        : this(new StringU8(label), new StringU8(description), new ColorDataUnion(constantDefault), new StringU8(section))
    { }

    [OverloadResolutionPriority(10)]
    public ColorData(TColorId selfDefault, ReadOnlySpan<byte> label, ReadOnlySpan<byte> description, ReadOnlySpan<byte> section = default)
        : this(new StringU8(label), new StringU8(description), ColorDataUnion.FromSelf(selfDefault), new StringU8(section))
    { }

    [OverloadResolutionPriority(50)]
    public ColorData(ImGuiColor imGuiDefault, ReadOnlySpan<byte> label, ReadOnlySpan<byte> description, ReadOnlySpan<byte> section = default)
        : this(new StringU8(label), new StringU8(description), new ColorDataUnion(imGuiDefault), new StringU8(section))
    { }

    [OverloadResolutionPriority(0)]
    public ColorData(DalamudColor dalamudDefault, ReadOnlySpan<byte> label, ReadOnlySpan<byte> description,
        ReadOnlySpan<byte> section = default)
        : this(new StringU8(label), new StringU8(description), new ColorDataUnion(dalamudDefault), new StringU8(section))
    { }
}
