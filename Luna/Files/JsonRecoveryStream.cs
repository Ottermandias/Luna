namespace Luna;

/// <summary> Detects and recovers from various kinds of invalid JSON. </summary>
/// <seealso href="https://ecma-international.org/publications-and-standards/standards/ecma-404/"/>
public sealed class JsonRecoveryStream : OutputFilterStream
{
    private const byte LeftSquareBracket  = (byte)'[';
    private const byte LeftCurlyBracket   = (byte)'{';
    private const byte RightSquareBracket = (byte)']';
    private const byte RightCurlyBracket  = (byte)'}';
    private const byte Colon              = (byte)':';
    private const byte Comma              = (byte)',';
    private const byte QuotationMark      = (byte)'"';
    private const byte ReverseSolidus     = (byte)'\\'; // Blame ECMA for the name.

    private static readonly SearchValues<byte> Whitespace  = SearchValues.Create("\t\n\r "u8);
    private static readonly SearchValues<byte> Decimal     = SearchValues.Create("0123456789"u8);
    private static readonly SearchValues<byte> Hexadecimal = SearchValues.Create("0123456789ABCDEFabcdef"u8);

    private static readonly SearchValues<byte> BadStringCharacters =
        SearchValues.Create(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0xA, 0xB, 0xC, 0xD, 0xE, 0xF, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
            0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, QuotationMark, ReverseSolidus);

    private static ReadOnlySpan<byte> HexadecimalUppercase
        => "0123456789ABCDEF"u8;

    private readonly JsonRecoveryFlags _allowedRecoveries;
    private readonly Stack<byte>       _blocks;
    private readonly List<byte>        _buffer;

    private JsonRecoveryFlags     _usedRecoveries;
    private State                 _state;
    private bool                  _isKey;
    private InlineStringU8<ulong> _escapeBuffer;

    /// <summary> The cases that this stream is allowed to recover from. </summary>
    public JsonRecoveryFlags AllowedRecoveries
        => _allowedRecoveries;

    /// <summary> The cases that this stream has recovered from so far. This property is safe to read after the stream is closed. </summary>
    public JsonRecoveryFlags UsedRecoveries
        => _usedRecoveries;

    /// <summary> Constructs a JSON recovery stream, wrapped around the given stream. </summary>
    /// <param name="allowedRecoveries"> The cases that this stream is allowed to recover from. </param>
    /// <param name="outputStream"> The stream where the corrected JSON data will be written to. </param>
    /// <param name="leaveOpen"> Whether to leave the output stream open when closing this filter. </param>
    public JsonRecoveryStream(JsonRecoveryFlags allowedRecoveries, Stream outputStream, bool leaveOpen = false)
        : base(outputStream, leaveOpen)
    {
        _allowedRecoveries = allowedRecoveries;
        _blocks            = [];
        _buffer            = [];
    }

    private bool AllowsRecovery(JsonRecoveryFlags recovery)
        => (_allowedRecoveries & recovery) == recovery;

    private void UseRecovery(JsonRecoveryFlags recovery)
    {
        if (!AllowsRecovery(recovery))
            throw new InvalidDataException();

        _usedRecoveries |= recovery;
    }

    private void UseRecovery(params ReadOnlySpan<JsonRecoveryFlags> recoveries)
    {
        foreach (var flag in recoveries)
        {
            if (AllowsRecovery(flag))
            {
                _usedRecoveries |= flag;
                return;
            }
        }

        throw new InvalidDataException();
    }

    protected override void Dispose(bool disposing)
    {
        ProcessEndOfStream();
        base.Dispose(disposing);
    }

