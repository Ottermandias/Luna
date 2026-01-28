using Dalamud.Interface;

namespace Luna;

/// <summary> A struct containing the printable bytes for a <see cref="FontAwesomeIcon"/>. </summary>
public readonly struct AwesomeIcon : IIconStandIn, IEquatable<AwesomeIcon>, IEqualityOperators<AwesomeIcon, AwesomeIcon, bool>
{
    private readonly ulong _data;

    /// <summary> Create a printable byte-span from an enum value. </summary>
    public unsafe AwesomeIcon(FontAwesomeIcon icon)
    {
        var iconChar = icon.ToIconChar();
        var tmp      = 0ul;
        var bytes    = (byte)Encoding.UTF8.GetBytes(&iconChar, 1, (byte*)&tmp, 8);
        _data = tmp | ((ulong)bytes << 40);
    }

    public static implicit operator AwesomeIcon(FontAwesomeIcon icon)
        => new(icon);

    /// <inheritdoc/>
    public unsafe ReadOnlySpan<byte> Span
    {
        get
        {
            fixed (ulong* ptr = &_data)
            {
                return new Span<byte>(ptr, (int)(_data >> 40));
            }
        }
    }

    /// <inheritdoc/>
    public static unsafe Im.Font Font
        => (Im.Native.ImFont*)UiBuilder.IconFont.Handle;

    public bool Equals(AwesomeIcon other)
        => _data == other._data;

    public override bool Equals(object? obj)
        => obj is AwesomeIcon other && Equals(other);

    public override int GetHashCode()
        => _data.GetHashCode();

    public static bool operator ==(AwesomeIcon left, AwesomeIcon right)
        => left.Equals(right);

    public static bool operator !=(AwesomeIcon left, AwesomeIcon right)
        => !left.Equals(right);
}
