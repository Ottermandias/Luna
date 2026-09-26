namespace Luna;

/// <summary> A base class to collect support info data. </summary>
/// <param name="pluginLoader"> The plugin loader. </param>
public class SupportInfoProvider(IPluginLoader pluginLoader)
{
    protected readonly IPluginLoader PluginLoader = pluginLoader;

    private record InfoSection(string Category, int Priority, List<(string Property, string Value)> Properties)
    {
        public int Priority { get; set; } = Priority;
    }

    private readonly List<InfoSection> _data = [];
    private          InfoSection?      _currentSection;
    private          int               _alignment;

    /// <summary> The internal names of all plugins we know we may be interested in. </summary>
    /// <remarks> This can be overriden to add or remove specific plugins. </remarks>
    protected virtual IEnumerable<string> RelevantPlugins
        =>
        [
            "Penumbra",
            "Glamourer",
            "CustomizePlus",
            "SimpleHeels",
            "Ktisis",
            "Brio",
            "DynamicBridge",
            "heliosphere-plugin",
            "VfxEditor",
            "IllusioVitae",
            "Aetherment",
            "GagSpeak",
            "ProjectGagSpeak",
            "Proteus",
            "AQuestReborn",
            "DragAndDropTexturing",
            "RoleplayingVoiceDalamud",
            "CharacterSelectPlugin",
            "LoporritSync",
            "KittenSync",
            "Snowcloak",
            "LightlessSync",
            "Sphene",
            "XivSync",
            "MareSempiterne", // PlayerSync
            "AnatoliIliou",
            "LaciSynchroni",
        ];

    /// <summary> Gather the full support information string, formatted for discord. </summary>
    /// <param name="callers"> Any IPC/API callers added during runtime. If they are also listed in <see cref="RelevantPlugins"/>, they are skipped in that list. </param>
    /// <returns> Support information pre-formatted for a discord message. </returns>
    public string GatherSupportInformation(IReadOnlySet<CallerPlugin>? callers = null)
    {
        var sb = new StringBuilder(10240);
        // Prepare data to obtain alignment.
        PrepareData(callers);

        try
        {
            // Actually print each section with its aligned properties.
            foreach (var (category, _, properties) in _data.OrderByDescending(t => t.Priority))
            {
                if (category.Length > 0)
                    sb.Append("**").Append(category).AppendLine("**");
                foreach (var (property, value) in properties)
                    CreateProperty(sb, _alignment, property, value);
            }

            return sb.ToString();
        }
        finally
        {
            // Clean up temp data.
            _data.Clear();
            _currentSection = null;
            _alignment      = 0;
        }
    }

    /// <summary> Prepare all data relayed to the support info. </summary>
    /// <param name="callers"> Any IPC/API callers added during runtime. If they are also listed in <see cref="RelevantPlugins"/>, they are skipped in that list. </param>
    /// <remarks> This method should use <see cref="StartSection"/> and <see cref="AddProperty"/> to create its data tree. The helpers <see cref="AddPluginData"/> and <see cref="GatherRelevantPlugins"/> can also be used.</remarks>
    protected virtual void PrepareData(IReadOnlySet<CallerPlugin>? callers)
    {
        AddPluginData();
        GatherRelevantPlugins(callers);
    }

    /// <summary> Add the basic version and load data for the calling plugin. </summary>
    /// <remarks> This creates a 'Settings' section with a priority of 1000, which should usually be the first section. </remarks>
    protected void AddPluginData()
    {
        StartSection("Settings", 1000);
        AddProperty("Plugin Version",
            $"{PluginLoader.Version}{PluginLoader.Source.ToName()}{(PluginLoader.HasPlugin ? string.Empty : "(LOAD FAILURE)")}");
        AddProperty("Commit Hash", PluginLoader.CommitHash);
        AddProperty("Load Reason",
            $"{PluginLoader.PluginInterface.Reason} at {PluginLoader.PluginInterface.LoadTimeUTC:g} ({PluginLoader.PluginInterface.LoadTimeDelta:g})");
        var isWine = Dalamud.Utility.Util.IsWine();
        AddProperty("Operating System", Dalamud.Utility.Util.IsWine() ? "Mac/Linux (Wine)" : "Windows");
        if (isWine)
            AddProperty("Locale Environment Variables", CollectLocaleEnvironmentVariables());
    }

