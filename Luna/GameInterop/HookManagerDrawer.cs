using Luna.Generators;

namespace Luna;

public sealed class HookManagerDrawer(HookManager manager, DynamisIpc dynamis) : IUiService
{
    private readonly HookFilter _filter = new();

    public void Draw()
    {
        _filter.DrawFilter("Filter Hooks..."u8, Im.ContentRegion.Width());
        var       cache = CacheManager.GetOrCreateGlobalCache(Im.Id.Current, () => new Cache(_filter, manager));
        using var table = Im.Table.Begin("##hmt"u8, 5, TableFlags.RowBackground);
        if (!table)
            return;

        table.SetupColumn("Name"u8,      TableColumnFlags.WidthFixed, 250 * Im.Style.GlobalScale);
        table.SetupColumn("Address"u8,   TableColumnFlags.WidthFixed, Im.Font.Mono.GetCharacterAdvance(' ') * 14);
        table.SetupColumn("State"u8,     TableColumnFlags.WidthFixed, 75 * Im.Style.GlobalScale);
        table.SetupColumn("Time"u8,      TableColumnFlags.WidthFixed, 50 * Im.Style.GlobalScale);
        table.SetupColumn("Signature"u8, TableColumnFlags.WidthStretch);

        table.HeaderRow();
        using var clipper = new Im.ListClipper(cache.Count, Im.Style.FrameHeightWithSpacing);
        using var id      = Im.Id.Empty();
        foreach (var hook in clipper.Iterate(cache))
        {
            id.PushNext();
            table.DrawFrameColumn(hook.Name.Utf8);
            table.NextColumn();
            if (hook.Hook.Hook?.Address is { } address)
            {
                dynamis.DrawPointer(address);
                table.NextColumn();
                if (ImEx.Button(hook.State.ToNameU8(), Im.ContentRegion.Width(),
                        hook.State is not HookState.Enabled and not HookState.Disabled))
                    if (hook.State is HookState.Enabled)
                        hook.Hook.Disable();
                    else
                        hook.Hook.Enable();
            }
            else
            {
                ImEx.TextFrameAligned("Error"u8);
                Im.Tooltip.OnHover(hook.Hook.Error?.ToString() ?? "Unknown Error");
                table.NextColumn();
            }

            table.DrawFrameColumn(hook.Time);
            table.DrawFrameColumn(hook.Signature.Utf8);
            id.Pop();
        }
    }

    private readonly struct CachedHook(IHookData hook)
    {
        public readonly StringPair Name      = new(hook.Name);
        public readonly StringPair Signature = new(hook.Signature);
        public readonly StringU8   Time      = new($"{hook.Timestamp} ms");
        public readonly IHookData  Hook      = hook;

        public readonly HookState State = hook.Hook is null
            ? HookState.Unavailable
            : hook.Hook.IsDisposed
                ? HookState.Disposed
                : hook.Hook.IsEnabled
                    ? HookState.Enabled
                    : HookState.Disabled;
    }

    [NamedEnum(Utf16: false)]
    public enum HookState
    {
        [Name("Disable")]
        Enabled,

        [Name("Enable")]
        Disabled,

        [Name("Disposed")]
        Disposed,

        [Name("Unavailable")]
        Unavailable,
    }

    private sealed class HookFilter : RegexFilterBase<CachedHook>
    {
        protected override string ToFilterString(in CachedHook item, int globalIndex)
            => item.Name.Utf16;
    }

    private sealed class Cache : BasicFilterCache<CachedHook>
    {
        private readonly HookManager _manager;

        public Cache(HookFilter filter, HookManager manager)
            : base(filter)
        {
            _manager                  =  manager;
            _manager.HookStateChanged += OnHookStateChanged;
        }

        private void OnHookStateChanged(string obj)
            => Dirty |= IManagedCache.DirtyFlags.Custom;

        protected override IEnumerable<CachedHook> GetItems()
            => _manager.Hooks.OrderBy(p => p.Name).Select(p => new CachedHook(p));

        protected override void Dispose(bool disposing)
            => _manager.HookStateChanged -= OnHookStateChanged;
    }
}
