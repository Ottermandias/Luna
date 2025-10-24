namespace Luna;

/// <summary> A queue that ensures only a single task is running at any given time and queues them up. </summary>
public class SingleTaskQueue
{
    /// <summary> A weak reference to the last task started. </summary>
    private readonly WeakReference<Task> _lastTask = new(null!);

    /// <summary> Enqueue an action to be executed when all previously enqueued actions have completed. </summary>
    /// <param name="action"> The action to execute. </param>
    /// <param name="token"> The cancellation token passed to the action. </param>
    /// <returns> An awaitable task. </returns>
    public Task Enqueue(IAction action, CancellationToken token = default)
    {
        lock (this)
        {
            // If there is a last task, and it is not completed, continue with the new action after it.
            // Otherwise, start a new task immediately.
            var resultTask = _lastTask.TryGetTarget(out var lastTask) && !lastTask.IsCompleted
                ? lastTask.ContinueWith(_ => action.Execute(token), token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current)
                : Task.Run(() => action.Execute(token), token);

            // Update the weak reference.
            _lastTask.SetTarget(resultTask);

            return resultTask;
        }
    }
}
