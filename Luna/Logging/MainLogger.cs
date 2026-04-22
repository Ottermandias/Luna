using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Luna;

/// <inheritdoc cref="MainLogger"/>
/// <typeparam name="T"> Unused type parameter for dependency injection. </typeparam>
public sealed class MainLogger<T>(string? pluginName = null) : MainLogger(pluginName), ILogger<T>;

/// <summary> Get the Serilog log, convert it to a Microsoft ILogger and set a plugin specific prefix. </summary>
public class MainLogger : LunaLogger
{
    /// <summary> Get a typed version of the main logger. </summary>
    /// <typeparam name="T"> The unused type parameter. </typeparam>
    public MainLogger<T> Typed<T>()
        => new(PluginName);

    // Keep loggers footprint a bit smaller by keeping those fields static. </summary>
    public static string          GlobalPluginName   { get; private set; } = null!;
    public static string          GlobalPrefix       { get; private set; } = null!;
    public static Serilog.ILogger GlobalPluginLogger { get; private set; } = null!;

    /// <summary> Get the loggers prefix. </summary>
    public sealed override string Prefix
        => GlobalPrefix;

    /// <summary> Get the name of the plugin. </summary>
    public string PluginName
        => GlobalPluginName;

    /// <summary> Get the Serilog logger provided by Dalamud. </summary>
    public sealed override Serilog.ILogger Logger
        => GlobalPluginLogger;

    /// <summary> Create a new instance by getting the assembly name as a plugin name and fetching Dalamuds context. </summary>
    public MainLogger(string? pluginName = null)
    {
        // The statics are null!-initialized.
        GlobalPluginName   ??= pluginName ?? Assembly.GetCallingAssembly().GetName().Name ?? "Unknown";
        GlobalPluginLogger ??= Serilog.Log.ForContext("Dalamud.PluginName", GlobalPluginName);
        GlobalPrefix       ??= $"[{GlobalPluginName}] ";
    }
}
