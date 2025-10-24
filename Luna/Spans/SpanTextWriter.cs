namespace Luna;

/// <summary> A text writer that writes into a span instead of a stream. </summary>
public unsafe ref struct SpanTextWriter
{
    private readonly ref byte _start;
    private ref          byte _pos;

    private SpanTextWriter(ref byte start, int length, bool nullTerminated)
    {
        _start         = ref start;
        _pos           = ref _start;
        Length         = length;
        Remaining      = Length;
        NullTerminated = nullTerminated;
    }

    /// <summary> Creates a new <see cref="SpanTextWriter"/> over the given span. </summary>
    /// <param name="span"> The span to write into. </param>
    /// <param name="nullTerminated"> Whether the resulting text is supposed to be null-terminated or not. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public SpanTextWriter(Span<byte> span, bool nullTerminated = true)
        : this(ref MemoryMarshal.GetReference(span), span.Length, nullTerminated)
    { }

    /// <summary> Get or set the current position in the span. </summary>
    /// <exception cref="ArgumentOutOfRangeException"> Thrown when setting the position out of bounds. </exception>
    public int Position
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        readonly get => (int)Unsafe.ByteOffset(ref _start, ref _pos);
        set
        {
            if (value < 0 || value > Length)
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    $"Position {value} is out of bounds for {nameof(SpanTextWriter)} of length {Length}");

            _pos      = ref Unsafe.Add(ref _start, value);
            Remaining = Length - value;
        }
    }

    /// <summary> The maximum length of the span. </summary>
    public readonly int Length;

    /// <summary> Whether the written text is intended to be null-terminated. </summary>
    public readonly bool NullTerminated;

    /// <summary> The total remaining number of bytes in the span. </summary>
    public int Remaining { get; private set; }

    /// <summary> The actual remaining number of bytes in the span, considering null-termination if applicable. </summary>
    public readonly int EffectiveRemaining
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        get => NullTerminated ? Math.Max(0, Remaining - 1) : Remaining;
    }

    /// <inheritdoc cref="Length"/>
    public readonly int Count
        => Length;

    /// <summary> Tests whether the buffer has enough room left for the given amount of bytes. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public readonly bool CanAppend(int size)
        => NullTerminated ? Remaining > size : Remaining >= size;

    /// <summary> Try to append a single byte to the buffer. </summary>
    /// <param name="value"> The byte to append. </param>
    /// <returns> Whether the byte could be appended. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryAppend(byte value)
    {
        if (!CanAppend(1))
            return false;

        _pos = value;
        DoAdvance(1);
        return true;
    }

    /// <summary> Append a single byte to the buffer. </summary>
    /// <param name="value"> The byte to append. </param>
    /// <exception cref="ImSharpSizeException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Append(byte value)
    {
        if (!TryAppend(value))
            throw new ImSharpSizeException();
    }

    /// <summary> Try to append a UTF8 span to the buffer. </summary>
    /// <param name="value"> The span to append. </param>
    /// <param name="bytesRead"> The number of bytes appended. </param>
    /// <param name="allowPartial"> Whether the method appends as many bytes as possible from the source span, or does not append anything if it does not fit entirely. </param>
    /// <returns> True when the entire span could be appended, false otherwise, even if a partial span was appended. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryAppend(scoped ReadOnlySpan<byte> value, out int bytesRead, bool allowPartial = false)
    {
        if (!CanAppend(value.Length))
        {
            if (allowPartial)
                TryAppend(FixUtf8Slicing(value[..EffectiveRemaining]), out bytesRead);
            else
                bytesRead = 0;
            return false;
        }

        var size = value.Length;
        var ptr  = Unsafe.AsPointer(ref _pos);
        DoAdvance(size);
        value.CopyTo(new Span<byte>(ptr, size));
        bytesRead = size;
        return true;
    }

    /// <summary> Append a UTF8 span to the buffer. </summary>
    /// <param name="value"> The span to append. </param>
    /// <exception cref="ImSharpSizeException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Append(scoped ReadOnlySpan<byte> value)
    {
        if (!TryAppend(value, out _))
            throw new ImSharpSizeException();
    }

    /// <summary> Try to append a UTF16 span to the buffer. </summary>
    /// <param name="value"> The span to append. </param>
    /// <param name="charsRead"> The number of UTF16 characters appended. </param>
    /// <param name="allowPartial"> Whether the method appends as many bytes as possible from the transcoded source span, or does not append anything if it does not fit entirely. </param>
    /// <returns> True when the entire span could be appended, false otherwise, even if a partial span was appended. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryAppend(scoped ReadOnlySpan<char> value, out int charsRead, bool allowPartial = false)
    {
        var status = Utf8.FromUtf16(value, GetRemainingSpan(), out charsRead, out var bytesWritten);
        if (allowPartial && status != OperationStatus.Done)
            DoAdvance(bytesWritten);
        return status == OperationStatus.Done;
    }

    /// <summary> Append a UTF16 span to the buffer. </summary>
    /// <param name="value"> The span to append. </param>
    /// <exception cref="ImSharpSizeException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Append(scoped ReadOnlySpan<char> value)
    {
        if (!TryAppend(value, out _))
            throw new ImSharpSizeException();
    }

    /// <summary> Try to append a formattable object to the buffer. </summary>
    /// <param name="formattable"> The formattable object to append. </param>
    /// <param name="format"> The format for the formattable object. </param>
    /// <param name="provider"> The format provider for the formattable object. </param>
    /// <returns> True if the object could be appended in the given format. </returns>
    /// <remarks> This is implemented as a generic to avoid boxing of formattable structs. </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryAppend<T>(T formattable, scoped ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        where T : IUtf8SpanFormattable
    {
        if (!formattable.TryFormat(GetRemainingSpan(), out var bytesWritten, format, provider))
            return false;

        DoAdvance(bytesWritten);
        return true;
    }

    /// <summary> Append a formattable object to the buffer. </summary>
    /// <param name="formattable"> The formattable object to append. </param>
    /// <param name="format"> The format for the formattable object. </param>
    /// <param name="provider"> The format provider for the formattable object. </param>
    /// <remarks> This is implemented as a generic to avoid boxing of formattable structs. </remarks>
    /// <exception cref="Utf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Append<T>(T formattable, scoped ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        where T : IUtf8SpanFormattable
    {
        if (!TryAppend(formattable, format, provider))
            throw new Utf8FormatException();
    }

    /// <inheritdoc cref="TryAppend(IFormatProvider?,ref AppendInterpolatedStringHandler)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryAppend([InterpolatedStringHandlerArgument("")] ref AppendInterpolatedStringHandler handler)
    {
        if (!handler.Finish(out var bytesWritten))
            return false;

        DoAdvance(bytesWritten);
        return true;
    }

    /// <inheritdoc cref="Append(IFormatProvider?,ref AppendInterpolatedStringHandler)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Append([InterpolatedStringHandlerArgument("")] ref AppendInterpolatedStringHandler handler)
    {
        if (!TryAppend(ref handler))
            throw new Utf8FormatException();
    }

    /// <summary> Try to append an interpolated string to the buffer. </summary>
    /// <param name="provider"> The format provider for the interpolated string handler </param>
    /// <param name="handler"> The interpolated string handler. </param>
    /// <returns> True if the handler could write its data to the buffer. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter",
        Justification = $"{nameof(provider)} is an interpolated string handler argument")]
    public bool TryAppend(IFormatProvider? provider,
        [InterpolatedStringHandlerArgument("", nameof(provider))]
        ref AppendInterpolatedStringHandler handler)
        => TryAppend(ref handler);

    /// <summary> Append an interpolated string to the buffer. </summary>
    /// <param name="provider"> The format provider for the interpolated string handler </param>
    /// <param name="handler"> The interpolated string handler. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter",
        Justification = $"{nameof(provider)} is an interpolated string handler argument")]
    public void Append(IFormatProvider? provider,
        [InterpolatedStringHandlerArgument("", nameof(provider))]
        ref AppendInterpolatedStringHandler handler)
    {
        if (!TryAppend(ref handler))
            throw new Utf8FormatException();
    }

    /// <summary> Gets a span over the part of the buffer that has already been written to. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public readonly ReadOnlySpan<byte> GetWrittenSpan()
        => new(Unsafe.AsPointer(ref _start), Position);

    /// <summary> Gets a span over the part of the buffer that has not yet been written to. Use in conjunction with <see cref="Advance(int)"/>. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Span<byte> GetRemainingSpan(bool withNullTerminator = false)
        => new(Unsafe.AsPointer(ref _pos), NullTerminated && !withNullTerminator ? Remaining - 1 : Remaining);

    /// <summary> Advances the position of this writer, considering the given number of bytes as written. Use in conjunction with <see cref="GetRemainingSpan(bool)"/>. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Advance(int size)
    {
        if (!CanAppend(size))
            throw new Utf8FormatException();

        DoAdvance(size);
    }

    /// <summary> Advance the position of this writer by the given size without checking. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void DoAdvance(int size)
    {
        _pos      =  ref Unsafe.Add(ref _pos, size);
        Remaining -= size;
    }

    /// <summary> Ensures the string being written is null-terminated, by either appending a null-terminator, or replacing the last character by a null-terminator if the buffer is full. </summary>
    public void EnsureNullTerminated()
    {
        if (Remaining >= 1)
        {
            _pos = 0;
            return;
        }

        if (Count > 0)
        {
            var contents = FixUtf8Slicing(GetWrittenSpan()[..^1]);
            Position = contents.Length;
            _pos     = 0;
        }
    }

    /// <summary> Fix splitting of UTF-8 characters at the end of a byte span. </summary>
    private static ReadOnlySpan<byte> FixUtf8Slicing(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 0)
            return bytes;

        // If the last byte is a single-byte UTF-8 character, nothing to fix.
        if (bytes[^1] < 0x80)
            return bytes;

        // Find the starting position of the last (multi-byte) UTF-8 character.
        var lastStart = bytes.LastIndexOfAnyInRange((byte)0xC0, (byte)0xFF);
        if (lastStart < 0)
            return [];

        var lastLength = bytes[lastStart] switch
        {
            >= 0xF0 => 4,
            >= 0xE0 => 3,
            _       => 2,
        };

        // If the last UTF-8 character is complete, nothing to fix.
        // (We assume the string is otherwise well-formed.)
        if (bytes.Length == lastStart + lastLength)
            return bytes;

        return bytes[..lastStart];
    }

    /// <summary> An interpolated string handler to append UTF8-encoded interpolated strings to a <see cref="SpanTextWriter"/>. </summary>
    /// <remarks> Uses the default functions for an interpolated string handler, so no documentation for those. </remarks>
    [InterpolatedStringHandler]
    public ref struct AppendInterpolatedStringHandler
    {
        private Utf8.TryWriteInterpolatedStringHandler _handler;

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public bool Finish(out int bytesWritten)
            => Utf8.TryWrite([], ref _handler, out bytesWritten);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AppendInterpolatedStringHandler(int literalLength, int formattedCount, ref SpanTextWriter writer, out bool shouldAppend)
            => _handler = new Utf8.TryWriteInterpolatedStringHandler(literalLength, formattedCount, writer.GetRemainingSpan(),
                out shouldAppend);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AppendInterpolatedStringHandler(int literalLength, int formattedCount, ref SpanTextWriter writer, IFormatProvider? provider,
            out bool shouldAppend)
            => _handler = new Utf8.TryWriteInterpolatedStringHandler(literalLength, formattedCount, writer.GetRemainingSpan(), provider,
                out shouldAppend);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AppendLiteral(string value)
            => _handler.AppendLiteral(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AppendFormatted<TValue>(TValue value)
            => _handler.AppendFormatted(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AppendFormatted<TValue>(TValue value, string? format)
            => _handler.AppendFormatted(value, format);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AppendFormatted<TValue>(TValue value, int alignment)
            => _handler.AppendFormatted(value, alignment);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AppendFormatted<TValue>(TValue value, int alignment, string? format)
            => _handler.AppendFormatted(value, alignment, format);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AppendFormatted(scoped ReadOnlySpan<char> value)
            => _handler.AppendFormatted(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null)
            => _handler.AppendFormatted(value, alignment, format);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AppendFormatted(scoped ReadOnlySpan<byte> utf8Value)
            => _handler.AppendFormatted(utf8Value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AppendFormatted(scoped ReadOnlySpan<byte> utf8Value, int alignment = 0, string? format = null)
            => _handler.AppendFormatted(utf8Value, alignment, format);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AppendFormatted(string? value)
            => _handler.AppendFormatted(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AppendFormatted(string? value, int alignment = 0, string? format = null)
            => _handler.AppendFormatted(value, alignment, format);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AppendFormatted(object? value, int alignment = 0, string? format = null)
            => _handler.AppendFormatted(value, alignment, format);
    }
}
