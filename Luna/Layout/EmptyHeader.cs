namespace Luna;

public sealed class EmptyHeader : IHeader
{
    public bool Collapsed
        => false;

    public void Draw(Vector2 size)
    {
        ImEx.TextFramed("Empty"u8, size);
    }

    public static readonly EmptyHeader Instance = new();
}
