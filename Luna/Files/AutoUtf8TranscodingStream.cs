namespace Luna;

/// <summary> Strips the byte order mark (BOM) from a UTF-8 stream. If a UTF-16 BOM is recognized, transcodes the stream to UTF-8. </summary>
/// <param name="outputStream"> The output stream that will receive UTF-8. </param>
/// <param name="leaveOpen"> Whether to leave the output stream open when closing this filter. </param>
public sealed class AutoUtf8TranscodingStream(Stream outputStream, bool leaveOpen = false) : OutputFilterStream(outputStream, leaveOpen)
{
    private State     _state       = State.Initial;
    private Encoding? _bomEncoding = null;

    /// <summary> The original encoding, if a BOM was recognized and stripped, otherwise <c>null</c>. This property is safe to read after the stream is closed. </summary>
    public Encoding? BomEncoding
        => _bomEncoding;

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
    {
        while (_state is not State.PassThrough && count > 0)
        {
            if (Process(buffer[offset]))
            {
                offset += 1;
                count  -= 1;
            }
        }

        if (count > 0)
            OutputStream.Write(buffer, offset, count);
    }

    /// <inheritdoc/>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        while (_state is not State.PassThrough && buffer.Length > 0)
        {
            if (Process(buffer[0]))
                buffer = buffer[1..];
        }

        if (buffer.Length > 0)
            OutputStream.Write(buffer);
    }

    /// <inheritdoc/>
    public override void WriteByte(byte value)
    {
        while (_state is not State.PassThrough)
        {
            if (Process(value))
                return;
        }

        OutputStream.WriteByte(value);
    }

    private bool Process(byte value)
    {
        var (nextState, consumed) = DoProcess(value);
        if (!consumed && nextState == _state)
            throw new UnreachableException();

        _state = nextState;
        return consumed;
    }

    private (State NextState, bool Consumed) DoProcess(byte value)
    {
        switch (_state, value)
        {
            case (State.Initial, 0xEF): return (State.Utf8First, true);
            case (State.Initial, 0xFE): return (State.Utf16BeMiddle, true);
            case (State.Initial, 0xFF): return (State.Utf16LeMiddle, true);
            case (State.Initial, _):    return (State.PassThrough, false);

            case (State.Utf8First, 0xBB): return (State.Utf8Second, true);
            case (State.Utf8First, _):
                OutputStream.WriteByte(0xEF);
                return (State.PassThrough, false);

            case (State.Utf8Second, 0xBF):
                _bomEncoding = Encoding.UTF8;
                return (State.PassThrough, true);
            case (State.Utf8Second, _):
                OutputStream.Write([0xEF, 0xBB]);
                return (State.PassThrough, false);

            case (State.Utf16LeMiddle, 0xFE):
                _bomEncoding = Encoding.Unicode;
                OutputStream = Encoding.CreateTranscodingStream(OutputStream, Encoding.UTF8, Encoding.Unicode, LeaveOpen);
                LeaveOpen    = false;
                return (State.PassThrough, true);
            case (State.Utf16LeMiddle, _):
                OutputStream.WriteByte(0xFF);
                return (State.PassThrough, false);

            case (State.Utf16BeMiddle, 0xFF):
                _bomEncoding = Encoding.BigEndianUnicode;
                OutputStream = Encoding.CreateTranscodingStream(OutputStream, Encoding.UTF8, Encoding.BigEndianUnicode, LeaveOpen);
                LeaveOpen    = false;
                return (State.PassThrough, true);
            case (State.Utf16BeMiddle, _):
                OutputStream.WriteByte(0xFE);
                return (State.PassThrough, false);

            default: throw new UnreachableException();
        }
    }

    private enum State
    {
        Initial,

        Utf8First,
        Utf8Second,
        Utf16LeMiddle,
        Utf16BeMiddle,

        PassThrough = -1,
    }
}
