using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Luna;

/// <summary> The base class to provide file paths for different type of configuration or data files. </summary>
public abstract class BaseFilePathProvider(IDalamudPluginInterface pluginInterface) : IService
{
    /// <summary> The directory containing the game's data. </summary>
    public readonly string GameDataDirectory = pluginInterface.GetRequiredService<IDataManager>().GameData.DataPath.FullName;

    /// <summary> The root path of the dalamud configuration files. </summary>
    public readonly string DalamudRootDirectory = pluginInterface.ConfigDirectory.Parent!.Parent!.FullName;

    /// <summary> The directory of the loaded DLL. </summary>
    public readonly string AssemblyDirectory = pluginInterface.AssemblyLocation.DirectoryName!;

    /// <summary> The default configuration file for this plugin. </summary>
    public readonly string ConfigurationFile = pluginInterface.ConfigFile.FullName;

    /// <summary> The default configuration directory for this plugin. </summary>
    public readonly string ConfigurationDirectory = pluginInterface.ConfigDirectory.FullName;

    /// <summary> Get all backup files for this plugin. </summary>
    public abstract List<IBackupFile> GetBackupFiles();
}
