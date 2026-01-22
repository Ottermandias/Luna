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

    /// <inheritdoc cref="DurationString(long,DateTime)"/>
    public static string DurationString(long timestamp)
        => DurationString(timestamp, DateTime.UtcNow);

    /// <summary> Obtain a human-readable duration from a unix millisecond timestamp. </summary>
    /// <param name="timestamp"> The unix epoch timestamp in milliseconds. </param>
    /// <param name="now"> The current time to compare it against. </param>
    /// <returns> A human-readable string of the time difference. </returns>
    public static string DurationString(long timestamp, DateTime now)
    {
        now = now.AddMilliseconds(-now.Millisecond);
        var diff = now - DateTimeOffset.FromUnixTimeMilliseconds(timestamp - timestamp % 1000);
        return diff.TotalSeconds switch
        {
            > 300 * TimeSpan.SecondsPerDay => string.Create(CultureInfo.InvariantCulture, $"{diff.TotalSeconds / 300.0 / TimeSpan.SecondsPerDay:F2} Years"),
            > 10 * TimeSpan.SecondsPerDay  => string.Create(CultureInfo.InvariantCulture, $"{diff.TotalSeconds / 10.0 / TimeSpan.SecondsPerDay:F2} Months"),
            > TimeSpan.SecondsPerDay       => string.Create(CultureInfo.InvariantCulture, $"{diff.TotalSeconds / TimeSpan.SecondsPerDay:F2} Days"),
            > TimeSpan.SecondsPerHour      => $"{diff.Hours}:{diff.Minutes:D2} Hours",
            _                              => $"{diff.Minutes}:{diff.Seconds:D2} Minutes",
        };
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
