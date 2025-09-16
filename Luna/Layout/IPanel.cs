namespace Luna;

public interface IPanel : IUiService
{
    public ReadOnlySpan<byte> Id { get; }

    public void Draw();
}