    private void ProcessEndOfStream()
    {
        if (_blocks.Count is 0)
        {
            if (_state is State.ValueEnd or State.NumberIntegral or State.NumberFractional or State.NumberExponent)
                return;

            if (_state is State.NumberIntegralZero)
            {
                OutputStream.WriteByte((byte)'0');
                return;
            }
        }

        UseRecovery(JsonRecoveryFlags.PrematureEndOfStream);
        switch (_state)
        {
            case State.ValueStart: OutputStream.Write("null"u8); break;
            case State.ValueEnd:
            case State.KeyStart:
                break;
            case State.KeyEnd: OutputStream.Write(":null"u8); break;
            case State.AfterLeftSquareBracket:
            case State.AfterComma:
                break;

            case State.String:
            case State.StringEscape:
                OutputStream.WriteByte(QuotationMark);
                if (_isKey)
                    OutputStream.Write(":null"u8);
                break;
            case State.StringOctal1 or State.StringOctal2:
                WriteBufferedOctalCharacter();
                goto case State.String;
            case State.StringHexadecimal0 or State.StringHexadecimal1 or State.StringUnicode0 or State.StringUnicode1
                or State.StringUnicode2 or State.StringUnicode3 or State.StringRune0 or State.StringRune1 or State.StringRune2
                or State.StringRune3 or State.StringRune4 or State.StringRune5 or State.StringRune6 or State.StringRune7:
                WriteBufferedHexadecimalCharacter();
                goto case State.String;

            case State.NumberIntegralStart or State.NumberIntegralZero or State.NumberFractionalStart or State.NumberExponentSign
                or State.NumberExponentStart:
                OutputStream.WriteByte((byte)'0');
                break;
            case State.NumberIntegral or State.NumberFractional or State.NumberExponent: break;

            case State.True1: OutputStream.Write("rue"u8); break;
            case State.True2: OutputStream.Write("ue"u8); break;
            case State.True3: OutputStream.WriteByte((byte)'e'); break;

            case State.False1: OutputStream.Write("alse"u8); break;
            case State.False2: OutputStream.Write("lse"u8); break;
            case State.False3: OutputStream.Write("se"u8); break;
            case State.False4: OutputStream.WriteByte((byte)'e'); break;

            case State.Null1: OutputStream.Write("ull"u8); break;
            case State.Null2: OutputStream.Write("ll"u8); break;
            case State.Null3: OutputStream.WriteByte((byte)'l'); break;

            default: throw new UnreachableException();
        }

        while (_blocks.TryPop(out var b))
            OutputStream.WriteByte(b);
    }

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
    {
        while (count > 0)
        {
            var consumed = Process(buffer.AsSpan(offset, count));
            offset += consumed;
            count  -= consumed;
        }
    }

