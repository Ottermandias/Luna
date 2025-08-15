namespace Luna;

/// <summary> A utility to track the initialization time of services or objects. </summary>
public class StartTimeTracker : IService
{
    /// <summary> The internally used timer. </summary>
    private class TimerTuple : Stopwatch
    {
        public TimeSpan StartDelay;
        public int      Thread;
    }

    /// <summary> The time when this service is constructed as a base time for the construction of other services. </summary>
    private readonly DateTime _constructionTime = DateTime.UtcNow;

    /// <summary> A list of all tracked services by name. </summary>
    private readonly ConcurrentDictionary<string, TimerTuple> _timers = [];

    /// <summary> Measure the time until the returned object is disposed and store its data. </summary>
    /// <param name="name"> The name to store the data under. </param>
    /// <returns> A disposable that stops the timer on disposal. Preferably use with using. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public TimingStopper Measure(string name)
        => new(this, name);

    /// <summary> Measure the time of a specific action and store its data. </summary>
    /// <param name="name"> The name to store the data under. </param>
    /// <param name="action"> The action to measure. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Measure(string name, Action action)
    {
        using var t = Measure(name);
        action();
    }

    /// <summary> Measure the time of a specific function, store its data and return its return value. </summary>
    /// <typeparam name="TRet"> The type of the return value. </typeparam>
    /// <param name="name"> The name to store the data under. </param>
    /// <param name="func"> The function to measure. </param>
    /// <returns> The return value of the function. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public TRet Measure<TRet>(string name, Func<TRet> func)
    {
        using var t = Measure(name);
        return func();
    }

    /// <summary> Start measuring time under a specific name. </summary>
    /// <param name="name"> The name to store the data under. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Start(string name)
    {
        var tuple = Get(name);
        tuple.Start();
        tuple.StartDelay = DateTime.UtcNow - _constructionTime;
        tuple.Thread     = Environment.CurrentManagedThreadId;
    }

    /// <summary> Stop measuring time under a specific name. </summary>
    /// <param name="name"> The name to finish the measurement under. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Stop(string name)
        => Get(name).Stop();

    public static void Draw(Utf8LabelHandler id)
    {
        using var _     = Im.Id.Push(ref id);
        var       cache = CacheManager.Instance.GetOrCreateCache<StartTimeTrackerViewModel>(Im.Id.Current);
        cache.Draw();
    }

    /// <summary> Get or create the timer for the given name. </summary>
    private TimerTuple Get(string name)
    {
        if (!_timers.TryGetValue(name, out var tuple))
        {
            tuple = new TimerTuple();
            _timers.TryAdd(name, tuple);
        }

        return tuple;
    }

    private sealed class StartTimeTrackerViewModel(StartTimeTracker tracker) : BasicCache
    {
        private record struct Column(StringU8 Name, SizedString Time, SizedString Start, SizedString End, SizedString Thread)
        {
            public static Column Create(KeyValuePair<string, TimerTuple> kvp)
            {
                var name      = new StringU8(kvp.Key);
                var duration  = kvp.Value.ElapsedTicks * 1000.0 / Stopwatch.Frequency;
                var startTime = kvp.Value.StartDelay.TotalMilliseconds;
                var time      = new SizedString($"{duration:F4}");
                var start     = new SizedString($"{startTime:F4}");
                var end       = new SizedString($"{duration + startTime:F4}");
                var thread    = new SizedString($"{kvp.Value.Thread}");
                return new Column(name, time, start, end, thread);
            }
        }

        private          UserRegex    _filter = UserRegex.Empty;
        private          float        _threadColumnWidth;
        private          float        _columnWidth;
        private readonly List<Column> _data = [];

        public override void ApplyStoredData(object existingData)
        {
            if (existingData is UserRegex filter)
                _filter = filter;
        }

        public override object SaveStoredData()
            => _filter;

        public override void Update()
        {
            if (FontDirty)
            {
                _threadColumnWidth = 50 * Im.Style.GlobalScale;
                _columnWidth       = 150 * Im.Style.GlobalScale;
                UpdateFilter();
            }
            else if (CustomDirty)
            {
                UpdateFilter();
            }

            Dirty = IManagedCache.DirtyFlags.Clean;
        }

        private void UpdateFilter()
        {
            _data.Clear();
            _data.AddRange(tracker._timers.Where(t => _filter.Match(t.Key)).OrderBy(t => t.Value.StartDelay)
                .Select(Column.Create));
        }

        public void Draw()
        {
            if (UserRegex.DrawRegexInput("##filter"u8, ref _filter, "Filter..."u8, null, Im.ContentRegion.Available.X,
                    LunaStyle.ErrorBorderColor))
                UpdateFilter();
            using var table = Im.Table.Begin("t"u8, 5, TableFlags.SizingFixedFit | TableFlags.RowBackground);
            if (!table)
                return;

            table.SetupColumn("Name"u8,   TableColumnFlags.None, _columnWidth);
            table.SetupColumn("Time"u8,   TableColumnFlags.None, _columnWidth);
            table.SetupColumn("Start"u8,  TableColumnFlags.None, _columnWidth);
            table.SetupColumn("End"u8,    TableColumnFlags.None, _columnWidth);
            table.SetupColumn("Thread"u8, TableColumnFlags.None, _threadColumnWidth);
            table.HeaderRow();

            foreach (var column in _data)
            {
                table.DrawColumn(column.Name);
                table.NextColumn();
                ImEx.TextRightAligned(column.Time);
                table.NextColumn();
                ImEx.TextRightAligned(column.Start);
                table.NextColumn();
                ImEx.TextRightAligned(column.End);
                table.NextColumn();
                ImEx.TextRightAligned(column.Thread);
            }
        }
    }

    /// <summary> The utility disposable that starts the timer on construction and stops it on disposal. </summary>
    public readonly ref struct TimingStopper : IDisposable
    {
        private readonly TimerTuple _watch;

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal TimingStopper(StartTimeTracker manager, string name)
        {
            // Get or create the correct object.
            var tuple = manager.Get(name);
            _watch = tuple;

            // Start the timer, set the other values.
            _watch.Start();
            tuple.StartDelay = DateTime.UtcNow - manager._constructionTime;
            tuple.Thread     = Environment.CurrentManagedThreadId;
        }

        /// <summary> Stop the timer on disposal. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void Dispose()
            => _watch.Stop();
    }
}
