namespace Luna;

/// <summary> Extensions for GUIDs. </summary>
public static class GuidExtensions
{
    /// <summary> Hexadecimal characters. </summary>
    private const string CharsLower =
        "0123456789abcdef";

    /// <summary> Hexadecimal characters. </summary>
    private const string CharsUpper =
        "0123456789abcdef";

    /// <summary> Write only the first 8 hexadecimal digits of a GUID to a string (the digits up to the first dash in the default format). </summary>
    /// <param name="guid"> The GUID to shorten and write. </param>
    /// <param name="lowercase"> Whether the hex letter symbols should be lowercase or uppercased. </param>
    public static unsafe string ShortGuid(this Guid guid, bool lowercase = true)
    {
        var bytes = (byte*)&guid;
        var chars = lowercase ? CharsLower : CharsUpper;
        Span<char> text =
        [
            chars[bytes[3] >> 4],
            chars[bytes[3] & 0x0F],
            chars[bytes[2] >> 4],
            chars[bytes[2] & 0x0F],
            chars[bytes[1] >> 4],
            chars[bytes[1] & 0x0F],
            chars[bytes[0] >> 4],
            chars[bytes[0] & 0x0F],
        ];
        return new string(text);
    }
}
