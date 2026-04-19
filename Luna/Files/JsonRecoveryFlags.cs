namespace Luna;

/// <summary> Invalid JSON kinds to recover from. </summary>
[Flags]
public enum JsonRecoveryFlags
{
    /// <summary> Transform forbidden raw characters in strings into their escaped form. </summary>
    StringRawCharacters = 1 << 0,

    /// <summary> Correct wrongly-cased escape sequences (for example \N). </summary>
    StringEscapeCase = 1 << 1,

    /// <summary> Transform C-like and C#-like escape sequences into their proper JSON equivalents. </summary>
    StringExtendedEscapes = 1 << 2,

    /// <summary> Correct Unicode escapes with insufficient hexadecimal digits. </summary>
    /// <remarks> With <see cref="StringExtendedEscapes"/>, also applies to octal, byte hexadecimal and rune hexadecimal escapes. </remarks>
    StringIncompleteEscapes = 1 << 3,

    /// <summary> Strip backslashes when they are followed by sequences that cannot be considered valid escapes by any of the other rules. </summary>
    StringInvalidEscapes = 1 << 4,

    /// <summary> Strip leading + sign. </summary>
    NumberExplicitPositive = 1 << 5,

    /// <summary> Strip superfluous leading zeroes from numbers' integral parts. </summary>
    NumberLeadingZeroes = 1 << 6,

    /// <summary> Insert zeroes in locations that expect digits but have none. </summary>
    NumberMissingDigits = 1 << 7,

    /// <summary> Correct wrongly-cased keywords (for example True, fAlSe, NULL). </summary>
    KeywordCase = 1 << 8,

    /// <summary> Complete tokens to make the JSON stream syntactically valid if it ends prematurely. </summary>
    PrematureEndOfStream = 1 << 9,

    /// <summary> Insert nulls in places where a value is expected but omitted. </summary>
    MissingValues = 1 << 10,

    /// <summary> Strip trailing commas at the end of objects and arrays. </summary>
    TrailingCommas = 1 << 11,

    /// <summary> Insert commas and colons between consecutive keys and values. </summary>
    MissingPunctuation = 1 << 12,

    /// <summary> Complete tokens to make sure blocks are always correctly closed. </summary>
    /// <remarks> This can introduce other errors down the line or change the document's meaning. </remarks>
    IncorrectBlockClosing = 1 << 13,

    /// <summary> Safe defaults that are unlikely to change the meaning of the document from the intended one. </summary>
    Safe = StringRawCharacters
      | StringEscapeCase
      | StringExtendedEscapes
      | NumberExplicitPositive
      | NumberLeadingZeroes
      | NumberMissingDigits
      | KeywordCase
      | PrematureEndOfStream
      | MissingValues
      | TrailingCommas,

    /// <summary> Everything. Might not respect the intended meaning of the document. The output should be reviewed by a human. </summary>
    All = Safe | StringIncompleteEscapes | StringInvalidEscapes | MissingPunctuation | IncorrectBlockClosing,
}
