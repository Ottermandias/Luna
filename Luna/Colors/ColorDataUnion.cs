using ImSharp.ImNodes;

namespace Luna;

/// <summary> A tagged union for a default color data type. </summary>
/// <param name="Value"> The internal value. </param>
public readonly record struct ColorDataUnion(ulong Value)
{
    /// <summary> A defaulted color. </summary>
    public static readonly ColorDataUnion Default = new();

    /// <summary> The type of contained data. </summary>
    public enum TypeEnum : byte
    {
        /// <summary> The default value. </summary>
        Default,

        /// <summary> A constant color is provided as uint. </summary>
        Const,

        /// <summary> A reference to another color of the data type - the developer is responsible to avoid loops. </summary>
        Self,

        /// <summary> A reference to a default ImGui color. </summary>
        ImGui,

        /// <summary> A reference to a default ImNodes color. </summary>
        ImNodes,

        /// <summary> A reference to a Dalamud color. </summary>
        Dalamud,

        /// <summary> A reference to a Luna color. </summary>
        Luna,
    }

    /// <summary> Get whether this is a defaulted color. </summary>
    public bool IsDefault
        => Type is TypeEnum.Default;

    /// <summary> Create a ColorDataUnion from a constant color. </summary>
    /// <param name="value"> The constant color. </param>
    public ColorDataUnion(Rgba32 value)
        : this(value.Color | ((ulong)TypeEnum.Const << 32))
    { }

    /// <summary> Create a ColorDataUnion from an ImGui color reference. </summary>
    /// <param name="value"> The ImGui color. </param>
    public ColorDataUnion(ImGuiColor value)
        : this((uint)value | ((ulong)TypeEnum.ImGui << 32))
    { }

    /// <summary> Create a ColorDataUnion from an ImNodes color reference. </summary>
    /// <param name="value"> The ImNodes color. </param>
    public ColorDataUnion(ImNodesColor value)
        : this((uint)value | ((ulong)TypeEnum.ImNodes << 32))
    { }

    /// <summary> Create a ColorDataUnion from a Dalamud color reference. </summary>
    /// <param name="value"> The Dalamud color. </param>
    public ColorDataUnion(DalamudColor value)
        : this((uint)value | ((ulong)TypeEnum.Dalamud << 32))
    { }

    /// <summary> Create a ColorDataUnion from a Luna color reference. </summary>
    /// <param name="value"> The Dalamud color. </param>
    public ColorDataUnion(LunaColor value)
        : this((uint)value | ((ulong)TypeEnum.Luna << 32))
    { }

    /// <summary> Create a ColorDataUnion from an arbitrary reference to a color ID of size 4. </summary>
    /// <typeparam name="TColorId"> The type of the color ID. Must be exactly 4 bytes large. </typeparam>
    /// <param name="value"> The given color ID. </param>
    /// <returns> The created union. </returns>
    public static unsafe ColorDataUnion FromSelf<TColorId>(TColorId value)
        where TColorId : unmanaged, Enum
        => new(*(uint*)&value | ((ulong)TypeEnum.Self << 32));

    /// <summary> Get the type of this union. </summary>
    public TypeEnum Type
        => (TypeEnum)(Value >> 32);

    /// <summary> Get the value of this union as a constant color. </summary>
    public Rgba32 ConstantValue
        => (Rgba32)Value;

    /// <summary> Get the value of this union as a reference to an ImGui color. </summary>
    public ImGuiColor ImGuiValue
        => (ImGuiColor)Value;

    /// <summary> Get the value of this union as a reference to an ImNodes color. </summary>
    public ImNodesColor ImNodesValue
        => (ImNodesColor)Value;

    /// <summary> Get the value of this union as a reference to a Dalamud color. </summary>
    public DalamudColor DalamudValue
        => (DalamudColor)Value;

    /// <summary> Get the value of this union as a reference to a Luna color. </summary>
    public LunaColor LunaValue
        => (LunaColor)Value;

    /// <summary> Get this value as a string representation. </summary>
    public unsafe StringU8 ToStringU8<TColorId>(ReadOnlySpan<byte> parent)
        where TColorId : unmanaged, Enum
    {
        Span<byte> buffer = stackalloc byte[128];
        if (!Write<TColorId>(buffer, parent, out var length))
            throw new Exception("Could not create string for color name.");

        return new StringU8(buffer[..length], false);
    }

    /// <summary> Try to write this color or color reference as a string with no terminating \0. </summary>
    /// <typeparam name="TColorId"> The own type of color. </typeparam>
    /// <param name="buffer"> The buffer to write to. </param>
    /// <param name="parent"> The name of the own type of color. </param>
    /// <param name="bytesWritten"> How many bytes were written to the buffer. </param>
    /// <returns> True if the full color could be written to the buffer. </returns>
    public bool Write<TColorId>(Span<byte> buffer, ReadOnlySpan<byte> parent, out int bytesWritten)
        where TColorId : unmanaged, Enum
    {
        ReadOnlySpan<byte> name         = [];
        ReadOnlySpan<byte> actualParent = [];
        switch (Type)
        {
            case TypeEnum.Default:
                if ("null"u8.TryCopyTo(buffer))
                {
                    bytesWritten = 4;
                    return true;
                }

                break;
            case TypeEnum.Const: return ConstantValue.TryFormat(buffer, out bytesWritten, [], null);
            case TypeEnum.Self:
                name         = SelfValue<TColorId>().StringU8;
                actualParent = parent;
                break;
            case TypeEnum.ImGui:
                name         = ImGuiValue.StringU8;
                actualParent = "ImGui"u8;
                break;
            case TypeEnum.ImNodes:
                name         = ImNodesValue.StringU8;
                actualParent = "ImNodes"u8;
                break;
            case TypeEnum.Dalamud:
                name         = DalamudValue.StringU8;
                actualParent = "Dalamud"u8;
                break;
            case TypeEnum.Luna:
                name         = LunaValue.StringU8;
                actualParent = "Luna"u8;
                break;
            default:
                bytesWritten = 0;
                return false;
        }

        var length = actualParent.Length + 1 + name.Length;
        if (buffer.Length < length)
        {
            bytesWritten = 0;
            return false;
        }

        bytesWritten = length;
        actualParent.CopyTo(buffer);
        buffer[actualParent.Length] = (byte)'.';
        name.CopyTo(buffer[(actualParent.Length + 1)..]);
        return true;
    }

    /// <summary> Try to parse a color reference from the given text. </summary>
    /// <typeparam name="TColorId"> The own type of color. </typeparam>
    /// <param name="text"> The UTF8 text to parse. </param>
    /// <param name="parent"> The name of the own type of color. </param>
    /// <param name="value"> On success, the parsed value. </param>
    /// <returns> True if the text could be parsed to a color reference. </returns>
    /// <remarks> The string null is parsed to <see cref="ColorDataUnion.Default"/>, but generally the null token needs to be handled separately when using JSON. </remarks>
    public static bool TryParse<TColorId>(ReadOnlySpan<byte> text, ReadOnlySpan<byte> parent, out ColorDataUnion value)
        where TColorId : unmanaged, Enum
    {
        value = Default;
        if (text.Length < 4)
            return false;

        if (Rgba32.TryRead(text, out var color))
        {
            value = new ColorDataUnion(color);
            return true;
        }

        if (text.SequenceEqual("null"u8))
            return true;

        if (CheckParent(text, parent))
        {
            if (!EnumExtensions.Parse(text[(parent.Length + 1)..], out TColorId id))
                return false;

            value = FromSelf(id);
            return true;
        }

        if (CheckParent(text, "ImGui"u8))
        {
            if (!ImGuiColor.Parse(text[("ImGui"u8.Length + 1)..], out var imgui))
                return false;

            value = new ColorDataUnion(imgui);
            return true;
        }

        if (CheckParent(text, "ImNodes"u8))
        {
            if (!ImNodesColor.Parse(text[("ImNodes"u8.Length + 1)..], out var imNodes))
                return false;

            value = new ColorDataUnion(imNodes);
            return true;
        }

        if (CheckParent(text, "Dalamud"u8))
        {
            if (!DalamudColor.Parse(text[("Dalamud"u8.Length + 1)..], out var dalamud))
                return false;

            value = new ColorDataUnion(dalamud);
            return true;
        }

        if (CheckParent(text, "Luna"u8))
        {
            if (!LunaColor.Parse(text[("Luna"u8.Length + 1)..], out var luna))
                return false;

            value = new ColorDataUnion(luna);
            return true;
        }

        return false;

        static bool CheckParent(ReadOnlySpan<byte> text, ReadOnlySpan<byte> parent)
        {
            return text.Length > parent.Length + 1 && text[parent.Length] is (byte)'.' && text.StartsWith(parent);
        }
    }

    /// <summary> Get the value of this union as a reference to a custom color ID. </summary>
    public unsafe TColorId SelfValue<TColorId>() where TColorId : unmanaged, Enum
    {
        if (sizeof(TColorId) is not 4)
            throw new NotSupportedException($"{typeof(TColorId).Name} must be a 4-byte type for {nameof(ColorDataUnion)}.");

        var value = (uint)Value;
        return *(TColorId*)&value;
    }
}
