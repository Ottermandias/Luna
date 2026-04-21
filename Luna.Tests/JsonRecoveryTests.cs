using System.Text;

namespace Luna.Tests;

public class JsonRecoveryTests
{
    [Theory]
    [MemberData(nameof(ValidJsonPassthroughTestData))]
    public void ValidJsonPassthroughTest(string data)
    {
        TestRecovery(data, 0, data);

        var spacedData = data + "\n";
        TestRecovery(spacedData, 0, spacedData);

        spacedData = "\t" + data;
        TestRecovery(spacedData, 0, spacedData);

        spacedData = string.Concat("\t", data, "\n");
        TestRecovery(spacedData, 0, spacedData);
    }

    public static readonly TheoryData<string> ValidJsonPassthroughTestData =
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

        var recoveryStream = new JsonRecoveryStream(allowedRecoveries, memoryStream, true);
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

        var recoveryStream = new JsonRecoveryStream(allowedRecoveries, memoryStream, true);
        using (recoveryStream)
        {
            foreach (var b in input)
                recoveryStream.WriteByte(b);
        }

        usedRecoveries = recoveryStream.UsedRecoveries;

        return memoryStream.ToArray();
    }
}
