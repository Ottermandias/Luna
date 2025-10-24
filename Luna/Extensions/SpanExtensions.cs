using System.Collections.Frozen;

namespace Luna;

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
}
