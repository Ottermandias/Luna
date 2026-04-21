using System.Text;

namespace Luna.Tests;

public class AutoUtf8TranscodeTests
{
    // The common test data is a BOM, then "hello" in English, Russian and Japanese, and finally a waving hand emoji.
    // The goal is to test all UTF-8 cases (single, double, triple and quadruple bytes) and both UTF-16 cases (basic and surrogate pair).

    #region Common Test Data

    private static readonly byte[] Utf8Data =
    [
        // BOM
        0xEF, 0xBB, 0xBF,
        // "hello" in English
        0x68, 0x65, 0x6C, 0x6C, 0x6F,
        // Space
        0x20,
        // "hello" in Russian
        0xD0, 0xBF, 0xD1, 0x80, 0xD0, 0xB8, 0xD0, 0xB2, 0xD0, 0xB5, 0xD1, 0x82,
        // Space
        0x20,
        // "hello" in Japanese
        0xE3, 0x81, 0x93, 0xE3, 0x82, 0x93, 0xE3, 0x81, 0xAF, 0xE3, 0x81, 0xAB, 0xE3, 0x81, 0xA1,
        // Space
        0x20,
        // Waving hand emoji
        0xF0, 0x9F, 0x91, 0x8B,
    ];

    private static readonly byte[] Utf16LeData =
    [
        // BOM
        0xFF, 0xFE,
        // "hello" in English
        0x68, 0x00, 0x65, 0x00, 0x6C, 0x00, 0x6C, 0x00, 0x6F, 0x00,
        // Space
        0x20, 0x00,
        // "hello" in Russian
        0x3F, 0x04, 0x40, 0x04, 0x38, 0x04, 0x32, 0x04, 0x35, 0x04, 0x42, 0x04,
        // Space
        0x20, 0x00,
        // "hello" in Japanese
        0x53, 0x30, 0x93, 0x30, 0x6F, 0x30, 0x6B, 0x30, 0x61, 0x30,
        // Space
        0x20, 0x00,
        // Waving hand emoji
        0x3D, 0xD8, 0x4B, 0xDC,
    ];

    #endregion

    [Fact]
    public void Utf8NoBomTest()
        => TestTranscoding(Utf8Data.AsSpan(3..), Utf8Data[3..], null);

    [Fact]
    public void Utf8BomTest()
        => TestTranscoding(Utf8Data, Utf8Data[3..], Encoding.UTF8);

    [Fact]
    public void Utf16LeBomTest()
        => TestTranscoding(Utf16LeData, Utf8Data[3..], Encoding.Unicode);

    [Fact]
    public void Utf16BeBomTest()
    {
        var utf16BeData = (byte[])Utf16LeData.Clone();
        for (var i = 0; i < utf16BeData.Length; i += 2)
            (utf16BeData[i], utf16BeData[i + 1]) = (utf16BeData[i + 1], utf16BeData[i]);

        TestTranscoding(utf16BeData, Utf8Data[3..], Encoding.BigEndianUnicode);
    }

    [Theory]
    [MemberData(nameof(QuasiBomData))]
    public void QuasiBomTest(byte[] data)
        => TestTranscoding(data, data, null);

    public static readonly TheoryData<byte[]> QuasiBomData =
    [
        // Actually Arabic letters.
        [0xEF, 0xBB, 0xAF],
        [0xEF, 0xBA, 0xBF],

        // This will just be passed through.
        // The transcoding stream won't care, but callers will most likely expect the written data to be UTF-8, which this is not.
        // Don't do this in real code!
        [0xFE, 0xFE],
        [0xFF, 0xFF],
    ];

    private static void TestTranscoding(ReadOnlySpan<byte> input, byte[] expectedOutput, Encoding? expectedBomEncoding)
    {
        Assert.Equal(expectedOutput, TranscodeSingleCall(input, out var bomEncoding));
        if (expectedBomEncoding is null)
            Assert.Null(bomEncoding);
        else
            Assert.Equal(expectedBomEncoding, bomEncoding);

        Assert.Equal(expectedOutput, TranscodeByteByByte(input, out bomEncoding));
        if (expectedBomEncoding is null)
            Assert.Null(bomEncoding);
        else
            Assert.Equal(expectedBomEncoding, bomEncoding);
    }

    private static byte[] TranscodeSingleCall(ReadOnlySpan<byte> input, out Encoding? bomEncoding)
    {
        using var memoryStream = new MemoryStream();

        using (var transcodeStream = new AutoUtf8TranscodingStream(memoryStream, true))
        {
            transcodeStream.Write(input);
            bomEncoding = transcodeStream.BomEncoding;
        }

        return memoryStream.ToArray();
    }

    private static byte[] TranscodeByteByByte(ReadOnlySpan<byte> input, out Encoding? bomEncoding)
    {
        using var memoryStream = new MemoryStream();

        using (var transcodeStream = new AutoUtf8TranscodingStream(memoryStream, true))
        {
            foreach (var b in input)
                transcodeStream.WriteByte(b);
            bomEncoding = transcodeStream.BomEncoding;
        }

        return memoryStream.ToArray();
    }
}
