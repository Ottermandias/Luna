namespace Luna;

/// <summary> A class that fires events whenever the current day changes in local time. </summary>
/// <remarks> This does not react to manual changes in the local system time or anything like that. </remarks>
public static class DayChangeTracker
{
    private static readonly System.Timers.Timer Timer = new(GetSleepTime());

    /// <summary> An event fired when the current day changes in local time. Parameters are Day of the Month, Month and Year. </summary>
    public static event Action<int, int, int>? DayChanged;

    /// <summary> Static initialization that fetches the current day and sets up the timer for the next day. </summary>
    static DayChangeTracker()
    {
        Timer.Elapsed += (s, e) =>
        {
            var now = DateTime.Now;
            DayChanged?.Invoke(now.Day, now.Month, now.Year);
            Timer.Interval = GetSleepTime();
        };
        Timer.Start();
    }

    /// <summary> Fetch the milliseconds to sleep until the next day. </summary>
    private static double GetSleepTime()
    {
        var midnightTonight          = DateTime.Today.AddDays(1);
        var differenceInMilliseconds = (midnightTonight - DateTime.Now).TotalMilliseconds;
        return differenceInMilliseconds;
    }

    /// <summary> Currently unused. </summary>
    private static void OnSystemTimeChanged(object sender, EventArgs e)
        => Timer.Interval = GetSleepTime();
}
