using Dalamud.Plugin.Services;

namespace Luna;

/// <summary> Manage certain actions to only occur on framework updates. </summary>
public sealed class FrameworkManager : IDisposable, IService
{
    /// <summary> The game's framework service. </summary>
    public readonly IFramework Framework;

    /// <summary> The logger to use. </summary>
    private readonly Logger _log;

    /// <summary> All actions queued as Important are invoked together on separate awaited threads every frame. </summary>
    private readonly Dictionary<string, Action> _important = [];

    /// <summary> At most one single action queued as OnTick is invoked per frame. </summary>
    private readonly Dictionary<string, Action> _onTick = [];

    /// <summary> The first action queued as Delayed whose delay has passed is invoked per frame. </summary>
    private readonly LinkedList<(DateTime, string, Action)> _delayed = [];

    /// <summary> Create a framework manager and subscribe to the update event. </summary>
    /// <param name="framework"> The game's framework service. </param>
    /// <param name="log"> The logger to use. </param>
    public FrameworkManager(IFramework framework, Logger log)
    {
        Framework        =  framework;
        _log             =  log;
        Framework.Update += OnUpdate;
    }


    /// <summary> Get a list of the tags of all currently queued Important actions. </summary>
    public List<string> Important
    {
        get
        {
            lock (_important)
            {
                return _important.Keys.ToList();
            }
        }
    }

    /// <summary> Get a list of the tags of all currently queued OnTick actions. </summary>
    public List<string> OnTick
    {
        get
        {
            lock (_onTick)
            {
                return _onTick.Keys.ToList();
            }
        }
    }

    /// <summary> Get a list of the tags and trigger times of all currently queued Delayed actions. </summary>
    public List<(DateTime, string)> Delayed
    {
        get
        {
            lock (_delayed)
            {
                return _delayed.Select(t => (t.Item1, t.Item2)).ToList();
            }
        }
    }

    /// <summary>
    /// Register an action that is not time-critical.
    /// One action per frame will be executed.
    /// On dispose, any remaining actions will be executed.
    /// </summary>
    public void RegisterOnTick(string tag, Action action)
    {
        lock (_onTick)
        {
            _onTick[tag] = action;
        }
    }

    /// <summary>
    /// Register an action that should be executed on the next frame.
    /// All of those actions will be executed in the next frame.
    /// If there are more than one, they will be launched in separated tasks, but waited for.
    /// </summary>
    public void RegisterImportant(string tag, Action action)
    {
        lock (_important)
        {
            _important[tag] = action;
        }
    }


    /// <summary>
    /// Register an action that is expected to be delayed.
    /// One action per frame will be executed when the delay has been waited for.
    /// On dispose, any remaining actions will be executed.
    /// If the action is already registered and the desired time is earlier, it will be updated,
    /// if it is later, it will be ignored.
    /// </summary>
    public void RegisterDelayed(string tag, Action action, TimeSpan delay)
    {
        var desiredTime = DateTime.UtcNow + delay;
        lock (_delayed)
        {
            var node = _delayed.First;
            if (node == null)
            {
                _delayed.AddFirst((desiredTime, tag, action));
                return;
            }

            LinkedListNode<(DateTime, string, Action)>? delete    = null;
            LinkedListNode<(DateTime, string, Action)>? addBefore = null;
            while (node != null)
            {
                if (delete == null && node.Value.Item2 == tag)
                {
                    if (node.Value.Item1 < desiredTime)
                        return;

                    delete = node;
                }

                if (addBefore == null && node.Value.Item1 > desiredTime)
                    addBefore = node;
                node = node.Next;
            }

            if (addBefore != null)
                _delayed.AddBefore(addBefore, (desiredTime, tag, action));
            else
                _delayed.AddLast((desiredTime, tag, action));

            if (delete != null)
                _delayed.Remove(delete);
        }
    }

    /// <summary> Unsubscribe from the update event and execute all remaining actions. </summary>
    public void Dispose()
    {
        Framework.Update -= OnUpdate;

        lock (_important)
        {
            foreach (var (_, action) in _important)
                action();
            _important.Clear();
        }

        lock (_onTick)
        {
            foreach (var (_, action) in _onTick)
                action();

            _onTick.Clear();
        }

        lock (_delayed)
        {
            foreach (var (_, _, action) in _delayed)
                action();
            _delayed.Clear();
        }
    }

    /// <summary> Invoke delayed actions on update. </summary>
    private void OnUpdate(IFramework _)
    {
        try
        {
            HandleOnTick(_onTick);
            HandleDelayed();
            HandleAllTasks();
        }
        catch (Exception e)
        {
            _log.Error($"Error executing actions data:\n{e}");
        }
    }

    private void HandleOnTick(Dictionary<string, Action> dict)
    {
        Action action;
        string key;
        lock (dict)
        {
            if (dict.Count == 0)
                return;

            (key, action) = dict.First();
            dict.Remove(key);
        }

        try
        {
            action();
        }
        catch (Exception ex)
        {
            _log.Error($"Error executing {key} on tick:\n{ex}");
        }
    }

    private void HandleDelayed()
    {
        var                                         now = DateTime.UtcNow;
        LinkedListNode<(DateTime, string, Action)>? node;
        lock (_delayed)
        {
            if (_delayed.Count == 0)
                return;

            node = _delayed.First;
            if (node != null && node.Value.Item1 < now)
                _delayed.RemoveFirst();
            else
                node = null;
        }

        if (node is null)
            return;

        try
        {
            node.Value.Item3.Invoke();
        }
        catch (Exception ex)
        {
            _log.Error($"Error executing {node.Value.Item2} after delay:\n{ex}");
        }
    }


    private void HandleAllTasks()
    {
        // ReSharper disable InconsistentlySynchronizedField
        if (_important.Count < 2)
        {
            HandleOnTick(_important);
            // ReSharper restore InconsistentlySynchronizedField
        }
        else
        {
            Task[] tasks;
            lock (_important)
            {
                tasks = _important.Values.Select(Task.Run).ToArray();
                _important.Clear();
            }

            Task.WaitAll(tasks);
        }
    }
}
