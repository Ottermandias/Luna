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
      | TrailingCommas,

    /// <summary> Everything. Might not respect the intended meaning of the document. The output should be reviewed by a human. </summary>
    All = Safe | StringIncompleteEscapes | StringInvalidEscapes | MissingValues | MissingPunctuation | IncorrectBlockClosing,
}

/// <summary> Extensions for JsonRecoveryFlags. </summary>
public static class JsonRecoveryExtensions
{
    extension(JsonRecoveryFlags flags)
    {
        /// <summary> Add textual versions as well as a hex representation of the active recovery flags to a string builder. </summary>
        /// <param name="sb"> The string builder to append to. </param>
        /// <returns> The string builder for method chaining. </returns>
        public StringBuilder AddToString(StringBuilder sb)
        {
            if (flags is 0)
                return sb;

            var appended = false;
            if (flags.HasFlag(JsonRecoveryFlags.StringRawCharacters))
            {
                sb.Append("correction of raw string characters");
                appended = true;
            }

            if (flags.CheckAny(JsonRecoveryFlags.StringEscapeCase
                  | JsonRecoveryFlags.StringExtendedEscapes
                  | JsonRecoveryFlags.StringIncompleteEscapes
                  | JsonRecoveryFlags.StringInvalidEscapes))
            {
                if (appended)
                    sb.Append(", ");
                appended = true;
                sb.Append("correction of wrongly escaped clauses");
            }

            if (flags.HasFlag(JsonRecoveryFlags.NumberExplicitPositive))
            {
                if (appended)
                    sb.Append(", ");
                appended = true;
                sb.Append("stripped leading pluses");
            }

            if (flags.HasFlag(JsonRecoveryFlags.NumberLeadingZeroes))
            {
                if (appended)
                    sb.Append(", ");
                appended = true;
                sb.Append("stripped leading zeroes");
            }

            if (flags.HasFlag(JsonRecoveryFlags.NumberMissingDigits))
            {
                if (appended)
                    sb.Append(", ");
                appended = true;
                sb.Append("added missing numbers");
            }

            if (flags.HasFlag(JsonRecoveryFlags.KeywordCase))
            {
                if (appended)
                    sb.Append(", ");
                appended = true;
                sb.Append("fixed casing of keywords");
            }

            if (flags.HasFlag(JsonRecoveryFlags.PrematureEndOfStream))
            {
                if (appended)
                    sb.Append(", ");
                appended = true;
                sb.Append("extended end of data");
            }

            if (flags.HasFlag(JsonRecoveryFlags.MissingValues))
            {
                if (appended)
                    sb.Append(", ");
                appended = true;
                sb.Append("added null values where missing");
            }

            if (flags.HasFlag(JsonRecoveryFlags.MissingPunctuation))
            {
                if (appended)
                    sb.Append(", ");
                appended = true;
                sb.Append("inserted missing syntax");
            }

            if (flags.HasFlag(JsonRecoveryFlags.IncorrectBlockClosing))
            {
                if (appended)
                    sb.Append(", ");
                sb.Append("closed open blocks");
            }

            return sb.Append($" ({flags:X})");
        }
    }
}
