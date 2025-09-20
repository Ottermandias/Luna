namespace Luna;

public sealed class FileSystemSelectorHeader<TCacheItem> : IHeader
{
    public StringU8            Label  { get; init; } = new("Filter..."u8);
    public IFilter<TCacheItem> Filter { get; init; } = NopFilter<TCacheItem>.Instance;

    public bool Collapsed
        => Filter == NopFilter<TCacheItem>.Instance;

    public void Draw(Vector2 size)
        => Filter.DrawFilter(Label, size);
}
