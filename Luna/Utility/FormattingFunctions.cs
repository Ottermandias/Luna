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
}
