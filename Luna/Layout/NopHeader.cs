namespace Luna;

public sealed class NopHeader : IHeader
{
    public bool Collapsed
        => true;

    public void Draw(Vector2 size)
    { }

    public static readonly NopHeader Instance = new();
}
