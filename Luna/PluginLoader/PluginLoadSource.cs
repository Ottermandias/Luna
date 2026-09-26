using Luna.Generators;

namespace Luna;

/// <summary> Load source information for a plugin. </summary>
[NamedEnum]
public enum PluginLoadSource
{
    /// <summary> The plugin was loaded from a repository and is a non-testing version. </summary>
    [Name("")]
    Normal,

    /// <summary> The plugin was loaded from a repository and is a testing version. </summary>
    [Name(" (Testing)")]
    Testing,

    /// <summary> The plugin was loaded from a developer assembly. </summary>
    [Name(" (Dev)")]
    Dev,
}
