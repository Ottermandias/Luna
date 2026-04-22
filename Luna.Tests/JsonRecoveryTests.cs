using System.Text;

namespace Luna.Tests;

public class JsonRecoveryTests
{
    [Theory]
    [MemberData(nameof(ValidJsonPassthroughData))]
    public void ValidJsonPassthroughTest(string data)
        => WithWhitespaceCombinations(data, static data => TestRecovery(data, 0, data));

    public static readonly TheoryData<string> ValidJsonPassthroughData =
    [
        // Keywords
        "true",
        "false",
        "null",

        // Strings
        "\"hello\"",
        "\"hello world\"",
        "\"hello\\nworld\"",
        "\"this\\\"is\\\\an\\/escape\\rtest\\tlorem\\nipsum\\fdolor\\bsit amet\"",
        "\"\\uD83D\\uDC4B = :wave:\"",

        // Numbers
        "-272",
        "-2.72e2",
        "-12.34",
        "-0.736",
        "-7.36e-1",
        "-0",
        "0",
        "0e10",
        "1.672e-27",
        "0.707",
        "3.14",
        "4", // Chosen by fair dice roll. Guaranteed to be random.
        "42",
        "5.9722e24",
        "1.9884e+30",
        "1e100",
        "1e+100",

        // Arrays
        "[]",
        "[ ]",
        "[\"hello\",\"world\"]",
        "[ \"human\", \"giraffe\", \"potato\", \"cat\", \"giant\", \"lizard\", \"lion\", \"rabbit\" ]",
        "[true,\"utf-8\",42]",
        "[true ,\"utf-8\" ,42 ]",
        "[ true, \"utf-8\", 42]",
        "[ true , \"utf-8\" , 42 ]",

        // Objects
        "{}",
        "{ }",
        "{\"otters\":\"cool\"}",
        "{ \"earth\": 5.9722e24, \"sun\": 1.9884e+30, \"proton\": 1.672e-27 }",
        "{ \"races\": [ \"human\", \"giraffe\", \"potato\", \"cat\", \"giant\", \"lizard\", \"lion\", \"rabbit\" ] }",
        "{\"recovered\":true,\"encoding\":\"utf-8\",\"recoveries\":42}",
        "{\"recovered\" :true ,\"encoding\" :\"utf-8\" ,\"recoveries\" :42 }",
        "{ \"recovered\": true, \"encoding\": \"utf-8\", \"recoveries\": 42}",
        "{ \"recovered\" : true , \"encoding\" : \"utf-8\" , \"recoveries\" : 42 }",

        // Almost real-world data
        "{\n  \"FileVersion\": 3,\n  \"Name\": \"My awesome outfit\",\n  \"Author\": \"Anonymous\",\n  \"Description\": \"This set will get all eyes on you in the party.\\n\\nAvailable for all bodies.\",\n  \"Version\": \"1.0.0\",\n  \"Website\": \"https://example.org/\",\n  \"ModTags\": [\n    \"gear\"\n  ],\n  \"DefaultPreferredItems\": null\n}",
        "{\n  \"Name\": \"Material\",\n  \"Description\": \"\",\n  \"Priority\": 4,\n  \"DefaultSettings\": 0,\n  \"Type\": \"Single\",\n  \"Options\": [\n    {\n      \"Name\": \"Red\",\n      \"Description\": \"\",\n      \"Priority\": 0,\n      \"Files\": {\n        \"chara/equipment/e7777/material/v0001/mt_c0201e7777_top_top.mtrl\": \"files\\\\materials\\\\top.mtrl\"\n      },\n      \"FileSwaps\": {},\n      \"Manipulations\": []\n    }, {      \"Name\": \"Blue\",\n      \"Description\": \"\",\n      \"Priority\": 0,\n      \"Files\": {\n        \"chara/equipment/e7777/material/v0001/mt_c0201e7777_top_top.mtrl\": \"files\\\\materials\\\\top_blue.mtrl\"\n      },\n      \"FileSwaps\": {},\n      \"Manipulations\": []\n    }\n  ]\n}",
    ];

