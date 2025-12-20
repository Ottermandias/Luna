namespace Luna;

/// <summary> Utility functions to handle display of data. </summary>
public static class FormattingFunctions
{
    /// <summary> Return a human-readable form of the size using the given format (which should be a float identifier followed by a placeholder). </summary>
    /// <param name="size"> The byte size to display. </param>
    /// <param name="format"> The format to display the data. </param>
    /// <returns> A human-readable format of the size in byte units. </returns>
    public static string HumanReadableSize(long size, string format = "{0:0.#} {1}")
    {
        var    order = 0;
        double s     = size;
        while (s >= 1024 && order < ByteAbbreviations.Length - 1)
        {
            order++;
            s /= 1024;
        }

        return string.Format(format, s, ByteAbbreviations[order]);
    }

    /// <summary> Convert a contiguous array of memory into its hex representation without spaces. </summary>
    /// <param name="bytes"> The data to format. </param>
    /// <param name="capitalized"> Whether the hex-letters should be capitalized or not. </param>
    /// <returns> A UTF8-encoded string consisting of the byte-wise hex data. </returns>
    public static unsafe StringU8 BytewiseHex(ReadOnlySpan<byte> bytes, bool capitalized = true)
    {
        var ret  = new byte[bytes.Length * 2 + 1];
        var span = capitalized ? HexDigitsUpper : HexDigitsLower;
        fixed (byte* retPtr = ret, dataPtr = bytes)
        {
            var end     = dataPtr + bytes.Length;
            var retPtr2 = retPtr;
            for (var ptr = dataPtr; ptr < end; ++ptr)
            {
                *retPtr2++ = span[*ptr & 0xF];
                *retPtr2++ = span[*ptr >> 4];
            }
            *retPtr2 = 0;
        }
        return new StringU8(ret.AsMemory(0, ret.Length - 1));
    }

    /// <summary> Convert a contiguous array of memory into its hex representation with spaces. </summary>
    /// <param name="bytes"> The data to format. </param>
    /// <param name="capitalized"> Whether the hex-letters should be capitalized or not. </param>
    /// <returns> A UTF8-encoded string consisting of the byte-wise hex data. </returns>
    public static unsafe StringU8 BytewiseHexSpaced(ReadOnlySpan<byte> bytes, bool capitalized = true)
    {
        var ret  = new byte[bytes.Length * 3];
        var span = capitalized ? HexDigitsUpper : HexDigitsLower;
        fixed (byte* retPtr = ret, dataPtr = bytes)
        {
            var end     = dataPtr + bytes.Length;
            var retPtr2 = retPtr;
            for (var ptr = dataPtr; ptr < end; ++ptr)
            {
                *retPtr2++ = span[*ptr & 0xF];
                *retPtr2++ = span[*ptr >> 4];
                *retPtr2++ = (byte)' ';
            }
            retPtr2[-1] = 0;
        }
        return new StringU8(ret.AsMemory(0, ret.Length - 1));
    }

    /// <summary> Reasonable byte abbreviations up to exabytes. </summary>
    private static readonly string[] ByteAbbreviations =
    [
        "B",
        "KB",
        "MB",
        "GB",
        "TB",
        "PB",
        "EB",
    ];

    private static ReadOnlySpan<byte> HexDigitsLower
        => "0123456789abcdef"u8;

    private static ReadOnlySpan<byte> HexDigitsUpper
        => "0123456789ABCDEF"u8;
}
