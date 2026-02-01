namespace Luna;

/// <summary> The choices of displaying the changelog. </summary>
public enum ChangeLogDisplayType
{
    /// <summary> Display a popup window whenever a new changelog is available. </summary>
    New,

    /// <summary> Only display popup windows for changelogs that contain notes marked as important. </summary>
    HighlightOnly,

    /// <summary> Never display any changelog popups unless manually triggered. </summary>
    Never,
}

/// <summary> A changelog window and manager. </summary>
public sealed class Changelog : Window
{
    /// <summary> Fresh installs should not show changelogs. </summary>
    public const int FreshInstallVersion = int.MaxValue;

    /// <summary> The default color used for version headers in the changelog. </summary>
    public const uint DefaultHeaderColor = 0xFF60D0D0;

    /// <summary> The default color used for notes that are marked as important. </summary>
    public const uint DefaultImportantColor = 0xFF6060FF;

    /// <summary> The default color used for notes that are highlighted. </summary>
    public const uint DefaultHighlightColor = 0xFFFF9090;


    /// <summary> Get the label for the changelog setting. </summary>
    public static ReadOnlySpan<byte> ToName(ChangeLogDisplayType type)
    {
        return type switch
        {
            ChangeLogDisplayType.New           => "Show New Changelogs (Recommended)"u8,
            ChangeLogDisplayType.HighlightOnly => "Only Show Important Changelogs"u8,
            ChangeLogDisplayType.Never         => "Never Show Changelogs (Dangerous)"u8,
            _                                  => ""u8,
        };
    }

    /// <summary> The function invoked to obtain the configuration for the changelog handler. </summary>
    private readonly Func<(int, ChangeLogDisplayType)> _getConfig;

    /// <summary> The function invoked when the changelog handler accepts changes to update data. </summary>
    private readonly Action<int, ChangeLogDisplayType> _setConfig;

    private readonly List<(StringU8 Title, List<Entry> Entries, bool IsImportant)> _entries = [];

    /// <summary> The last seen version, fetched via <see cref="_getConfig"/>, and kept up to date via <see cref="_setConfig"/>. </summary>
    private int _lastVersion;

    /// <summary> The configuration for when to display changelog popups, fetched via <see cref="_getConfig"/>, and kept up to date via <see cref="_setConfig"/>. </summary>
    private ChangeLogDisplayType _displayType;

    /// <summary> The name for the changelog window. </summary>
    private readonly StringU8 _headerName;

    /// <summary> The color used for version headers in the changelog. </summary>
    public Rgba32 HeaderColor { get; set; } = DefaultHeaderColor;

    /// <summary> Value that can be set to open the changelog regardless of settings./// </summary>
    public bool ForceOpen { get; set; }

    /// <summary> Create the changelog window. </summary>
    /// <param name="label"> The label for the windowing system. The part up to the first whitespace is used for the window header, too. </param>
    /// <param name="getConfig"> The function to get the configuration for changelogs from the application. </param>
    /// <param name="setConfig"> The function to set the configuration for changelogs for the application. </param>
    public Changelog(string label, Func<(int, ChangeLogDisplayType)> getConfig, Action<int, ChangeLogDisplayType> setConfig)
        : base(label, WindowFlags.NoCollapse | WindowFlags.NoResize, true)
    {
        _headerName         = new StringU8(label.Split().FirstOrDefault(string.Empty));
        _getConfig          = getConfig;
        _setConfig          = setConfig;
        Position            = null;
        RespectCloseHotkey  = false;
        ShowCloseButton     = false;
        DisableWindowSounds = true;
    }

    /// <inheritdoc/>
    public override void PreOpenCheck()
    {
        // Update the configuration.
        (_lastVersion, _displayType) = _getConfig();

        // Respect the ForceOpen state.
        if (ForceOpen)
        {
            IsOpen = true;
            return;
        }

        // Skip changelogs for fresh installs and set the config to the current number of changelogs.
        if (_lastVersion == FreshInstallVersion)
        {
            IsOpen = false;
            _setConfig(_entries.Count, _displayType);
            return;
        }

        switch (_displayType)
        {
            case ChangeLogDisplayType.New:
                // For new, open if the last seen version is less than the number of total entries.
                IsOpen = _lastVersion < _entries.Count;
                break;
            case ChangeLogDisplayType.HighlightOnly:
                // For highlight only, check if any of the unseen entries has any important entries.
                IsOpen = _entries.Skip(_lastVersion).Any(t => t.IsImportant);
                if (!IsOpen && _lastVersion < _entries.Count)
                    _setConfig(_entries.Count, ChangeLogDisplayType.HighlightOnly);
                break;
            case ChangeLogDisplayType.Never:
                IsOpen = false;
                // Update the count if necessary.
                if (_lastVersion < _entries.Count)
                    _setConfig(_entries.Count, ChangeLogDisplayType.Never);
                break;
        }
    }