    [Theory]
    [MemberData(nameof(UnrecoverableJsonData))]
    public void UnrecoverableJsonTest(string data)
        => WithWhitespaceCombinations(data, static data =>
        {
            var input = Encoding.UTF8.GetBytes(data);
            Assert.Throws<InvalidDataException>(() => RecoverSingleCall(input, JsonRecoveryFlags.All, out _));
            Assert.Throws<InvalidDataException>(() => RecoverByteByByte(input, JsonRecoveryFlags.All, out _));
        });

    public static readonly TheoryData<string> UnrecoverableJsonData =
    [
        "otters are cool",        // Arbitrary string, not JSON
        "true be or not true be", // JSON with arbitrary stuff tacked at the end
        "{3.14: 2.718}",          // Non-string key
        "{\"hello\" world}",      // Invalid stuff after a key
        "true, false",            // Comma out of a block
        "fact",                   // Keyword bait-and-switch (starts like false)
        "}",                      // Closing a block that was never opened (IncorrectBlockClosing cannot recover from that)
        "[}",                     // Ditto
        "\\U01234567",            // Out of range (StringExtendedEscapes allows up to \U0010FFFF)
    ];

    [Theory]
    [MemberData(nameof(PrematureEndOfStreamCompletionData))]
    public void PrematureEndOfStreamCompletionTest(string input, string completion)
        => TestRecovery(input, JsonRecoveryFlags.PrematureEndOfStream, input + completion);

    public static readonly TheoryData<string, string> PrematureEndOfStreamCompletionData =
        new TheoryData<string, string>
        {
            // Keywords
            { "t", "rue" },
            { "tr", "ue" },
            { "tru", "e" },
            { "f", "alse" },
            { "fa", "lse" },
            { "fal", "se" },
            { "fals", "e" },
            { "n", "ull" },
            { "nu", "ll" },
            { "nul", "l" },

            // Strings
            { "\"", "\"" },
            { "\"hello", "\"" },
            { "\"\\u", "0000\"" },
            { "\"\\u4", "000\"" },
            { "\"\\u43", "00\"" },
            { "\"\\u432", "0\"" },
            { "\"\\u4321", "\"" },

            // Numbers
            { "-", "0" },
            { "0.", "0" },
            { "1e", "0" },
            { "1.2e+", "0" },
            { "4.", "0" },

            // Arrays
            { "[", "]" },
            { "[\"hello\"", "]" },
            { "[\"hi", "\"]" },
            { "[42", "]" },

            // Objects
            { "{ \"otters\": \"are", "\"}" },
            { "{\"sun\": 1.9884e", "0}" },
            { "{ \"FileVersion", "\":null}" },
            { "{ \"FileVersion\"", ":null}" },
            { "{ \"FileVersion\":", "null}" },
        };

    [Theory]
    [MemberData(nameof(PrematureEndOfStreamReplacementData))]
    public void PrematureEndOfStreamReplacementTest(string input, string replacement)
        => TestRecovery(input, JsonRecoveryFlags.PrematureEndOfStream, replacement);

    public static readonly TheoryData<string, string> PrematureEndOfStreamReplacementData =
        new TheoryData<string, string>
        {
            // Strings
            { "\"\\", "\"\"" },

            // Arrays
            { "[\"hello\",", "[\"hello\"]" },
            { "[\n\t\"hello\",\n", "[\n\t\"hello\"\n]" },

            // Objects
            { "{\"hello\": \"world\",", "{\"hello\": \"world\"}" },
            { "{\n\t\"hello\": \"world\",\n", "{\n\t\"hello\": \"world\"\n}" },
        };

    [Theory]
    [MemberData(nameof(StringRawCharactersData))]
    public void StringRawCharactersTest(string input, string replacement)
        => TestRecovery(input, JsonRecoveryFlags.StringRawCharacters, replacement);

    public static readonly TheoryData<string, string> StringRawCharactersData =
        new TheoryData<string, string>
        {
            {
                "\"This is an\tawesome description.\r\n\r\nUnfortunately,\bit is not\fJSON-compliant.\"",
                "\"This is an\\tawesome description.[NL][NL]Unfortunately,\\bit is not\\fJSON-compliant.\""
            },
            {
                "{\n  \"Description\": \"This is an\tawesome description.\r\n\r\nUnfor\rtuna\r\btely,\bit is not\fJSON-compliant.\"\n}",
                "{\n  \"Description\": \"This is an\\tawesome description.[NL][NL]Unfor\\rtuna\\r\\btely,\\bit is not\\fJSON-compliant.\"\n}"
            },
        };

