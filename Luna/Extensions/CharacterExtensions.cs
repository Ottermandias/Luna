using System.Collections.Frozen;

namespace Luna;

/// <summary> Extensions for characters. </summary>
public static class CharacterExtensions
{
    /// <summary> A set of all UTF16 characters invalid in file names. </summary>
    public static readonly FrozenSet<char> InvalidFileNameCharacters = Path.GetInvalidFileNameChars().ToFrozenSet();

    /// <summary> A set of all UTF16 characters invalid in paths. </summary>
    public static readonly FrozenSet<char> InvalidPathCharacters = Path.GetInvalidPathChars().ToFrozenSet();

    /// <summary> Get whether this character is invalid in a windows file name. </summary>
    /// <param name="c"> The character to check. </param>
    /// <returns> Whether the character is allowed in a windows file name. </returns>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public static bool IsInvalidInFileName(this char c)
        => InvalidFileNameCharacters.Contains(c);

    /// <summary> Get whether this character is invalid in a windows file name. </summary>
    /// <param name="c"> The character to check. </param>
    /// <returns> Whether the character is allowed in a windows file name. </returns>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public static bool IsInvalidInPath(this char c)
        => InvalidPathCharacters.Contains(c);

    /// <summary> Get whether this character is an ASCII character. </summary>
    /// <param name="c"> The character to check. </param>
    /// <returns> Whether the character is representable with a default ASCII symbol. </returns>
    public static bool IsInvalidAscii(this char c)
        => c >= 128;
}
