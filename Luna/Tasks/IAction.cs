namespace Luna;

/// <summary> Actions that can be queued in a <see cref="SingleTaskQueue"/>. </summary>
public interface IAction : IEquatable<IAction>
{
    /// <summary> Execute the asynchronous action. </summary>
    /// <param name="token"> A cancellation token passed to the action. </param>
    public void Execute(CancellationToken token);
}
