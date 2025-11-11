namespace Luna;

/// <summary> A basic header that draws a single filter. </summary>
/// <typeparam name="TCacheItem"> The type of the items the filter is for. </typeparam>
/// <param name="filter"> The filter to draw. </param>
public class FilterHeader<TCacheItem>(IFilter<TCacheItem> filter, StringU8? id) : IHeader, IConstructedService
{
    /// <summary> The filter drawn. </summary>
    public readonly IFilter<TCacheItem> Filter = filter;

    /// <summary> The ID to pass to the filter. </summary>
    public readonly StringU8 Id = id ?? StringU8.Empty;

    /// <inheritdoc/>
    public bool Collapsed { get; } = filter is not NopFilter<TCacheItem>;

    /// <inheritdoc/>
    public void Draw(Vector2 size)
        => Filter.DrawFilter(Id, size);
}
