namespace Luna;

public interface IHeader : IUiService
{
    public bool Collapsed { get; }
    public void Draw(Vector2 size);
}
