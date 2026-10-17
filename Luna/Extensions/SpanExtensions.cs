namespace Luna;

/// <summary> Extension methods for Spans and ReadOnlySpans. </summary>
public static class SpanExtensions
{
    /// <summary> Find the first object fulfilling <paramref name="predicate"/>'s criteria in <paramref name="array"/>, if one exists. </summary>
    /// <returns> True if an object is found, false otherwise. </returns>
    public static bool FindFirst<T>(this ReadOnlySpan<T> array, Predicate<T> predicate, [NotNullWhen(true)] out T? result)
    {
        foreach (var obj in array)
        {
            if (predicate(obj))
            {
                result = obj!;
                return true;
            }
        }

        result = default;
        return false;
    }

    /// <inheritdoc cref="FindFirst{T}(ReadOnlySpan{T},Predicate{T},out T)"/>
    public static bool FindFirst<T>(this Span<T> array, Predicate<T> predicate, [NotNullWhen(true)] out T? result)
        => ((ReadOnlySpan<T>)array).FindFirst(predicate, out result);

    /// <summary> Find the index of the first occurence of <paramref name="needle"/> within <paramref name="array"/>, if one exists. </summary>
    /// <returns> The index if an occurence was found, -1 otherwise. </returns>
    public static int IndexOf<T>(this ReadOnlySpan<T> array, T needle) where T : IEquatable<T>
    {
        for (var i = 0; i < array.Length; ++i)
        {
            if (array[i].Equals(needle))
                return i;
        }

        return -1;
    }

    /// <inheritdoc cref="IndexOf{T}(ReadOnlySpan{T},T)"/>
    public static int IndexOf<T>(this Span<T> array, T needle) where T : IEquatable<T>
        => ((ReadOnlySpan<T>)array).IndexOf(needle);

    /// <summary> Check whether a span contains all passed needles at least once. </summary>
    /// <param name="needle1"> The first value checked. </param>
    /// <param name="needle2"> The second value checked. </param>
    /// <returns> True if the span contains both values. </returns>
    public static bool Contains<T>(this ReadOnlySpan<T> array, T needle1, T needle2) where T : IEquatable<T>
    {
        if (needle1.Equals(needle2))
            return array.Contains(needle1);

        var found1 = false;
        var found2 = false;
        foreach (ref readonly var t in array)
        {
            if (needle1.Equals(t))
            {
                found1 = true;
                if (found2)
                    return true;
            }
            else if (needle2.Equals(t))
            {
                found2 = true;
                if (found1)
                    return true;
            }
        }

        return false;
    }

    /// <summary> Check whether a span contains all passed needles at least once after a selector. </summary>
    /// <param name="predicate1"> The first predicate checked. </param>
    /// <param name="predicate2"> The second predicate checked. </param>
    /// <returns> True if the span contains both values. </returns>
    public static bool Any<T>(this ReadOnlySpan<T> array, Predicate<T> predicate1, Predicate<T> predicate2)
    {
        if (ReferenceEquals(predicate1, predicate2))
        {
            foreach (ref readonly var value in array)
            {
                if (predicate1(value))
                    return true;
            }

            return false;
        }

        var found1 = false;
        var found2 = false;
        foreach (ref readonly var value in array)
        {
            if (predicate1(value))
            {
                found1 = true;
                if (found2)
                    return true;
            }
            else if (predicate2(value))
            {
                found2 = true;
                if (found1)
                    return true;
            }
        }

        return false;
    }

    /// <summary> Write a byte span as a list of hexadecimal bytes separated by spaces. </summary>
    public static string WriteHexBytes(this ReadOnlySpan<byte> bytes)
    {
        var sb = new StringBuilder(bytes.Length * 3);
        for (var i = 0; i < bytes.Length - 1; ++i)
            sb.Append($"{bytes[i]:X2} ");
        sb.Append($"{bytes[^1]:X2}");
        return sb.ToString();
    }

    /// <inheritdoc cref="WriteHexBytes(ReadOnlySpan{byte})"/>
    public static string WriteHexBytes(this Span<byte> bytes)
        => ((ReadOnlySpan<byte>)bytes).WriteHexBytes();

    /// <summary> Write only the difference of a byte span as a list of hexadecimal bytes separated by spaces, keeping equal bytes as double spaces. </summary>
    public static string WriteHexByteDiff(this ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> diff)
    {
        var shorter = Math.Min(bytes.Length, diff.Length);
        var sb      = new StringBuilder(shorter * 3);
        for (var i = 0; i < shorter - 1; ++i)
        {
            var d = (byte)(bytes[i] ^ diff[i]);
            if (d == 0)
                sb.Append("   ");
            else
                sb.Append($"{d:X2} ");
        }

        var last = (byte)(bytes[shorter - 1] ^ diff[shorter - 1]);
        if (last == 0)
            sb.Append("   ");
        else
            sb.Append($"{last:X2}");
        return sb.ToString();
    }

    /// <inheritdoc cref="WriteHexByteDiff(ReadOnlySpan{byte},ReadOnlySpan{byte})"/>
    public static string WriteHexByteDiff(this Span<byte> bytes, ReadOnlySpan<byte> diff)
        => ((ReadOnlySpan<byte>)bytes).WriteHexByteDiff(diff);

    /// <summary> Remove all characters that are invalid in a windows path from the given string. </summary>
    /// <param name="s"> The input string. </param>
    /// <returns> The string with all invalid characters omitted. </returns>
    public static string RemoveInvalidPathSymbols(this ReadOnlySpan<char> s)
    {
        var buffer = s.Length >= 1024 ? new char[s.Length] : stackalloc char[1024];
        var index  = 0;
        foreach (var character in s)
        {
            if (!character.IsInvalidInPath())
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
            if (!character.IsInvalidInFileName())
                buffer[index++] = character;
        }

        return new string(buffer[..index]);
    }

    /// <summary> Trim all whitespace in the front or end of the given byte span and return the resulting span. </summary>
    /// <param name="span"> The input string. </param>
    /// <returns> The trimmed span. </returns>
    public static ReadOnlySpan<byte> Trim(this ReadOnlySpan<byte> span)
    {
        var start = 0;
        var end   = span.Length - 1;
        while (start <= end && char.IsWhiteSpace((char)span[start]))
            ++start;
        while (end >= start && char.IsWhiteSpace((char)span[end]))
            --end;
        return span[start..(end + 1)];
    }

    /// <summary> Trim all whitespace in the front of the given byte span and return the resulting span. </summary>
    /// <param name="span"> The input string. </param>
    /// <returns> The trimmed span. </returns>
    public static ReadOnlySpan<byte> TrimFront(this ReadOnlySpan<byte> span)
    {
        var start = 0;
        var end   = span.Length;
        while (start < end && char.IsWhiteSpace((char)span[start]))
            ++start;
        return span[start..];
    }


    /// <summary> Trim all whitespace in the end of the given byte span and return the resulting span. </summary>
    /// <param name="span"> The input string. </param>
    /// <returns> The trimmed span. </returns>
    public static ReadOnlySpan<byte> TrimEnd(this ReadOnlySpan<byte> span)
    {
        var end = span.Length - 1;
        while (end >= 0 && char.IsWhiteSpace((char)span[end]))
            --end;
        return span[..(end + 1)];
    }
}
