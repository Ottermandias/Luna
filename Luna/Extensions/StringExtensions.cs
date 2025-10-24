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

    /// <summary> Normalize for nicer names, and remove invalid symbols or invalid paths, trim whitespace from the start and end. </summary>
    /// <param name="s"> The string to normalize. </param>
    /// <param name="onlyAscii"> Whether only ASCII symbols are allowed in the resulting string. </param>
    /// <param name="replacement"> The replacement string for each replaced invalid symbol. </param>
    /// <returns> A string KC-normalized, trimmed and with all invalid symbols replaced by <paramref cref="replacement"/>. This string can be empty. </returns>
    public static string ReplaceBadXivSymbols(this string s, bool onlyAscii, string replacement = "_")
    {
        switch (s)
        {
            case ".":  return replacement;
            case "..": return replacement + replacement;
        }

        var           normalized               = s.Normalize(NormalizationForm.FormKC);
        StringBuilder sb                       = new(normalized.Length);
        var           encounteredNonWhiteSpace = false;
        foreach (var c in normalized)
        {
            if (!encounteredNonWhiteSpace)
            {
                if (char.IsWhiteSpace(c))
                    continue;

                encounteredNonWhiteSpace = true;
            }

            if (c.IsInvalidInFileName() || onlyAscii && c.IsInvalidAscii())
                sb.Append(replacement);
            else
                sb.Append(c);
        }

        while (sb.Length != 0 && char.IsWhiteSpace(sb[^1]))
            --sb.Length;

        return sb.ToString();
    }
}