    /// <summary> Start a new section for properties or append to an existing one. </summary>
    /// <param name="header"> The name of the section to append to. </param>
    /// <param name="priority"> The priority. Higher priority sections will be printed first. If this changes for an existing section, the new priority will be used. </param>
    protected void StartSection(string header, int priority)
    {
        var existingIndex = _data.FindIndex(t => t.Category == header);
        if (existingIndex < 0)
        {
            _currentSection = new InfoSection(header, priority, []);
            _data.Add(_currentSection);
        }
        else
        {
            _currentSection = _data[existingIndex];
            if (_currentSection.Priority == priority)
                return;

            _currentSection.Priority = priority;
        }
    }

    /// <summary> Add a property and its value to the current section. Starts a default section if no section is open. </summary>
    /// <param name="property"> The name of the property. </param>
    /// <param name="value"> The value of the property. </param>
    /// <param name="countAlignment"> Whether the name of this property contributes to the desired alignment or not. </param>
    protected void AddProperty(string property, string value, bool countAlignment = true)
    {
        if (_currentSection is null)
            StartSection(string.Empty, 0);
        _currentSection!.Properties.Add((property, value));
        if (countAlignment && _alignment <= property.Length)
            _alignment = property.Length + 1; // +1 for the :
    }

    /// <summary> Add a property and its value to the current section. Starts a default section if no section is open. </summary>
    /// <param name="property"> The name of the property. </param>
    /// <param name="value"> The value of the property. </param>
    /// <param name="countAlignment"> Whether the name of this property contributes to the desired alignment or not. </param>
    protected void AddProperty<T>(string property, T? value, bool countAlignment = true)
        => AddProperty(property, value?.ToString() ?? "NULL", countAlignment);

    /// <summary> Obtain and add all relevant plugins and add them to a 'Plugins' section with priority 100. </summary>
    protected void GatherRelevantPlugins(IReadOnlySet<CallerPlugin>? callers = null)
    {
        var installedPlugins = PluginLoader.PluginInterface.InstalledPlugins.GroupBy(p => p.InternalName)
            .ToDictionary(g => g.Key, g =>
            {
                var item = g.OrderByDescending(p => p.IsLoaded).ThenByDescending(p => p.Version).First();
                return (item.IsLoaded, item.Version, item.Name, item.Manifest.WorkingPluginId);
            });

        callers ??= new HashSet<CallerPlugin>();
        StartSection("Plugins", 100);
        foreach (var plugin in callers.OrderBy(p => p.DisplayName).ThenBy(p => p.Version))
            AddProperty(plugin.DisplayName, $"{plugin.Version} (IPC)", false);

        foreach (var plugin in RelevantPlugins)
        {
            // Skip self
            if (plugin == PluginLoader.PluginName)
                continue;

            // Skip plugins included through caller tracking.
            if (callers.Any(c => c.InternalName == plugin))
                continue;

            if (installedPlugins.TryGetValue(plugin, out var data))
                AddProperty(data.Name, $"{data.Version}{(data.IsLoaded ? string.Empty : " (Disabled)")}", false);
        }
    }

    private static string CollectLocaleEnvironmentVariables()
    {
        var variableNames = new List<string>();
        var variables     = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (DictionaryEntry variable in Environment.GetEnvironmentVariables())
        {
            var key = (string)variable.Key;
            if (key.Equals("LANG", StringComparison.Ordinal) || key.StartsWith("LC_", StringComparison.Ordinal))
            {
                variableNames.Add(key);
                variables.Add(key, (string?)variable.Value ?? string.Empty);
            }
        }

        variableNames.Sort();

        var pos = variableNames.IndexOf("LC_ALL");
        if (pos > 0) // If it's == 0, we're going to do a no-op.
        {
            variableNames.RemoveAt(pos);
            variableNames.Insert(0, "LC_ALL");
        }

        pos = variableNames.IndexOf("LANG");
        if (pos >= 0 && pos < variableNames.Count - 1)
        {
            variableNames.RemoveAt(pos);
            variableNames.Add("LANG");
        }

        return variableNames.Count == 0
            ? "None"
            : string.Join(", ", variableNames.Select(name => $"`{name}={variables[name]}`"));
    }

    private static void CreateProperty(StringBuilder sb, int alignment, string property, string value)
    {
        sb.Append("> **`").Append(property).Append(':');
        while (property.Length < alignment--)
            sb.Append(' ');
        sb.Append("`** ").Append(value).AppendLine();
    }
}
