namespace Luna;

public static class SymbolExtensionsUtf8
{
    extension(ReadOnlySpan<byte>)
    {
        /// <summary> An UTF8 string containing an em-dash character. </summary>
        public static ReadOnlySpan<byte> EmDash
            => "\u2014"u8;
    }
}

public static class SymbolExtensionsChar
{
    extension(char)
    {
        /// <summary> An em-dash character. </summary>
        public static char EmDash
            => '\u2014';
    }
}

public static class SymbolExtensionsString
{
    extension(string)
    {
        /// <summary> A string containing an em-dash character. </summary>
        public static string EmDash
            => "\u2014";
    }
}