    /// <inheritdoc/>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        while (buffer.Length > 0)
            buffer = buffer[Process(buffer)..];
    }

    /// <inheritdoc/>
    public override void WriteByte(byte value)
    {
        var buffer = new ReadOnlySpan<byte>(in value);
        for (;;)
        {
            if (Process(buffer) > 0)
                return;
        }
    }

    private int Process(ReadOnlySpan<byte> buffer)
    {
        var (nextState, consumed) = _state switch
        {
            State.ValueStart => ProcessAtValueStart(buffer),
            State.ValueEnd   => ProcessAtValueEnd(buffer),

            State.KeyStart => ProcessAtKeyStart(buffer),
            State.KeyEnd   => ProcessAtKeyEnd(buffer),

            State.AfterLeftSquareBracket => ProcessAfterLeftSquareBracket(buffer),
            State.AfterComma             => ProcessAfterComma(buffer),

            State.String             => ProcessInString(buffer),
            State.StringEscape       => ProcessInStringEscape(buffer[0]),
            State.StringOctal1       => ProcessInStringOctalEscape(buffer[0], 1, State.StringOctal2),
            State.StringOctal2       => ProcessInStringOctalEscape(buffer[0], 2, State.String),
            State.StringHexadecimal0 => ProcessInStringHexadecimalEscape(buffer[0], 0, State.StringHexadecimal1),
            State.StringHexadecimal1 => ProcessInStringHexadecimalEscape(buffer[0], 1, State.String),
            State.StringUnicode0     => ProcessInStringHexadecimalEscape(buffer[0], 0, State.StringUnicode1),
            State.StringUnicode1     => ProcessInStringHexadecimalEscape(buffer[0], 1, State.StringUnicode2),
            State.StringUnicode2     => ProcessInStringHexadecimalEscape(buffer[0], 2, State.StringUnicode3),
            State.StringUnicode3     => ProcessInStringHexadecimalEscape(buffer[0], 3, State.String),
            State.StringRune0        => ProcessInStringHexadecimalEscape(buffer[0], 0, State.StringRune1),
            State.StringRune1        => ProcessInStringHexadecimalEscape(buffer[0], 1, State.StringRune2),
            State.StringRune2        => ProcessInStringHexadecimalEscape(buffer[0], 2, State.StringRune3),
            State.StringRune3        => ProcessInStringHexadecimalEscape(buffer[0], 3, State.StringRune4),
            State.StringRune4        => ProcessInStringHexadecimalEscape(buffer[0], 4, State.StringRune5),
            State.StringRune5        => ProcessInStringHexadecimalEscape(buffer[0], 5, State.StringRune6),
            State.StringRune6        => ProcessInStringHexadecimalEscape(buffer[0], 6, State.StringRune7),
            State.StringRune7        => ProcessInStringHexadecimalEscape(buffer[0], 7, State.String),

            State.NumberIntegralStart   => ProcessInNumberIntegralStart(buffer),
            State.NumberIntegralZero    => ProcessInNumberIntegralZero(buffer),
            State.NumberIntegral        => ProcessInNumberIntegral(buffer),
            State.NumberFractionalStart => ProcessInNumberFractionalStart(buffer),
            State.NumberFractional      => ProcessInNumberFractional(buffer),
            State.NumberExponentSign    => ProcessInNumberExponentSign(buffer),
            State.NumberExponentStart   => ProcessInNumberExponentStart(buffer),
            State.NumberExponent        => ProcessInNumberExponent(buffer),

            State.True1 => ProcessInKeyword(buffer[0], (byte)'r', (byte)'R', State.True2),
            State.True2 => ProcessInKeyword(buffer[0], (byte)'u', (byte)'U', State.True3),
            State.True3 => ProcessInKeyword(buffer[0], (byte)'e', (byte)'E', State.ValueEnd),

            State.False1 => ProcessInKeyword(buffer[0], (byte)'a', (byte)'A', State.False2),
            State.False2 => ProcessInKeyword(buffer[0], (byte)'l', (byte)'L', State.False3),
            State.False3 => ProcessInKeyword(buffer[0], (byte)'s', (byte)'S', State.False4),
            State.False4 => ProcessInKeyword(buffer[0], (byte)'e', (byte)'E', State.ValueEnd),

            State.Null1 => ProcessInKeyword(buffer[0], (byte)'u', (byte)'U', State.Null2),
            State.Null2 => ProcessInKeyword(buffer[0], (byte)'l', (byte)'L', State.Null3),
            State.Null3 => ProcessInKeyword(buffer[0], (byte)'l', (byte)'L', State.ValueEnd),

            _ => throw new UnreachableException(),
        };
        if (consumed is 0 && nextState == _state)
            throw new UnreachableException();

        _state = nextState;
        return consumed;
    }

    private (State NextState, int Consumed) ProcessAtValueStart(ReadOnlySpan<byte> buffer)
    {
        switch (buffer[0])
        {
            case RightCurlyBracket:
                UseRecovery(JsonRecoveryFlags.MissingValues);
                OutputStream.Write("null"u8);
                CloseBlock(RightCurlyBracket);
                return (State.ValueEnd, 1);
            case RightSquareBracket:
                UseRecovery(JsonRecoveryFlags.MissingValues);
                OutputStream.Write("null"u8);
                CloseBlock(RightSquareBracket);
                return (State.ValueEnd, 1);
            case Comma:
                UseRecovery(JsonRecoveryFlags.MissingValues);
                OutputStream.Write("null"u8);
                return (State.AfterComma, 1);

            case LeftCurlyBracket:
                OutputStream.WriteByte(LeftCurlyBracket);
                _blocks.Push(RightCurlyBracket);
                return (State.KeyStart, 1);
            case LeftSquareBracket:
                OutputStream.WriteByte(LeftSquareBracket);
                _blocks.Push(RightSquareBracket);
                return (State.AfterLeftSquareBracket, 1);

            case (byte)'+':
                UseRecovery(JsonRecoveryFlags.NumberExplicitPositive);
                return (State.NumberIntegralStart, 0);
            case (byte)'-':
                OutputStream.WriteByte((byte)'-');
                return (State.NumberIntegralStart, 1);
            case (byte)'0':                     return (State.NumberIntegralZero, 1);
            case >= (byte)'1' and <= (byte)'9': return (State.NumberIntegral, PassThroughWhile(buffer, Decimal).Consumed);
            case (byte)'.':
                UseRecovery(JsonRecoveryFlags.NumberMissingDigits);
                OutputStream.Write("0."u8);
                return (State.NumberFractionalStart, 1);

            case QuotationMark:
                OutputStream.WriteByte(QuotationMark);
                _isKey = false;
                return (State.String, 1);

            case (byte)'T':
                UseRecovery(JsonRecoveryFlags.KeywordCase);
                OutputStream.WriteByte((byte)'t');
                return (State.True1, 1);
            case (byte)'t':
                // Opportunistically consume the whole token when possible.
                if (buffer.StartsWith("true"u8))
                {
                    OutputStream.Write("true"u8);
                    return (State.ValueEnd, 4);
                }

                OutputStream.WriteByte((byte)'t');
                return (State.True1, 1);

            case (byte)'F':
                UseRecovery(JsonRecoveryFlags.KeywordCase);
                OutputStream.WriteByte((byte)'f');
                return (State.False1, 1);
            case (byte)'f':
                // Opportunistically consume the whole token when possible.
                if (buffer.StartsWith("false"u8))
                {
                    OutputStream.Write("false"u8);
                    return (State.ValueEnd, 5);
                }

                OutputStream.WriteByte((byte)'f');
                return (State.False1, 1);

            case (byte)'N':
                UseRecovery(JsonRecoveryFlags.KeywordCase);
                OutputStream.WriteByte((byte)'n');
                return (State.Null1, 1);
            case (byte)'n':
                // Opportunistically consume the whole token when possible.
                if (buffer.StartsWith("null"u8))
                {
                    OutputStream.Write("null"u8);
                    return (State.ValueEnd, 4);
                }

                OutputStream.WriteByte((byte)'n');
                return (State.Null1, 1);

            default:
                if (Whitespace.Contains(buffer[0]))
                    return PassThroughWhile(buffer, Whitespace);

                throw new InvalidDataException();
        }
    }

    private (State NextState, int Consumed) ProcessAtValueEnd(ReadOnlySpan<byte> buffer)
    {
        switch (buffer[0])
        {
            case Comma: return (State.AfterComma, 1);
            case RightSquareBracket or RightCurlyBracket:
                CloseBlock(buffer[0]);
                return (State.ValueEnd, 1);
            case LeftCurlyBracket or LeftSquareBracket or (byte)'+' or (byte)'-' or >= (byte)'0' and <= (byte)'9' or (byte)'.' or QuotationMark
                or (byte)'t' or (byte)'T' or (byte)'f' or (byte)'F' or (byte)'n' or (byte)'N':
                UseRecovery(JsonRecoveryFlags.MissingPunctuation);
                return (State.AfterComma, 0);
            default:
                if (Whitespace.Contains(buffer[0]))
                    return PassThroughWhile(buffer, Whitespace);

                throw new InvalidDataException();
        }
    }

    private (State NextState, int Consumed) ProcessAtKeyStart(ReadOnlySpan<byte> buffer)
    {
        switch (buffer[0])
        {
            case QuotationMark:
                OutputStream.WriteByte(QuotationMark);
                _isKey = true;
                return (State.String, 1);
            case RightCurlyBracket:
                CloseBlock(RightCurlyBracket);
                return (State.ValueEnd, 1);
            default:
                if (Whitespace.Contains(buffer[0]))
                    return PassThroughWhile(buffer, Whitespace);

                throw new InvalidDataException();
        }
    }

    private (State NextState, int Consumed) ProcessAtKeyEnd(ReadOnlySpan<byte> buffer)
    {
        switch (buffer[0])
        {
            case Colon:
                OutputStream.WriteByte(Colon);
                return (State.ValueStart, 1);
            case Comma:
                UseRecovery(JsonRecoveryFlags.MissingValues);
                OutputStream.Write(":null"u8);
                return (State.AfterComma, 1);
            case RightCurlyBracket:
                UseRecovery(JsonRecoveryFlags.MissingValues);
                OutputStream.Write(":null"u8);
                CloseBlock(RightCurlyBracket);
                return (State.ValueEnd, 1);
            case LeftCurlyBracket or LeftSquareBracket or (byte)'+' or (byte)'-' or >= (byte)'0' and <= (byte)'9' or (byte)'.' or QuotationMark
                or (byte)'t' or (byte)'T' or (byte)'f' or (byte)'F' or (byte)'n' or (byte)'N':
                UseRecovery(JsonRecoveryFlags.MissingPunctuation);
                OutputStream.WriteByte(Colon);
                return (State.ValueStart, 0);
            default:
                if (Whitespace.Contains(buffer[0]))
                    return PassThroughWhile(buffer, Whitespace);

                throw new InvalidDataException();
        }
    }

    private (State NextState, int Consumed) ProcessAfterLeftSquareBracket(ReadOnlySpan<byte> buffer)
    {
        if (buffer[0] is RightSquareBracket)
        {
            CloseBlock(RightSquareBracket);
            return (State.ValueEnd, 1);
        }

        if (Whitespace.Contains(buffer[0]))
            return PassThroughWhile(buffer, Whitespace);

        return (State.ValueStart, 0);
    }

    private (State NextState, int Consumed) ProcessAfterComma(ReadOnlySpan<byte> buffer)
    {
        switch (buffer[0])
        {
            case RightSquareBracket or RightCurlyBracket:
                UseRecovery(JsonRecoveryFlags.TrailingCommas);
                FlushBuffer();
                CloseBlock(buffer[0]);
                return (State.ValueEnd, 1);
            default:
                if (Whitespace.Contains(buffer[0]))
                    return BufferWhile(buffer, Whitespace);

                if (!_blocks.TryPeek(out var b))
                    throw new InvalidDataException();

                OutputStream.WriteByte(Comma);
                FlushBuffer();
                return (b is RightCurlyBracket ? State.KeyStart : State.ValueStart, 0);
        }
    }

    private (State NextState, int Consumed) ProcessInString(ReadOnlySpan<byte> buffer)
    {
        var value = buffer[0];
        if (value is QuotationMark)
        {
            OutputStream.WriteByte(QuotationMark);
            return (_isKey ? State.KeyEnd : State.ValueEnd, 1);
        }

        if (value is ReverseSolidus)
            return (State.StringEscape, 1);

        if (!BadStringCharacters.Contains(value))
            return PassThroughUntil(buffer, BadStringCharacters);

        UseRecovery(JsonRecoveryFlags.StringRawCharacters);
        WriteStringCharacter(value);
        return (State.String, 1);
    }

    private (State NextState, int Consumed) ProcessInStringEscape(byte value)
    {
        switch (value)
        {
            case QuotationMark:
            case ReverseSolidus:
            case (byte)'/':
            case (byte)'b':
            case (byte)'f':
            case (byte)'n':
            case (byte)'r':
            case (byte)'t':
                OutputStream.WriteByte(ReverseSolidus);
                OutputStream.WriteByte(value);
                return (State.String, 1);
            case (byte)'B':
                UseRecovery(JsonRecoveryFlags.StringEscapeCase);
                OutputStream.Write("\\b"u8);
                return (State.String, 1);
            case (byte)'F':
                UseRecovery(JsonRecoveryFlags.StringEscapeCase);
                OutputStream.Write("\\f"u8);
                return (State.String, 1);
            case (byte)'N':
                UseRecovery(JsonRecoveryFlags.StringEscapeCase);
                OutputStream.Write("\\n"u8);
                return (State.String, 1);
            case (byte)'R':
                UseRecovery(JsonRecoveryFlags.StringEscapeCase);
                OutputStream.Write("\\r"u8);
                return (State.String, 1);
            case (byte)'T':
                UseRecovery(JsonRecoveryFlags.StringEscapeCase);
                OutputStream.Write("\\t"u8);
                return (State.String, 1);
            case (byte)'a':
                UseRecovery(JsonRecoveryFlags.StringExtendedEscapes);
                OutputStream.Write("\\u0007"u8);
                return (State.String, 1);
            case (byte)'e':
                UseRecovery(JsonRecoveryFlags.StringExtendedEscapes);
                OutputStream.Write("\\u001B"u8);
                return (State.String, 1);
            case (byte)'v':
                UseRecovery(JsonRecoveryFlags.StringExtendedEscapes);
                OutputStream.Write("\\u000B"u8);
                return (State.String, 1);
            case (byte)'A':
                UseRecovery(JsonRecoveryFlags.StringExtendedEscapes | JsonRecoveryFlags.StringEscapeCase);
                OutputStream.Write("\\u0007"u8);
                return (State.String, 1);
            case (byte)'E':
                UseRecovery(JsonRecoveryFlags.StringExtendedEscapes | JsonRecoveryFlags.StringEscapeCase);
                OutputStream.Write("\\u001B"u8);
                return (State.String, 1);
            case (byte)'V':
                UseRecovery(JsonRecoveryFlags.StringExtendedEscapes | JsonRecoveryFlags.StringEscapeCase);
                OutputStream.Write("\\u000B"u8);
                return (State.String, 1);
            case (byte)'\'' or (byte)'?':
                UseRecovery(JsonRecoveryFlags.StringExtendedEscapes, JsonRecoveryFlags.StringInvalidEscapes);
                OutputStream.WriteByte(value);
                return (State.String, 1);
            case >= (byte)'0' and <= (byte)'7':
                UseRecovery(JsonRecoveryFlags.StringExtendedEscapes);
                _escapeBuffer = new InlineStringU8<ulong>(value);
                return (State.StringOctal1, 1);
            case (byte)'x':
                UseRecovery(JsonRecoveryFlags.StringExtendedEscapes);
                _escapeBuffer = default;
                return (State.StringHexadecimal0, 1);
            case (byte)'X':
                UseRecovery(JsonRecoveryFlags.StringExtendedEscapes | JsonRecoveryFlags.StringEscapeCase);
                _escapeBuffer = default;
                return (State.StringHexadecimal0, 1);
            case (byte)'u':
                _escapeBuffer = default;
                return (State.StringUnicode0, 1);
            case (byte)'U':
                UseRecovery(JsonRecoveryFlags.StringExtendedEscapes, JsonRecoveryFlags.StringEscapeCase);
                _escapeBuffer = default;
                return (AllowsRecovery(JsonRecoveryFlags.StringExtendedEscapes) ? State.StringRune0 : State.StringUnicode0, 1);
            default:
                UseRecovery(JsonRecoveryFlags.StringInvalidEscapes);
                OutputStream.WriteByte(value);
                return (State.String, 1);
        }
    }

    private (State NextState, int Consumed) ProcessInStringOctalEscape(byte value, int position, State nextState)
    {
        if (value is < (byte)'0' or > (byte)'7')
        {
            // \0 is a complete escape in its own right, but can also be the beginning of an octal escape.
            if (position is not 1 || !_escapeBuffer.Equals("0"u8))
                UseRecovery(JsonRecoveryFlags.StringIncompleteEscapes);

            WriteBufferedOctalCharacter();
            return (State.String, 0);
        }

        _escapeBuffer[position] = value;
        if (nextState is State.String)
            WriteBufferedOctalCharacter();

        return (nextState, 1);
    }

    private void WriteBufferedOctalCharacter()
    {
        var ch = 0u;
        foreach (var b in _escapeBuffer)
            ch = (ch << 3) + unchecked((uint)b - '0');

        WriteStringCharacter(ch);
    }

    private (State NextState, int Consumed) ProcessInStringHexadecimalEscape(byte value, int position, State nextState)
    {
        if (!Hexadecimal.Contains(value))
        {
            UseRecovery(JsonRecoveryFlags.StringIncompleteEscapes);
            WriteBufferedHexadecimalCharacter();
            return (State.String, 0);
        }

        _escapeBuffer[position] = value;
        if (nextState is State.String)
            WriteBufferedHexadecimalCharacter();

        return (nextState, 1);
    }

    private void WriteBufferedHexadecimalCharacter()
    {
        var hexBytes = _escapeBuffer.GetBytes();
        if (hexBytes.Length <= 4)
        {
            OutputStream.Write("\\u0000"u8.Slice(0, 6 - hexBytes.Length));
            OutputStream.Write(hexBytes);
            return;
        }

        WriteStringCharacter(uint.Parse(_escapeBuffer.GetBytes(), NumberStyles.HexNumber));
    }

    private (State NextState, int Consumed) ProcessInNumberIntegralStart(ReadOnlySpan<byte> buffer)
    {
        switch (buffer[0])
        {
            case (byte)'0':                     return (State.NumberIntegralZero, 1);
            case >= (byte)'1' and <= (byte)'9': return (State.NumberIntegral, PassThroughWhile(buffer, Decimal).Consumed);
            case (byte)'.':
                UseRecovery(JsonRecoveryFlags.NumberMissingDigits);
                OutputStream.Write("0."u8);
                return (State.NumberFractionalStart, 1);
            default:
                UseRecovery(JsonRecoveryFlags.NumberMissingDigits);
                OutputStream.WriteByte((byte)'0');
                return (State.ValueEnd, 0);
        }
    }

    private (State NextState, int Consumed) ProcessInNumberIntegralZero(ReadOnlySpan<byte> buffer)
    {
        switch (buffer[0])
        {
            case (byte)'0':
                UseRecovery(JsonRecoveryFlags.NumberLeadingZeroes);
                return (State.NumberIntegralZero, buffer.IndexOfAnyExcept((byte)'0'));
            case >= (byte)'1' and <= (byte)'9':
                UseRecovery(JsonRecoveryFlags.NumberLeadingZeroes);
                return (State.NumberIntegral, PassThroughWhile(buffer, Decimal).Consumed);
            case (byte)'.':
                OutputStream.Write("0."u8);
                return (State.NumberFractionalStart, 1);
            case (byte)'e' or (byte)'E':
                OutputStream.WriteByte((byte)'0');
                OutputStream.WriteByte(buffer[0]);
                return (State.NumberExponentSign, 1);
            default:
                OutputStream.WriteByte((byte)'0');
                return (State.ValueEnd, 0);
        }
    }

    private (State NextState, int Consumed) ProcessInNumberIntegral(ReadOnlySpan<byte> buffer)
    {
        switch (buffer[0])
        {
            case >= (byte)'0' and <= (byte)'9': return PassThroughWhile(buffer, Decimal);
            case (byte)'.':
                OutputStream.WriteByte((byte)'.');
                return (State.NumberFractionalStart, 1);
            case (byte)'e' or (byte)'E':
                OutputStream.WriteByte(buffer[0]);
                return (State.NumberExponentSign, 1);
            default: return (State.ValueEnd, 0);
        }
    }

    private (State NextState, int Consumed) ProcessInNumberFractionalStart(ReadOnlySpan<byte> buffer)
    {
        switch (buffer[0])
        {
            case >= (byte)'0' and <= (byte)'9': return (State.NumberFractional, PassThroughWhile(buffer, Decimal).Consumed);
            case (byte)'e' or (byte)'E':
                UseRecovery(JsonRecoveryFlags.NumberMissingDigits);
                OutputStream.WriteByte((byte)'0');
                OutputStream.WriteByte(buffer[0]);
                return (State.NumberExponentSign, 1);
            default:
                UseRecovery(JsonRecoveryFlags.NumberMissingDigits);
                OutputStream.WriteByte((byte)'0');
                return (State.ValueEnd, 0);
        }
    }

    private (State NextState, int Consumed) ProcessInNumberFractional(ReadOnlySpan<byte> buffer)
    {
        switch (buffer[0])
        {
            case >= (byte)'0' and <= (byte)'9': return PassThroughWhile(buffer, Decimal);
            case (byte)'e' or (byte)'E':
                OutputStream.WriteByte(buffer[0]);
                return (State.NumberExponentSign, 1);
            default: return (State.ValueEnd, 0);
        }
    }

    private (State NextState, int Consumed) ProcessInNumberExponentSign(ReadOnlySpan<byte> buffer)
    {
        switch (buffer[0])
        {
            case (byte)'+' or (byte)'-':
                OutputStream.WriteByte(buffer[0]);
                return (State.NumberExponentStart, 1);
            case >= (byte)'0' and <= (byte)'9': return (State.NumberExponent, PassThroughWhile(buffer, Decimal).Consumed);
            default:
                UseRecovery(JsonRecoveryFlags.NumberMissingDigits);
                OutputStream.WriteByte((byte)'0');
                return (State.ValueEnd, 0);
        }
    }

    private (State NextState, int Consumed) ProcessInNumberExponentStart(ReadOnlySpan<byte> buffer)
    {
        if (Decimal.Contains(buffer[0]))
            return (State.NumberExponent, PassThroughWhile(buffer, Decimal).Consumed);

        UseRecovery(JsonRecoveryFlags.NumberMissingDigits);
        OutputStream.WriteByte((byte)'0');
        return (State.ValueEnd, 0);
    }

    private (State NextState, int Consumed) ProcessInNumberExponent(ReadOnlySpan<byte> buffer)
    {
        if (Decimal.Contains(buffer[0]))
            return PassThroughWhile(buffer, Decimal);

        return (State.ValueEnd, 0);
    }

    private (State NextState, int Consumed) ProcessInKeyword(byte value, byte nextExpected, byte nextBadCase, State nextState)
    {
        if (value != nextExpected)
        {
            if (value == nextBadCase)
                UseRecovery(JsonRecoveryFlags.KeywordCase);
            else
                throw new InvalidDataException();
        }

        OutputStream.WriteByte(value);
        return (nextState, 1);
    }

    private (State NextState, int Consumed) PassThroughWhile(ReadOnlySpan<byte> buffer, SearchValues<byte> values)
    {
        var count = buffer.IndexOfAnyExcept(values);
        if (count < 0)
            count = buffer.Length;
        if (count > 0)
            OutputStream.Write(buffer[..count]);
        return (_state, count);
    }

    private (State NextState, int Consumed) PassThroughUntil(ReadOnlySpan<byte> buffer, SearchValues<byte> values)
    {
        var count = buffer.IndexOfAny(values);
        if (count < 0)
            count = buffer.Length;
        if (count > 0)
            OutputStream.Write(buffer[..count]);
        return (_state, count);
    }

    private (State NextState, int Consumed) BufferWhile(ReadOnlySpan<byte> buffer, SearchValues<byte> values)
    {
        var count = buffer.IndexOfAnyExcept(values);
        if (count < 0)
            count = buffer.Length;
        if (count > 0)
            _buffer.AddRange(buffer[..count]);
        return (_state, count);
    }

    private void FlushBuffer()
    {
        OutputStream.Write(CollectionsMarshal.AsSpan(_buffer));
        _buffer.Clear();
    }

    private void WriteStringCharacter(uint value)
    {
        if (value >= 0x110000)
            throw new InvalidDataException();

        if (value < 0x20)
        {
            var shortEscape = value switch
            {
                '\b' => (byte)'b',
                '\f' => (byte)'f',
                '\n' => (byte)'n',
                '\r' => (byte)'r',
                '\t' => (byte)'t',
                _    => (byte)0,
            };

            if (shortEscape is not 0)
            {
                OutputStream.WriteByte(ReverseSolidus);
                OutputStream.WriteByte(shortEscape);
            }
            else
            {
                OutputStream.Write("\\u00"u8);
                OutputStream.WriteByte(HexadecimalUppercase[unchecked((int)(value >> 4))]);
                OutputStream.WriteByte(HexadecimalUppercase[unchecked((int)(value & 0xF))]);
            }

            return;
        }

        if (value < 0x80)
        {
            OutputStream.WriteByte((byte)value);
            return;
        }

        if (value < 0x800)
        {
            OutputStream.WriteByte((byte)(0xC0u | (value >> 6)));
            OutputStream.WriteByte((byte)(0x80u | (value & 0x3F)));
            return;
        }

        if (value is >= 0xD800 and < 0xE000)
        {
            OutputStream.Write("\\u"u8);
            OutputStream.WriteByte(HexadecimalUppercase[unchecked((int)(value >> 12))]);
            OutputStream.WriteByte(HexadecimalUppercase[unchecked((int)((value >> 8) & 0xF))]);
            OutputStream.WriteByte(HexadecimalUppercase[unchecked((int)((value >> 4) & 0xF))]);
            OutputStream.WriteByte(HexadecimalUppercase[unchecked((int)(value & 0xF))]);
            return;
        }

        if (value < 0x10000)
        {
            OutputStream.WriteByte((byte)(0xE0u | (value >> 12)));
            OutputStream.WriteByte((byte)(0x80u | ((value >> 6) & 0x3F)));
            OutputStream.WriteByte((byte)(0x80u | (value & 0x3F)));
            return;
        }

        OutputStream.WriteByte((byte)(0xF0u | (value >> 18)));
        OutputStream.WriteByte((byte)(0x80u | ((value >> 12) & 0x3F)));
        OutputStream.WriteByte((byte)(0x80u | ((value >> 6) & 0x3F)));
        OutputStream.WriteByte((byte)(0x80u | (value & 0x3F)));
    }

    private void CloseBlock(byte block)
    {
        if (!_blocks.TryPop(out var b) || b != block)
        {
            UseRecovery(JsonRecoveryFlags.IncorrectBlockClosing);
            do
            {
                OutputStream.WriteByte(b);
                if (!_blocks.TryPop(out b))
                    throw new InvalidDataException();
            } while (b != block);
        }

        OutputStream.WriteByte(b);
    }

    private enum State
    {
        ValueStart,
        ValueEnd,

        KeyStart,
        KeyEnd,

        AfterLeftSquareBracket,
        AfterComma,

        String,
        StringEscape,
        StringOctal1,
        StringOctal2,
        StringHexadecimal0,
        StringHexadecimal1,
        StringUnicode0,
        StringUnicode1,
        StringUnicode2,
        StringUnicode3,
        StringRune0,
        StringRune1,
        StringRune2,
        StringRune3,
        StringRune4,
        StringRune5,
        StringRune6,
        StringRune7,

        NumberIntegralStart,
        NumberIntegralZero,
        NumberIntegral,
        NumberFractionalStart,
        NumberFractional,
        NumberExponentSign,
        NumberExponentStart,
        NumberExponent,

        True1,
        True2,
        True3,

        False1,
        False2,
        False3,
        False4,

        Null1,
        Null2,
        Null3,
    }
}
