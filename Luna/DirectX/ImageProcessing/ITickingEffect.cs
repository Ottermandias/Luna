namespace Luna.DirectX;

/// <summary> An image processing effect that runs a slice of work each frame, and the list of its outputs. </summary>
public interface ITickingEffect : IEffect
{
    /// <summary> Runs a slice of work. </summary>
    /// <remarks> This will be called each frame. </remarks>
    public void Tick();
}