    [Theory]
    [MemberData(nameof(StringEscapeCaseData))]
    public void StringEscapeCaseTest(string input, string replacement)
        => TestRecovery(input, JsonRecoveryFlags.StringEscapeCase, replacement);

    public static readonly TheoryData<string, string> StringEscapeCaseData =
        new TheoryData<string, string>
        {
            {
                "\"This is an\\Tawesome description.\\R\\N\\R\\NUnfortunately,\\Bit is not\\FJSON-compliant\\U002E\"",
                "\"This is an\\tawesome description.\\r\\n\\r\\nUnfortunately,\\bit is not\\fJSON-compliant\\u002E\""
            },
            {
                "{\n  \"Description\": \"This is an\\Tawesome description.\\R\\N\\R\\NUnfortunately,\\Bit is not\\FJSON-compliant\\U002E\"\n}",
                "{\n  \"Description\": \"This is an\\tawesome description.\\r\\n\\r\\nUnfortunately,\\bit is not\\fJSON-compliant\\u002E\"\n}"
            },
        };

    [Theory]
    [MemberData(nameof(StringExtendedEscapesData))]
    public void StringExtendedEscapesTest(string input, string replacement)
        => TestRecovery(input, JsonRecoveryFlags.StringExtendedEscapes, replacement);

    public static readonly TheoryData<string, string> StringExtendedEscapesData =
        new TheoryData<string, string>
        {
            {
                "\"This is an\\aawesome description.\\x0D\\x0A\\U0000000D\\U0000000AUnfortunately\\054\\eit isn\\'t\\vJSON-compliant.\\0Or is it\\?\\777\"",
                "\"This is an\\u0007awesome description.\\u000D\\u000A\\r\\nUnfortunately,\\u001Bit isn't\\u000BJSON-compliant.\\u0000Or is it?\u01FF\""
            },
            {
                "{\n  \"Description\": \"This is an\\aawesome description.\\x0D\\x0A\\U0000000D\\U0000000AUnfortunately\\054\\eit isn\\'t\\vJSON-compliant.\\0Or is it\\?\\777\"\n}",
                "{\n  \"Description\": \"This is an\\u0007awesome description.\\u000D\\u000A\\r\\nUnfortunately,\\u001Bit isn't\\u000BJSON-compliant.\\u0000Or is it?\u01FF\"\n}"
            },
        };

    [Theory]
    [MemberData(nameof(StringIncompleteEscapesData))]
    public void StringIncompleteEscapesTest(string input, string replacement)
        => TestRecovery(input, JsonRecoveryFlags.StringIncompleteEscapes, replacement);

    public static readonly TheoryData<string, string> StringIncompleteEscapesData =
        new TheoryData<string, string>
        {
            {
                "\"This is an awesome description.\\uD\\uA\\uD\\uAUnfortunately, it is not JSON-compliant.\"",
                "\"This is an awesome description.\\u000D\\u000A\\u000D\\u000AUnfortunately, it is not JSON-compliant.\""
            },
            {
                "{\n  \"Description\": \"This is an awesome description.\\uD\\uA\\uD\\uAUnfortunately, it is not JSON-compliant.\"\n}",
                "{\n  \"Description\": \"This is an awesome description.\\u000D\\u000A\\u000D\\u000AUnfortunately, it is not JSON-compliant.\"\n}"
            },
        };

    [Theory]
    [MemberData(nameof(StringInvalidEscapesData))]
    public void StringInvalidEscapesTest(string input, string replacement)
        => TestRecovery(input, JsonRecoveryFlags.StringInvalidEscapes, replacement);

    public static readonly TheoryData<string, string> StringInvalidEscapesData =
        new TheoryData<string, string>
        {
            {
                "\"T\\h\\i\\s\\ is an awesome description\\.\\r\\n\\r\\nUnfortunately\\, it is not JSON\\-compliant\\.\"",
                "\"This is an awesome description.\\r\\n\\r\\nUnfortunately, it is not JSON-compliant.\""
            },
            {
                "{\n  \"Description\": \"T\\h\\i\\s\\ is an awesome description\\.\\r\\n\\r\\nUnfortunately\\, it is not JSON\\-compliant\\.\"\n}",
                "{\n  \"Description\": \"This is an awesome description.\\r\\n\\r\\nUnfortunately, it is not JSON-compliant.\"\n}"
            },
        };

