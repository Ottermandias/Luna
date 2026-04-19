namespace Luna;

/// <summary> Provides common functionality to write-only streams that act as filters wrapped around their actual output. </summary>
/// <param name="outputStream"> The actual output, where filtered data will get written. </param>
/// <param name="leaveOpen"> Whether to leave the output stream open when closing this filter. </param>
public abstract class OutputFilterStream(Stream outputStream, bool leaveOpen = false) : Stream
{
    protected Stream OutputStream = outputStream;
    protected bool   LeaveOpen    = leaveOpen;

    /// <inheritdoc/>
    public override bool CanRead
        => false;

    /// <inheritdoc/>
    public override bool CanSeek
        => false;

    /// <inheritdoc/>
    public override bool CanWrite
        => OutputStream.CanWrite;

    /// <inheritdoc/>
    public override void Flush()
        => OutputStream.Flush();

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (!LeaveOpen)
                OutputStream.Close();
        }

        base.Dispose(disposing);
    }

    #region Unsupported Stream Operations

    /// <inheritdoc/>
    public override long Length
        => throw new NotSupportedException();

    /// <inheritdoc/>
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
        => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void SetLength(long value)
        => throw new NotSupportedException();

    #endregion
}
