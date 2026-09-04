using Dalamud.Plugin;

namespace Luna;

/// <summary> A record of an exposed plugin. </summary>
/// <param name="DisplayName"> The user-visible name of the plugin. </param>
/// <param name="InternalName"> The unique internal name of the plugin. </param>
/// <param name="UniqueId"> A Dalamud-provided uniquely identifying ID for a plugin. </param>
/// <param name="Version"> The version of the plugin. </param>
/// <param name="Info"> Additional information about the plugin. </param>
public readonly record struct CallerPlugin(string DisplayName, string InternalName, Guid UniqueId, Version Version, CallerPluginFlags Info)
{
    /// <summary> Create a record from the data passed by Dalamud's IPC channels. </summary>
    /// <param name="caller"> The exposed plugin from Dalamud. </param>
    public static CallerPlugin FromPlugin(IExposedPlugin? caller)
    {
        if (caller is null)
            return new CallerPlugin("Unknown", string.Empty, Guid.Empty, new Version(0, 0), 0u);

        return new CallerPlugin(caller.Name, caller.InternalName, caller.Manifest.WorkingPluginId, caller.Version, CreateFlags(caller));
    }

    /// <inheritdoc/>
    public bool Equals(CallerPlugin other)
        => UniqueId == other.UniqueId;

    /// <inheritdoc cref="Equals(Luna.CallerPlugin)"/>
    public bool Equals(CallerPlugin? other)
        => UniqueId == other?.UniqueId;

    /// <inheritdoc/>
    public override int GetHashCode()
        => UniqueId.GetHashCode();

    private static CallerPluginFlags CreateFlags(IExposedPlugin caller)
    {
        CallerPluginFlags ret = default;
        if (caller.IsDev)
            ret |= CallerPluginFlags.Developer;
        if (caller.IsBanned)
            ret |= CallerPluginFlags.Banned;
        if (caller.IsDecommissioned)
            ret |= CallerPluginFlags.Decommissioned;
        if (caller.IsOutdated)
            ret |= CallerPluginFlags.Outdated;
        if (caller.IsOrphaned)
            ret |= CallerPluginFlags.Orphan;
        if (caller.IsLoaded)
            ret |= CallerPluginFlags.Loaded;
        if (caller.IsTesting)
            ret |= CallerPluginFlags.Testing;
        if (caller.IsThirdParty)
            ret |= CallerPluginFlags.ThirdParty;
        return ret;
    }
}

/// <summary> Additional information flags. </summary>
[Flags]
public enum CallerPluginFlags
{
    /// <summary> The plugin is from a third party repository. </summary>
    ThirdParty = 1 << 0,

    /// <summary> The plugin is installed as a developer plugin. </summary>
    Developer = 1 << 1,

    /// <summary> The plugin is marked as outdated. </summary>
    Outdated = 1 << 2,

    /// <summary> The plugin is installed as a testing version. </summary>
    Testing = 1 << 3,

    /// <summary> The plugin is currently loaded. </summary>
    Loaded = 1 << 4,

    /// <summary> The plugins version was banned. </summary>
    Banned = 1 << 5,

    /// <summary> The plugin is decommissioned. </summary>
    Decommissioned = 1 << 6,

    /// <summary> The plugin is orphaned, i.e. its repository is unavailable. </summary>
    Orphan = 1 << 7,
}