    /// <inheritdoc/>
    public override void PreDraw()
    {
        // Set the unscaled size for the window.
        Size = Im.Viewport.Main.Size / 2 / Im.Style.GlobalScale;
        if (Size.Value.X < 800)
            Size = Size.Value with { X = 800 };
        var offset = (Im.Viewport.Main.Size - Size.Value * Im.Style.GlobalScale) / 2;
        Im.Viewport.Main.SetNextWindowPositionRelative(offset, Condition.Appearing);
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        DrawEntries();
        var pos = Size!.Value.X * Im.Style.GlobalScale / 3;
        Im.Cursor.X = pos;
        DrawDisplayTypeCombo(pos);
        Im.Cursor.X = pos;
        DrawUnderstoodButton(pos);
    }

    /// <summary> Draw all entries in the changelog. </summary>
    private void DrawEntries()
    {
        // Draw a child for the entries so we get a scrollbar without losing the button.
        var childSize = Im.ContentRegion.Available;
        childSize.Y -= 3 * Im.Style.FrameHeight;
        using var child = Im.Child.Begin("Entries"u8, childSize);
        if (!child)
            return;

        var color = new Im.ColorDisposable();
        foreach (var (idx, (name, list, hasHighlight)) in _entries.Index().Reverse())
        {
            if (name.Length == 0)
                continue;

            var flags = TreeNodeFlags.NoTreePushOnOpen;

            // Do open the newest entry if it is the only new entry, if it has highlights or if no highlights are required
            var isOpen = idx == _entries.Count - 1
                ? idx == _lastVersion || _displayType != ChangeLogDisplayType.HighlightOnly || hasHighlight
                // Automatically open all entries that have not been seen, if they have highlights or do not require highlights
                : idx >= _lastVersion && (hasHighlight || _displayType != ChangeLogDisplayType.HighlightOnly);

            if (isOpen)
                flags |= TreeNodeFlags.DefaultOpen;

            using var id = Im.Id.Push(idx);
            color.Push(ImGuiColor.Text, HeaderColor);
            using var tree = Im.Tree.Node(name, flags);
            color.Pop();
            CopyToClipboard(_headerName, name, list);
            if (!tree)
                continue;

            foreach (var entry in list)
                entry.Draw();
        }
    }

    /// <summary> Draw the combo to select the changelog display type. </summary>
    private void DrawDisplayTypeCombo(float width)
    {
        Im.Item.SetNextWidth(width);
        using var combo = Im.Combo.Begin("##DisplayType"u8, ToName(_displayType));
        if (!combo)
            return;

        foreach (var type in ChangeLogDisplayType.Values)
        {
            if (Im.Selectable(ToName(type)))
                _setConfig(_lastVersion, type);
        }
    }

    /// <summary> Draw the button to close the changelog window. </summary>
    /// <param name="width"></param>
    private void DrawUnderstoodButton(float width)
    {
        if (!Im.Button("Understood"u8, new Vector2(width, 0)))
            return;

        if (_lastVersion != _entries.Count)
            _setConfig(_entries.Count, _displayType);
        ForceOpen = false;
    }

    /// <summary> Create a new collapsible version header. </summary>
    /// <param name="title"> The name of the version. </param>
    /// <returns> This object for method chaining. </returns>
    public Changelog NextVersion(ReadOnlySpan<byte> title)
    {
        _entries.Add((new StringU8(title), [], false));
        return this;
    }

    /// <summary> Register a changelog note marked as important for the current version. </summary>
    /// <param name="text"> The text of the note to register. </param>
    /// <param name="level"> The indentation level of the note. </param>
    /// <param name="color"> An optional color. If this is <see cref="ColorParameter.Default"/>, <see cref="DefaultImportantColor"/> is used. </param>
    /// <returns> This object for method chaining. </returns>
    [OverloadResolutionPriority(100)]
    public Changelog RegisterImportant(StringU8 text, ushort level = 0, ColorParameter color = default)
    {
        var lastEntry = _entries.Last();
        lastEntry.Entries.Add(new Entry(text, color.CheckDefault(DefaultImportantColor), level));
        _entries[^1] = lastEntry with { IsImportant = true };
        return this;
    }