    [Theory]
    [MemberData(nameof(NumberExplicitPositiveData))]
    public void NumberExplicitPositiveTest(string input, string replacement)
        => TestRecovery(input, JsonRecoveryFlags.NumberExplicitPositive, replacement);

    public static readonly TheoryData<string, string> NumberExplicitPositiveData =
        new TheoryData<string, string>
        {
            { "+3", "3" },
            { "{\n  \"FileVersion\": +3\n}", "{\n  \"FileVersion\": 3\n}" },
        };

    [Theory]
    [MemberData(nameof(NumberLeadingZeroesData))]
    public void NumberLeadingZeroesTest(string input, string replacement)
        => TestRecovery(input, JsonRecoveryFlags.NumberLeadingZeroes, replacement);

    public static readonly TheoryData<string, string> NumberLeadingZeroesData =
        new TheoryData<string, string>
        {
            { "00", "0" },
            { "00.1", "0.1" },
            { "03", "3" },
            { "003", "3" },
            { "03.14", "3.14" },
            { "010e0100", "10e0100" },
            { "{\n  \"FileVersion\": 03\n}", "{\n  \"FileVersion\": 3\n}" },
        };

    [Theory]
    [MemberData(nameof(NumberMissingDigitsData))]
    public void NumberMissingDigitsTest(string input, string replacement)
        => TestRecovery(input, JsonRecoveryFlags.NumberMissingDigits, replacement);

    public static readonly TheoryData<string, string> NumberMissingDigitsData =
        new TheoryData<string, string>
        {
            { ".707", "0.707" },
            { "-.736", "-0.736" },
            { "[.707]", "[0.707]" },
            { "[4.]", "[4.0]" },
            { "[-.736]", "[-0.736]" },
            { "[1e]", "[1e0]" },
            { "[4.e]", "[4.0e0]" },
            { "[.12e]", "[0.12e0]" },
            { "[.e]", "[0.0e0]" },
            { "[-]", "[-0]" },
            { "[1.2e+]", "[1.2e+0]" },
        };

    [Theory]
    [MemberData(nameof(KeywordCaseData))]
    public void KeywordCaseTest(string input)
        => WithWhitespaceCombinations(input, static input => TestRecovery(input, JsonRecoveryFlags.KeywordCase, input.ToLowerInvariant()));

    public static readonly TheoryData<string> KeywordCaseData =
    [
        "True",
        "TRUE",
        "TrUe",

        "False",
        "FALSE",
        "FaLsE",

        "Null",
        "NULL",
        "NuLl",
    ];

    [Theory]
    [MemberData(nameof(MissingValuesData))]
    public void MissingValuesTest(string input, string replacement)
        => TestRecovery(input, JsonRecoveryFlags.MissingValues, replacement);

    public static readonly TheoryData<string, string> MissingValuesData =
        new TheoryData<string, string>
        {
            { "[12,,false]", "[12,null,false]" },
            { "{\"hello\",,\"world\"}", "{\"hello\":null,\"\":null,\"world\":null}" },
            { "{\"hello\":,\"world\":}", "{\"hello\":null,\"world\":null}" },
        };

    [Theory]
    [MemberData(nameof(TrailingCommasData))]
    public void TrailingCommasTest(string input, string replacement)
        => TestRecovery(input, JsonRecoveryFlags.TrailingCommas, replacement);

    public static readonly TheoryData<string, string> TrailingCommasData =
        new TheoryData<string, string>
        {
            { "[12,false,]", "[12,false]" },
            { "{\"hello\":\"world\",}", "{\"hello\":\"world\"}" },
        };

    [Theory]
    [MemberData(nameof(MissingPunctuationData))]
    public void MissingPunctuationTest(string input, string replacement)
        => TestRecovery(input, JsonRecoveryFlags.MissingPunctuation, replacement);

    public static readonly TheoryData<string, string> MissingPunctuationData =
        new TheoryData<string, string>
        {
            { "[12 false]", "[12 ,false]" },
            { "{\"hello\" \"world\" \"otters\" \"are cool\"}", "{\"hello\" :\"world\" ,\"otters\" :\"are cool\"}" },
        };

