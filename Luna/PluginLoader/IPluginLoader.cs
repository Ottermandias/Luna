using System.Reflection;
using Dalamud.Plugin;

namespace Luna;

/// <summary> An untyped interface for any <see cref="PluginLoader{TPlugin}"/>. </summary>
public interface IPluginLoader
{
    /// <summary> The plugin interface passed by Dalamud. </summary>
    public IDalamudPluginInterface PluginInterface       { get; }

    /// <summary> The main logger. </summary>
    public MainLogger              Log                   { get; }

    /// <summary> The service manager. </summary>
    public ServiceManager          Services              { get; }

    /// <summary> The source of the plugin. </summary>
    public PluginLoadSource        Source                { get; }

    /// <summary> The name of the plugin. </summary>
    public string                  PluginName            { get; }

    /// <summary> The version of the plugin manifest or assembly. </summary>
    public Version                 Version               { get; }

    /// <summary> The commit hash of the plugin, if available. </summary>
    /// <remarks> The commit hash is obtained by querying the custom assembly attribute <see cref="AssemblyInformationalVersionAttribute"/>'s <see cref="AssemblyInformationalVersionAttribute.InformationalVersion"/>, if available. </remarks>
    public string                  CommitHash            { get; }

    /// <summary> Whether the loader is in the intermediary plugin state and waits for user input on that. </summary>
    public bool                    HasIntermediaryPlugin { get; }

    /// <summary> Whether the loader successfully loaded the main plugin. </summary>
    public bool                    HasPlugin             { get; }

    /// <summary> Obtain the load source of a plugin as passed by Dalamud. </summary>
    /// <param name="pluginInterface"> The plugin interface passed by Dalamud. </param>
    /// <returns> The load source. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static PluginLoadSource FetchSource(IDalamudPluginInterface pluginInterface)
        => pluginInterface.IsDev
            ? PluginLoadSource.Dev
            : pluginInterface.IsTesting
                ? PluginLoadSource.Testing
                : PluginLoadSource.Normal;

    /// <summary> Obtain the version of a plugin using its manifest for regularly installed plugins or its assembly version for developer plugins. </summary>
    /// <typeparam name="TPlugin"> The plugin definition. Only used for the assembly for developer plugins. </typeparam>
    /// <param name="pluginInterface"> The plugin interface passed by Dalamud, containing the manifest for regularly installed plugins. </param>
    /// <param name="log"> The logger for failure logging. </param>
    /// <param name="source"> The load source to distinguish testing plugins. </param>
    /// <returns> The appropriate version. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Version FetchVersion<TPlugin>(IDalamudPluginInterface pluginInterface, MainLogger log, PluginLoadSource source)
    {
        try
        {
            return source switch
            {
                PluginLoadSource.Normal  => pluginInterface.Manifest.AssemblyVersion,
                PluginLoadSource.Testing => pluginInterface.Manifest.TestingAssemblyVersion ?? pluginInterface.Manifest.AssemblyVersion,
                _                        => typeof(TPlugin).Assembly.GetName().Version ?? pluginInterface.Manifest.AssemblyVersion,
            };
        }
        catch (Exception ex)
        {
            log.Error($"Could not fetch plugin version:\n{ex}");
            return new Version(1, 1, 1, 1);
        }
    }

    /// <summary> Try to fetch the commit hash of a plugin if it is embedded in the InformationalVersion. </summary>
    /// <remarks>
    ///   The commit hash can be embedded into the assembly using the following MSBuild target:
    ///   <code>
    ///   &lt;Target Name="GetGitHash" BeforeTargets="GetAssemblyVersion" Returns="InformationalVersion"&gt;
    ///       &lt;Exec Command="git rev-parse --short HEAD" ConsoleToMSBuild="true" StandardOutputImportance="low" ContinueOnError="true"&gt;
    ///           &lt;Output TaskParameter="ExitCode" PropertyName="GitCommitHashSuccess" /&gt;
    ///           &lt;Output TaskParameter="ConsoleOutput" PropertyName="GitCommitHash" Condition="$(GitCommitHashSuccess) == 0" /&gt;
    ///       &lt;/Exec&gt;
    ///   
    ///       &lt;PropertyGroup&gt;
    ///           &lt;InformationalVersion&gt;$(GitCommitHash)&lt;/InformationalVersion&gt;
    ///       &lt;/PropertyGroup&gt;
    ///   &lt;/Target&gt;
    ///   </code>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string FetchHash<TPlugin>()
        => typeof(TPlugin).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";
}