    /// <inheritdoc cref="RegisterImportant(StringU8,ushort,ColorParameter)"/>
    [OverloadResolutionPriority(0)]
    public Changelog RegisterImportant(ReadOnlySpan<byte> text, ushort level = 0, ColorParameter color = default)
        => RegisterImportant(new StringU8(text), level, color);

    /// <summary> Register a regular changelog note for the current version. </summary>
    /// <param name="text"> The text of the note to register. </param>
    /// <param name="level"> The indentation level of the note. </param>
    /// <returns> This object for method chaining. </returns>
    [OverloadResolutionPriority(0)]
    public Changelog RegisterEntry(StringU8 text, ushort level = 0)
    {
        _entries.Last().Entries.Add(new Entry(text, 0, level));
        return this;
    }

    /// <inheritdoc cref="RegisterEntry(StringU8,ushort)"/>
    [OverloadResolutionPriority(0)]
    public Changelog RegisterEntry(ReadOnlySpan<byte> text, ushort level = 0)
        => RegisterEntry(new StringU8(text), level);

    /// <summary> Register a highlighted changelog note that is NOT marked as important for the current version. </summary>
    /// <param name="text"> The text of the note to register. </param>
    /// <param name="level"> The indentation level of the note. </param>
    /// <param name="color"> An optional color. If this is <see cref="ColorParameter.Default"/>, <see cref="DefaultHighlightColor"/> is used. </param>
    /// <returns> This object for method chaining. </returns>
    public Changelog RegisterHighlight(StringU8 text, ushort level = 0, ColorParameter color = default)
    {
        _entries.Last().Entries.Add(new Entry(text, color.CheckDefault(DefaultHighlightColor), level));
        return this;
    }

    /// <inheritdoc cref="RegisterHighlight(StringU8,ushort,ColorParameter)"/>
    [OverloadResolutionPriority(0)]
    public Changelog RegisterHighlight(ReadOnlySpan<byte> text, ushort level = 0, ColorParameter color = default)
        => RegisterHighlight(new StringU8(text), level, color);

    /// <summary> The internal structure to contain single changelog notes. </summary>
    /// <param name="text"> The text of the note. </param>
    /// <param name="color"> The color of the note. </param>
    /// <param name="subText"> The indentation level of the note. </param>
    private readonly struct Entry(StringU8 text, Rgba32 color, ushort subText)
    {
        public readonly StringU8 Text    = new(text);
        public readonly Rgba32   Color   = color;
        public readonly ushort   SubText = subText;

        /// <summary> Draw this entry as a wrapped bullet text. </summary>
        public void Draw()
        {
            using var tab   = Im.Indent((1 + SubText) * Im.Style.IndentSpacing);
            using var color = ImGuiColor.Text.Push(Color, !Color.IsTransparent);
            Im.Bullet();
            Im.TextWrapped(Text);
        }

        /// <summary> Append this entry to the Discord export string. </summary>
        public void Append(StringBuilder sb)
        {
            for (var i = 0; i < SubText; ++i)
                sb.Append("  ");

            sb.Append("- ");
            if (Color != 0)
                sb.Append("**");

            sb.Append(Text.ToString());
            if (Color != 0)
                sb.Append("**");

            sb.Append('\n');
        }
    }

    /// <summary> Allow copying the changelog with a right-click on the version, formatted for copying to Discord. </summary>
    /// <param name="label"> The label of the entire changelog, usually the name of the plugin. </param>
    /// <param name="name"> The name of the version clicked on. </param>
    /// <param name="entries"> The list of entries belonging to that version. </param>
    [Conditional("DEBUG")]
    private static void CopyToClipboard(StringU8 label, StringU8 name, IEnumerable<Entry> entries)
    {
        try
        {
            if (!Im.Item.RightClicked())
                return;

            var sb = new StringBuilder(1024 * 64);
            if (label.Length > 0)
                sb.Append("# ").Append(label).Append('\n');

            sb.Append("## ")
                .Append(name)
                .Append(" notes, Update <t:")
                .Append(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                .Append(">\n");

            foreach (var entry in entries)
                entry.Append(sb);

            Im.Clipboard.Set($"{sb}");
        }
        catch
        {
            // ignored
        }
    }
}
