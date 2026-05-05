namespace Luna;

/// <summary> Helpers for modifier handling. </summary>
public sealed class ModifierHelper
{
    /// <summary> The modifiers to use for destructive actions like deletions. </summary>
    public readonly CachedModifier Destructive = new(ModifierHotkey.Control, ModifierHotkey.Shift);

    /// <summary> The modifiers to use for actions that should not happen accidentally but are not destructive. </summary>
    public readonly CachedModifier Misclick = new(ModifierHotkey.Control, ModifierHotkey.NoKey);

    /// <summary> The modifiers to use when adding things to a set (like selections) or chaining actions. </summary>
    public readonly CachedModifier Add = new(ModifierHotkey.Control, ModifierHotkey.NoKey);

    /// <summary> The modifiers to use when grouping multiple things, like a mass-selection. </summary>
    public readonly CachedModifier Group = new(ModifierHotkey.Shift, ModifierHotkey.NoKey);

    /// <summary> The modifiers to use to protect from bulk actions. </summary>
    public readonly CachedModifier Bulk = new(ModifierHotkey.Shift, ModifierHotkey.NoKey);

    /// <summary> Modifier data stored and updated on setting the modifier or per frame. </summary>
    public sealed class CachedModifier : IUtf8SpanFormattable, ISpanFormattable
    {
        /// <summary> The default preamble for tooltips. </summary>
        private StringU8 _defaultTooltip = StringU8.Empty;

        /// <summary> The name of the modifiers. </summary>
        public StringPair Name { get; private set; } = StringPair.Empty;

        /// <summary> The modifiers set. </summary>
        public DoubleModifier Modifier { get; private set; } = DoubleModifier.NoKey;

        /// <summary> Whether the modifiers are active in this frame or not. Updated per frame. </summary>
        public bool Active { get; internal set; }

        /// <summary> Create a new cached modifier base data. </summary>
        public CachedModifier(ModifierHotkey hotkey1, ModifierHotkey hotkey2)
            => Set(new DoubleModifier(hotkey1, hotkey2));

        /// <summary> Set the modifier to your desired values. </summary>
        public void Set(DoubleModifier modifier)
        {
            Modifier        = modifier;
            Name            = new StringPair($"{modifier}");
            _defaultTooltip = new StringU8($"Hold {Name} while clicking to ");
            Active          = false;
        }

        /// <summary> Draw a default tooltip only if the modifier is not currently active when the last item was hovered. </summary>
        /// <param name="action"> The action suffix to write. </param>
        /// <remarks> The tooltip will have the form <c>'Hold {Name} while clicking to {action}.'</c> </remarks>
        public void Tooltip(ReadOnlySpan<byte> action)
        {
            if (!Active)
                Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled, $"{_defaultTooltip} {action}.");
        }

        /// <inheritdoc cref="Tooltip(ReadOnlySpan{byte})"/>
        public void Tooltip(ReadOnlySpan<char> action)
        {
            if (!Active)
                Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled, $"{_defaultTooltip} {action}.");
        }

        /// <summary> Draw a default tooltip with two line breaks at the start only if the modifier is not currently active when the last item was hovered. </summary>
        /// <param name="action"> The action suffix to write. </param>
        /// <remarks> The tooltip will have the form <c>'\n\nHold {Name} while clicking to {action}.'</c> </remarks>
        public void TooltipLineBreak(ReadOnlySpan<byte> action)
        {
            if (!Active)
                Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled, $"\n\n{_defaultTooltip} {action}.");
        }

        /// <inheritdoc cref="TooltipLineBreak(ReadOnlySpan{byte})"/>
        public void TooltipLineBreak(ReadOnlySpan<char> action)
        {
            if (!Active)
                Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled, $"\n\n{_defaultTooltip} {action}.");
        }

        /// <summary> Implicit conversion to bool. </summary>
        public static implicit operator bool(CachedModifier modifier)
            => modifier.Active;

        /// <inheritdoc/>
        public string ToString(string? format, IFormatProvider? formatProvider)
            => Name.ToString(format, formatProvider);

        /// <inheritdoc/>
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
            => Name.TryFormat(destination, out charsWritten, format, provider);

        /// <inheritdoc/>
        public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
            => Name.TryFormat(destination, out bytesWritten, format, provider);

        /// <inheritdoc/>
        public override string ToString()
            => Name.Utf16;
    }

    private void OnUpdate()
    {
        Destructive.Active = Destructive.Modifier.IsActive();
        Misclick.Active    = Misclick.Modifier.IsActive();
        Add.Active         = Add.Modifier.IsActive();
        Group.Active       = Group.Modifier.IsActive();
        Bulk.Active        = Bulk.Modifier.IsActive();
    }

    /// <summary> Subscribe to per-frame updates for all modifiers. </summary>
    public ModifierHelper()
        => ImSharpPerFrame.Update += OnUpdate;

    /// <summary> Unsubscribe from per-frame updates. </summary>
    ~ModifierHelper()
        => ImSharpPerFrame.Update -= OnUpdate;
}