    [Theory]
    [MemberData(nameof(IncorrectBlockClosingData))]
    public void IncorrectBlockClosingTest(string input, string replacement)
        => TestRecovery(input, JsonRecoveryFlags.IncorrectBlockClosing, replacement);

    public static readonly TheoryData<string, string> IncorrectBlockClosingData =
        new TheoryData<string, string>
        {
            { "{\"ModTags\":[\"gear\"}", "{\"ModTags\":[\"gear\"]}" },
            { "{\n  \"ModTags\":[\n    \"gear\"\n}", "{\n  \"ModTags\":[\n    \"gear\"\n]}" },

            // This might not always give the result one would want, hence its exclusion from Safe.
            { "[{\"hello\":[\"world\",{\"hello\":[\"people\"]]]", "[{\"hello\":[\"world\",{\"hello\":[\"people\"]}]}]" },
        };

    [Theory]
    [MemberData(nameof(CompositeRecoveryData))]
    public void CompositeRecoveryTest(string input, JsonRecoveryFlags recoveries, string expectedOutput)
        => TestRecovery(input, recoveries, expectedOutput);

    public static readonly TheoryData<string, JsonRecoveryFlags, string> CompositeRecoveryData =
        new TheoryData<string, JsonRecoveryFlags, string>
        {
            {
                "\"This is an\\Aawesome description.\\X0D\\X0A\\U0000000D\\U0000000AUnfortunately\\054\\Eit isn\\'t\\VJSON-compliant.\\0Or is it\\?\"",
                JsonRecoveryFlags.StringExtendedEscapes | JsonRecoveryFlags.StringEscapeCase,
                "\"This is an\\u0007awesome description.\\u000D\\u000A\\r\\nUnfortunately,\\u001Bit isn't\\u000BJSON-compliant.\\u0000Or is it?\""
            },
            {
                "{\n  \"Description\": \"This is an\\Aawesome description.\\X0D\\X0A\\U0000000D\\U0000000AUnfortunately\\054\\Eit isn\\'t\\VJSON-compliant.\\0Or is it\\?\"\n}",
                JsonRecoveryFlags.StringExtendedEscapes | JsonRecoveryFlags.StringEscapeCase,
                "{\n  \"Description\": \"This is an\\u0007awesome description.\\u000D\\u000A\\r\\nUnfortunately,\\u001Bit isn't\\u000BJSON-compliant.\\u0000Or is it?\"\n}"
            },
            {
                "\"This is an awesome description.\\15\\xA\\uD\\UAUnfortunately\\54 it isn\\'t JSON-compliant.\\0Or is it\\?\"",
                JsonRecoveryFlags.StringExtendedEscapes | JsonRecoveryFlags.StringIncompleteEscapes,
                "\"This is an awesome description.\\r\\u000A\\u000D\\u000AUnfortunately, it isn't JSON-compliant.\\u0000Or is it?\""
            },
            {
                "{\n  \"Description\": \"This is an awesome description.\\15\\xA\\uD\\UAUnfortunately\\54 it isn\\'t JSON-compliant.\\0Or is it\\?\"\n}",
                JsonRecoveryFlags.StringExtendedEscapes | JsonRecoveryFlags.StringIncompleteEscapes,
                "{\n  \"Description\": \"This is an awesome description.\\r\\u000A\\u000D\\u000AUnfortunately, it isn't JSON-compliant.\\u0000Or is it?\"\n}"
            },
            { "\"\\1", JsonRecoveryFlags.StringExtendedEscapes | JsonRecoveryFlags.PrematureEndOfStream, "\"@\"" },
            { "\"\\11", JsonRecoveryFlags.StringExtendedEscapes | JsonRecoveryFlags.PrematureEndOfStream, "\"H\"" },
            { "\"\\U", JsonRecoveryFlags.StringExtendedEscapes | JsonRecoveryFlags.PrematureEndOfStream, "\"\\u0000\"" },
            { "\"\\U0", JsonRecoveryFlags.StringExtendedEscapes | JsonRecoveryFlags.PrematureEndOfStream, "\"\\u0000\"" },
            { "\"\\U00", JsonRecoveryFlags.StringExtendedEscapes | JsonRecoveryFlags.PrematureEndOfStream, "\"\\u0000\"" },
            { "\"\\U000", JsonRecoveryFlags.StringExtendedEscapes | JsonRecoveryFlags.PrematureEndOfStream, "\"\\u0000\"" },
            { "\"\\U0001", JsonRecoveryFlags.StringExtendedEscapes | JsonRecoveryFlags.PrematureEndOfStream, "\"\uD800\uDC00\"" },
            { "\"\\U0001F", JsonRecoveryFlags.StringExtendedEscapes | JsonRecoveryFlags.PrematureEndOfStream, "\"\uD83C\uDC00\"" },
            { "\"\\U0001F4", JsonRecoveryFlags.StringExtendedEscapes | JsonRecoveryFlags.PrematureEndOfStream, "\"\uD83D\uDC00\"" },
            { "\"\\U0001F44", JsonRecoveryFlags.StringExtendedEscapes | JsonRecoveryFlags.PrematureEndOfStream, "\"\uD83D\uDC40\"" },
            { "[{\"hello\":]", JsonRecoveryFlags.MissingValues | JsonRecoveryFlags.IncorrectBlockClosing, "[{\"hello\":null}]" },
        };

