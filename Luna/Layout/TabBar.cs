namespace Luna;

/// <summary> A managed tab bar. </summary>
/// <typeparam name="T"> The identifying enum to control tab selection. </typeparam>
/// <param name="barName"> The name of the tab bar used for the event and logging and as the label of the bar. </param>
/// <param name="log"> A logger. </param>
/// <param name="tabs"> The list of all possible tabs in this tab bar. </param>
public abstract class TabBar<T>(string barName, Logger log, params IReadOnlyList<ITab<T>> tabs) : IUiService
    where T : unmanaged, Enum
{
    /// <summary> The label used to draw the bar. </summary>
    public readonly StringU8 Label = new(barName);

    /// <summary> Additional flags to control the tab bar's behavior. </summary>
    public TabBarFlags Flags { get; set; } = TabBarFlags.None;

    /// <summary> The list of all tabs in this tab bar. </summary>
    public readonly IReadOnlyList<ITab<T>> Tabs = tabs;

    /// <summary> A list of optional buttons to add at the end of the tab bar. </summary>
    public readonly ButtonList Buttons = new();

    /// <summary> The currently selected tab. </summary>
    /// <remarks> If the tab is set, <see cref="TabSelected"/> is invoked. </remarks>
    public T CurrentTab
    {
        get;
        private set
        {
            if (field.Equals(value))
                return;

            field = value;
            TabSelected.Invoke(value);
        }
    }

    /// <summary> A tab to be selected when the tab bar is drawn next. </summary>
    public T? NextTab { get; set; }

    /// <summary> Invoked whenever a different tab is selected. </summary>
    public readonly TabEvent TabSelected = new($"{barName}Changed", log);

    /// <summary> Draw the tab bar. </summary>
    public void Draw()
    {
        using var tabBar = Im.TabBar.Begin(Label, Flags);
        if (!tabBar)
            return;

        // Do not overwrite the next tab after the iteration because it may be changed during it.
        var nextTab = NextTab;
        NextTab = null;

        foreach (var tabData in Tabs.Where(t => t.IsVisible))
        {
            var flags = tabData.Flags;
            if (nextTab.HasValue && tabData.Identifier.Equals(nextTab.Value))
                flags |= TabItemFlags.SetSelected;

            using var tab = tabBar.Item(tabData.Label, flags);
            tabData.PostTabButton();
            if (!tab)
                continue;

            tabData.DrawContent();
            CurrentTab = tabData.Identifier;
        }

        foreach (var button in Buttons)
            button.DrawTabBarButton(tabBar);
    }

    /// <summary> The event for changing tabs. </summary>
    public sealed class TabEvent(string name, Logger log) : EventBase<T, uint>(name, log), IConstructedService;
}
