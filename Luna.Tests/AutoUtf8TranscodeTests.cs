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
    {
        var expectedResult = Utf8Data[3..];
        Assert.Equal(expectedResult, TranscodeSingleCall(Utf8Data.AsSpan(3..), out var bomEncoding));
        Assert.Null(bomEncoding);

        Assert.Equal(expectedResult, TranscodeByteByByte(Utf8Data.AsSpan(3..), out bomEncoding));
        Assert.Null(bomEncoding);
    }

    [Fact]
    public void Utf8BomTest()
    {
        var expectedResult = Utf8Data[3..];
        Assert.Equal(expectedResult, TranscodeSingleCall(Utf8Data, out var bomEncoding));
        Assert.Equal(Encoding.UTF8,  bomEncoding);

        Assert.Equal(expectedResult, TranscodeByteByByte(Utf8Data, out bomEncoding));
        Assert.Equal(Encoding.UTF8,  bomEncoding);
    }

    [Fact]
    public void Utf16LeBomTest()
    {
        var expectedResult = Utf8Data[3..];
        Assert.Equal(expectedResult,   TranscodeSingleCall(Utf16LeData, out var bomEncoding));
        Assert.Equal(Encoding.Unicode, bomEncoding);

        Assert.Equal(expectedResult,   TranscodeByteByByte(Utf16LeData, out bomEncoding));
        Assert.Equal(Encoding.Unicode, bomEncoding);
    }

    [Fact]
    public void Utf16BeBomTest()
    {
        var expectedResult = Utf8Data[3..];
        var inputData      = (byte[])Utf16LeData.Clone();
        for (var i = 0; i < inputData.Length; i += 2)
            (inputData[i], inputData[i + 1]) = (inputData[i + 1], inputData[i]);

        Assert.Equal(expectedResult,            TranscodeSingleCall(inputData, out var bomEncoding));
        Assert.Equal(Encoding.BigEndianUnicode, bomEncoding);

        Assert.Equal(expectedResult,            TranscodeByteByByte(inputData, out bomEncoding));
        Assert.Equal(Encoding.BigEndianUnicode, bomEncoding);
    }

    [Theory]
    [MemberData(nameof(QuasiBomTestData))]
    public void QuasiBomTest(byte[] data)
    {
        Assert.Equal(data, TranscodeSingleCall(data, out var bomEncoding));
        Assert.Null(bomEncoding);

        Assert.Equal(data, TranscodeByteByByte(data, out bomEncoding));
        Assert.Null(bomEncoding);
    }

    public static readonly TheoryData<byte[]> QuasiBomTestData =
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
