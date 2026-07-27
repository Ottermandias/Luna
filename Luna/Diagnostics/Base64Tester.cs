namespace Luna;

public static class Base64Tester
{
    /// <summary> Draw a widget with an import button that shows the imported data in all decoding states. </summary>
    public static void Draw()
    {
        var cache = CacheManager.GetOrCreateGlobalCache(Im.Id.Current, () => new Cache());
        if (Im.Button("Import"u8))
        {
            cache.Base64Data = StringU8.Null;
            cache.ZippedData = Memory<byte>.Empty;
            cache.RawData    = Memory<byte>.Empty;
            cache.Text       = StringU8.Null;
            cache.Exception  = null;
            cache.Version    = 0;
            try
            {
                cache.Base64Data = Im.Clipboard.GetCopy();
                var bytes = new byte[System.Buffers.Text.Base64.GetMaxDecodedFromUtf8Length(cache.Base64Data.Length) + 1];
                System.Buffers.Text.Base64.DecodeFromUtf8(cache.Base64Data, bytes, out _, out var written);
                bytes[written]   = 0;
                cache.ZippedData = bytes.AsMemory(0, written);
                using var compressedStream = new MemoryStream(bytes, 0, written);
                using var zipStream        = new GZipStream(compressedStream, CompressionMode.Decompress);
                using var resultStream     = new MemoryStream();
                zipStream.CopyTo(resultStream);
                cache.RawData = resultStream.ToArray();
                cache.Version = cache.RawData.Span[0];
                cache.RawData = cache.RawData[1..];
                var encoding = new UTF8Encoding(false, true);
                try
                {
                    encoding.GetCharCount(cache.RawData.Span);
                    cache.Text = new StringU8(cache.RawData, false);
                }
                catch
                {
                    // ignored, the data does not have to be valid UTF8.
                }
            }
            catch (Exception ex)
            {
                cache.Exception = ex;
            }
        }

        if (!cache.Base64Data.IsNull)
        {
            using var mono = Im.Font.PushMono();
            for (var i = 0; i < cache.Base64Data.Length / 128; ++i)
            {
                var lower = i * 128;
                var upper = Math.Min(lower + 128, cache.Base64Data.Length);
                Im.Text(cache.Base64Data.Span[lower..upper]);
            }

            if (!cache.ZippedData.IsEmpty)
            {
                LunaStyle.DrawSeparator();
                ImEx.HexViewer(cache.ZippedData.Span);
                if (!cache.RawData.IsEmpty)
                {
                    LunaStyle.DrawSeparator();
                    Im.Text($"Version: {cache.Version}");
                    ImEx.HexViewer(cache.RawData.Span);
                }

                if (!cache.Text.IsNull)
                {
                    LunaStyle.DrawSeparator();
                    Im.Text(cache.Text);
                }
            }
        }

        if (cache.Exception is not null)
        {
            using var style = ImGuiColor.Text.Push(DalamudColor.ErrorForeground.Value);
            Im.TextWrapped($"{cache.Exception}");
        }
    }

    private sealed class Cache : BasicCache
    {
        public StringU8     Base64Data = StringU8.Empty;
        public Memory<byte> ZippedData;
        public Memory<byte> RawData;
        public StringU8     Text;
        public byte         Version;
        public Exception?   Exception;

        public override void Update()
        { }
    }
}
