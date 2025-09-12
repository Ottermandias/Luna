using Dalamud.Plugin;

namespace Luna;

/// <summary> The base class to provide file paths for different type of configuration or data files. </summary>
public abstract class BaseFilePathProvider(IDalamudPluginInterface pluginInterface) : IService
{
    /// <summary> The default configuration file for this plugin. </summary>
    public readonly string ConfigurationFile = pluginInterface.ConfigFile.FullName;

    /// <summary> The default configuration directory for this plugin. </summary>
    public readonly string ConfigurationDirectory = pluginInterface.ConfigDirectory.FullName;

    /// <summary> Get all backup files for this plugin. </summary>
    public abstract List<FileInfo> GetBackupFiles();
}