    [Fact]
    public void WithTranscodingTest()
    {
        const JsonRecoveryFlags recoveries = JsonRecoveryFlags.StringRawCharacters | JsonRecoveryFlags.PrematureEndOfStream;

        var (recoveredBytes, bomEncoding, usedRecoveries) =
            JsonFunctions.RecoverBytes(TranscodingInput, true, recoveries);

        Assert.Equal(ExpectedTranscodingOutput, recoveredBytes);
        Assert.Equal(Encoding.Unicode,          bomEncoding);
        Assert.Equal(recoveries,                usedRecoveries);
    }

    private static readonly byte[] TranscodingInput =
    [
        // BOM
        0xFF, 0xFE,
        // Start of object
        0x7B, 0x00,
        // Key: waving emoji
        0x22, 0x00, 0x3D, 0xD8, 0x4B, 0xDC, 0x22, 0x00, 0x3A, 0x00,
        // Value: world + raw line feed
        0x22, 0x00, 0x77, 0x00, 0x6F, 0x00, 0x72, 0x00, 0x6C, 0x00, 0x64, 0x00, 0x0A, 0x00,
        // Missing end of string, missing end of object
    ];

    private static readonly byte[] ExpectedTranscodingOutput =
        "{\"\uD83D\uDC4B\":\"world\\n\"}"u8.ToArray();

    private static void WithWhitespaceCombinations(string input, Action<string> test)
    {
        test(input);
        test(input + "\n");
        test("\t" + input);
        test(string.Concat("\t", input, "\n"));
    }

    /// <summary> Tests that a given input yields a given output with the given set of recoveries, no more, no less. </summary>
    private static void TestRecovery(string input, JsonRecoveryFlags recoveries, string expectedOutput)
    {
        var byteInput = Encoding.UTF8.GetBytes(input);
        Assert.Equal(expectedOutput, Encoding.UTF8.GetString(RecoverSingleCall(byteInput, recoveries, out var usedRecoveries)));
        Assert.Equal(recoveries,     usedRecoveries);

        Assert.Equal(expectedOutput, Encoding.UTF8.GetString(RecoverByteByByte(byteInput, recoveries, out usedRecoveries)));
        Assert.Equal(recoveries,     usedRecoveries);
    }

    private static byte[] RecoverSingleCall(ReadOnlySpan<byte> input, JsonRecoveryFlags allowedRecoveries, out JsonRecoveryFlags usedRecoveries)
    {
        using var memoryStream = new MemoryStream();

        var recoveryStream = new JsonRecoveryStream(allowedRecoveries, memoryStream, "[NL]", true);
        using (recoveryStream)
        {
            recoveryStream.Write(input);
        }

        usedRecoveries = recoveryStream.UsedRecoveries;

        return memoryStream.ToArray();
    }

    private static byte[] RecoverByteByByte(ReadOnlySpan<byte> input, JsonRecoveryFlags allowedRecoveries, out JsonRecoveryFlags usedRecoveries)
    {
        using var memoryStream = new MemoryStream();

        var recoveryStream = new JsonRecoveryStream(allowedRecoveries, memoryStream, "[NL]", true);
        using (recoveryStream)
        {
            foreach (var b in input)
                recoveryStream.WriteByte(b);
        }

        usedRecoveries = recoveryStream.UsedRecoveries;

        return memoryStream.ToArray();
    }
}
