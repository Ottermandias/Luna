using System.Collections.Frozen;

namespace Luna;

public static class StringExtensions
{
    /// <inheritdoc cref="string.Contains(string,StringComparison)"/>
    public static bool Contains(this string text, ReadOnlySpan<char> needle, StringComparison comparison = StringComparison.Ordinal)
        => text.AsSpan().Contains(needle, comparison);

    /// <summary> Check whether a value formatted to string contains the given needle without allocations. </summary>
    /// <param name="data"> The value to format. </param>
    /// <param name="needle"> The search string. </param>
    /// <param name="comparison"> The comparison method. </param>
    /// <param name="format"> An optional format for the value. </param>
    /// <param name="provider"> An optional format provider for the value. </param>
    /// <returns> True if the formatted string contains the needle. </returns>
    public static unsafe bool Contains(this ISpanFormattable data, ReadOnlySpan<char> needle,
        StringComparison comparison = StringComparison.Ordinal, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
    {
        Span<char> span = stackalloc char[64];
        if (!data.TryFormat(span, out var written, format, provider))
        {
            span = stackalloc char[1024];
            if (!data.TryFormat(span, out written, format, provider))
                return false;
        }

        return span[..written].Contains(needle, comparison);
    }

    /// <summary> Remove all characters that are invalid in a windows path from the given string. </summary>
    /// <param name="s"> The input string. </param>
    /// <returns> The string with all invalid characters omitted. </returns>
    public static string RemoveInvalidPathSymbols(this ReadOnlySpan<char> s)
    {
        var buffer = s.Length >= 1024 ? new char[s.Length] : stackalloc char[1024];
        var index  = 0;
        foreach (var character in s)
        {
            if (!InvalidPathCharacters.Contains(character))
                buffer[index++] = character;
        }

        return new string(buffer[..index]);
    }

    /// <summary> Remove all characters that are invalid in a windows file name from the given string. </summary>
    /// <param name="s"> The input string. </param>
    /// <returns> The string with all invalid characters omitted. </returns>
    public static string RemoveInvalidFileNameSymbols(this ReadOnlySpan<char> s)
    {
        var buffer = s.Length >= 1024 ? new char[s.Length] : stackalloc char[1024];
        var index  = 0;
        foreach (var character in s)
        {
            if (!InvalidFileNameCharacters.Contains(character))
                buffer[index++] = character;
        }

        return new string(buffer[..index]);
    }

    /// <summary> A set of all UTF16 characters invalid in file names. </summary>
    public static readonly FrozenSet<char> InvalidFileNameCharacters = Path.GetInvalidFileNameChars().ToFrozenSet();

    /// <summary> A set of all UTF16 characters invalid in paths. </summary>
    public static readonly FrozenSet<char> InvalidPathCharacters = Path.GetInvalidPathChars().ToFrozenSet();
}
