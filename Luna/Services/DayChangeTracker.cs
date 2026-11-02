namespace Luna;

/// <summary> A class that fires events whenever the current day changes in local time. </summary>
public interface IDayChangeTracker
{
    /// <summary> An event that is invoked with the day of the month, the month and the year whenever the current date changes. </summary>
    public event Action<int, int, int>? DayChanged;
}

/// <summary> A class that fires events whenever the current day changes in local time according to the time provider. </summary>
/// <remarks> Through regular polling, this should correctly handle manual changes in time or DST changes. </remarks>
public sealed class DayChangeTracker : IDayChangeTracker, IDisposable
{
    /// <inheritdoc/>
    public event Action<int, int, int>? DayChanged;

    /// <summary> Create a new day change tracker using the default system time provider. </summary>
    public DayChangeTracker(LunaLogger? log = null)
        : this(TimeProvider.System, log)
    { }

    private readonly Lock         _sync = new();
    private readonly LunaLogger?  _log;
    private readonly TimeProvider _provider;
    private          ITimer?      _timer;
    private          DateOnly     _lastDate;

    /// <summary> Create a new day change tracker using a specified time provider. </summary>
    /// <param name="provider"> The time provider to use. </param>
    /// <param name="log"> An optional logger for exceptions during invocation and logging day changes. </param>
    public DayChangeTracker(TimeProvider provider, LunaLogger? log = null)
    {
        _log      = log;
        _provider = provider;
        _lastDate = DateOnly.FromDateTime(provider.GetLocalNow().DateTime);
        _timer    = provider.CreateTimer(Reschedule, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        Reschedule(null);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_sync)
        {
            _timer?.Dispose();
            _timer = null;
        }
    }

    private void Reschedule(object? _)
    {
        DateOnly today;
        bool     changed;
        lock (_sync)
        {
            if (_timer is null)
                return;

            var now = _provider.GetLocalNow().DateTime;
            today     = DateOnly.FromDateTime(now);
            changed   = today != _lastDate;
            _lastDate = today;
            var untilMidnight = now.Date.AddDays(1) - now;
            var minute        = TimeSpan.FromMinutes(1);
            var delay         = untilMidnight < minute ? untilMidnight > TimeSpan.Zero ? untilMidnight : TimeSpan.FromMilliseconds(1) : minute;
            _timer.Change(delay, Timeout.InfiniteTimeSpan);
        }

        if (!changed)
            return;

        _log?.Debug($"The date changed to {today:yyyy/MM/dd}.");
        foreach (var subscriber in Delegate.EnumerateInvocationList(DayChanged))
        {
            try
            {
                subscriber.Invoke(today.Day, today.Month, today.Year);
            }
            catch (Exception ex)
            {
                _log?.Error($"Exception thrown by subscriber during day change:\n{ex}");
            }
        }
    }
}
