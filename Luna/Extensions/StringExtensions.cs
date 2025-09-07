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

            return ((ReadOnlySpan<char>)span[..written]).Contains(needle, comparison);
        }

        return ((ReadOnlySpan<char>)span[..written]).Contains(needle, comparison);
    }
}
