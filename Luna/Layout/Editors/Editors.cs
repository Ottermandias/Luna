namespace Luna;

/// <summary> Utility and extension methods related to <see cref="IEditor{T}"/>. </summary>
public static class Editors
{
    /// <summary> Provides a default editor suitable for <see cref="float"/> values. </summary>
    public static readonly IEditor<float> DefaultFloat = DragEditor<float>.CreateFloat(null, null, 0.1f, 0.0f, 3, ""u8, 0);

    /// <summary> Provides a default editor suitable for <see cref="int"/> values. </summary>
    public static readonly IEditor<int> DefaultInt = DragEditor<int>.CreateInteger(null, null, 0.1f, 0.0f, ""u8, 0);

    /// <summary> Adapts a <see cref="MultiStateCheckbox{T}"/> as an <see cref="IEditor{T}"/>. </summary>
    /// <typeparam name="T"> The type of the editable value. </typeparam>
    /// <param name="inner"> A <see cref="MultiStateCheckbox{T}"/>. </param>
    /// <returns> An <see cref="IEditor{T}"/> adapter for the given checkbox. </returns>
    public static IEditor<T> AsEditor<T>(this MultiStateCheckbox<T> inner) where T : unmanaged
        => new MultiStateCheckboxEditor<T>(inner);

    /// <summary> Prepare a helper that contains data for multi-component editors. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComponentHelper PrepareMultiComponent(int numComponents)
    {
        var spacing        = Im.Style.ItemInnerSpacing.X;
        var componentWidth = (Im.Item.CalculateWidth() - (numComponents - 1) * spacing) / numComponents;
        return new ComponentHelper(spacing, componentWidth);
    }

    /// <summary> Create a default integer format with a given unit. </summary>
    internal static FormatBuffer GenerateIntegerFormat<T>(bool hex, ref Utf8TextHandler unit) where T : unmanaged, INumber<T>
    {
        var buffer = new FormatBuffer();
        var writer = new SpanTextWriter(buffer);
        try
        {
            AppendIntegerFormat<T>(ref writer, hex);
            if (unit.GetSpan(out var span) && span.Length > 0)
            {
                writer.TryAppend((byte)' ');
                TryAppendPrintfLiteral(ref writer, span);
            }
        }
        finally
        {
            writer.EnsureNullTerminated();
        }

        return buffer;
    }

    /// <summary> Create a default floating point format with a given unit. </summary>
    internal static FormatBuffer GenerateFloatFormat<T>(byte precision, ref Utf8TextHandler unit) where T : unmanaged, INumber<T>
    {
        var buffer = new FormatBuffer();
        var writer = new SpanTextWriter(buffer);
        try
        {
            AppendFloatFormat<T>(ref writer, precision);
            if (unit.GetSpan(out var span) && span.Length > 0)
            {
                writer.TryAppend((byte)' ');
                TryAppendPrintfLiteral(ref writer, span);
            }
        }
        finally
        {
            writer.EnsureNullTerminated();
        }

        return buffer;
    }

    /// <remarks> Takes 3 to 7 bytes. </remarks>
    private static void AppendIntegerFormat<T>(ref SpanTextWriter writer, bool hex) where T : unmanaged, INumber<T>
        => writer.Append(DataTypeExtensions.From<T>() switch
        {
            DataType.U8  => hex ? "%02hhX"u8 : "%hhu"u8,
            DataType.S8  => hex ? "%02hhX"u8 : "%hhd"u8,
            DataType.U16 => hex ? "%04hX"u8 : "%hu"u8,
            DataType.S16 => hex ? "%04hX"u8 : "%hd"u8,
            DataType.U32 => hex ? "%08lX"u8 : "%lu"u8,
            DataType.S32 => hex ? "%08lX"u8 : "%ld"u8,
            DataType.U64 => hex ? "%016llX"u8 : "%llu"u8,
            DataType.S64 => hex ? "%016llX"u8 : "%lld"u8,
            _            => throw new NotSupportedException($"Unsupported integer type {typeof(T)}"),
        });

    /// <remarks> Takes 4 bytes. </remarks>
    private static void AppendFloatFormat<T>(ref SpanTextWriter writer, byte precision) where T : unmanaged, INumber<T>
    {
        if (DataTypeExtensions.From<T>() is not DataType.Float and not DataType.Double)
            throw new NotSupportedException($"Unsupported floating-point type {typeof(T)}");

        writer.Append([(byte)'%', (byte)'.', (byte)(48 + Math.Min(precision, (byte)9)), (byte)'f']);
    }

    /// <remarks> Takes <code>literal.Length</code> to <code>2 * literal.Length</code> bytes. </remarks>
    public static bool TryAppendPrintfLiteral(ref SpanTextWriter writer, scoped ReadOnlySpan<byte> literal)
    {
        for (;;)
        {
            var pos = MemoryExtensions.IndexOf(literal, (byte)'%');
            if (pos < 0)
                break;

            if (!writer.TryAppend(literal[..pos], out _))
                return false;
            if (!writer.TryAppend("%%"u8, out _))
                return false;

            literal = literal[(pos + 1)..];
        }

        return writer.TryAppend(literal, out _);
    }

    /// <summary> A buffer for the format string of an editor. </summary>
    [InlineArray(Size)]
    public struct FormatBuffer
    {
        // This should be plenty for most formats: for the formats defined above, that leaves 23 bytes for the unit.
        public const int Size = 32;

        private byte _element0;
    }

    /// <summary> A buffer for an identifier string of an editor. </summary>
    [InlineArray(Size)]
    public struct IdBuffer
    {
        // "###", widest integer (11), "\0", rounded up to power of 2
        public const int Size = 16;

        private byte _element0;
    }

    /// <summary> The helper data for multi component editors. </summary>
    public struct ComponentHelper
    {
        /// <summary> The item spacing between components, generally <see cref="Im.ImGuiStyle.ItemInnerSpacing"/>. </summary>
        public readonly float Spacing;

        /// <summary> The width of each component. </summary>
        public readonly float ComponentWidth;

        /// <summary> The ID for the group of components. </summary>
        public IdBuffer Id;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentHelper(float spacing, float componentWidth)
        {
            Spacing        = spacing;
            ComponentWidth = componentWidth;

            Id[0] = (byte)'#';
            Id[1] = (byte)'#';
            Id[2] = (byte)'#';
        }

        /// <summary> Set up a new component for the given index. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetupComponent(int index)
        {
            if (index > 0)
                Im.Line.Same(0.0f, Spacing);

            Im.Item.SetNextWidth(MathF.Round(ComponentWidth * (index + 1)) - MathF.Round(ComponentWidth * index));

            if (!index.TryFormat(Id[3..], out var bytesWritten))
                throw new Utf8FormatException();

            Id[3 + bytesWritten] = 0;
        }
    }
}
