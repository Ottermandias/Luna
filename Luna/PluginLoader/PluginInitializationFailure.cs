namespace Luna;

/// <summary> Arguments passed to the <see cref="IIntermediaryPlugin{TPlugin}"/> if the initialization of the plugin fails. </summary>
/// <param name="Stage"> The current initialization stage. </param>
/// <param name="Exception"> The thrown exception. </param>
public record PluginInitializationFailure(PluginInitializationFailure.InitializationStage Stage, Exception Exception)
{
    /// <summary> The distinguished initialization stages. </summary>
    public enum InitializationStage : byte
    {
        /// <summary> During initial launch and construction. </summary>
        Launch,

        /// <summary> During the creation of backups and the loading of game data. </summary>
        BackupsGameData,

        /// <summary> During the creation, reading, parsing and migration of configuration. </summary>
        Configuration,

        /// <summary> During the creation, reading and parsing of data objects for the plugin. </summary>
        PluginObjects,

        /// <summary> During the injection and hooking of game functions. </summary>
        GameInterop,

        /// <summary> During the creation and preparation of the plugin UI. </summary>
        Ui,

        /// <summary> During the creation of the plugin API and IPC methods. </summary>
        Api,
    }

    /// <summary> Whether a thrown exception is handled by the <see cref="IIntermediaryPlugin{TPlugin}"/> or should be rethrown in the plugin initialization step. </summary>
    public enum FailureHandling : byte
    {
        /// <summary> The exception is handled by the intermediary. </summary>
        Handled,

        /// <summary> The exception is not handled and should be rethrown. </summary>
        Rethrow,
    }
}
