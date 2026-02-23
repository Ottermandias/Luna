using Dalamud.Interface.ImGuiNotification;

namespace Luna;

/// <summary> A base class for efficient configuration files using the Luna saving subsystems. </summary>
/// <typeparam name="TProvider"> The type of file name provider this file uses. </typeparam>
/// <param name="saveService"> The actual save service this file uses. </param>
/// <param name="messager"> The file name provider this file uses. </param>
/// <param name="saveDelay"> An optional delay for saving this file. If this is null, a minute is used. </param>
public abstract class ConfigurationFile<TProvider>(BaseSaveService<TProvider> saveService, MessageService messager, TimeSpan? saveDelay = null)
    : ISavable<TProvider>, IService
    where TProvider : BaseFilePathProvider
{
    /// <summary> The save service this file uses. </summary>
    protected readonly BaseSaveService<TProvider> SaveService = saveService;

    /// <summary> The messager this file uses. </summary>
    protected readonly MessageService Messager = messager;

    /// <summary> The current version of this file in the program. </summary>
    public abstract int CurrentVersion { get; }

    /// <summary> The function that writes this file's specific data to the JSON stream. </summary>
    /// <param name="j"> The JSON writer. </param>
    protected abstract void AddData(JsonTextWriter j);

    /// <summary> The function that parses the JObject and fills this files data with the result. </summary>
    /// <param name="j"> The deserialized JObject. </param>
    protected abstract void LoadData(JObject j);

    /// <summary> Obtain this file's path for saving or loading from the file name provider. </summary>
    /// <returns> The full path for this file. </returns>
    public abstract string ToFilePath(TProvider fileNames);

    /// <summary> An optional delay for saving this file. </summary>
    protected TimeSpan SaveDelay { get; set; } = saveDelay ?? TimeSpan.FromMinutes(1);

    /// <summary> Save this file using the stored delay. </summary>
    public virtual void Save()
        => SaveService.DelaySave(this, SaveDelay);

    /// <summary> Save this file with common data, indentation and using <see cref="AddData"/>. </summary>
    /// <param name="writer"> The stream writer to write to.</param>
    public virtual void Save(StreamWriter writer)
    {
        using var j = new JsonTextWriter(writer);
        j.Formatting = Formatting.Indented;
        j.WriteStartObject();
        j.WritePropertyName("Version");
        j.WriteValue(CurrentVersion);
        AddData(j);
        j.WriteEndObject();
    }

    /// <summary> Load this file from its default file path if it exists using <see cref="LoadData"/> and notify on errors. </summary>
    protected virtual void Load()
    {
        var fileName = ToFilePath(SaveService.FileNames);
        var logName  = ((ISavable<TProvider>)this).LogName(fileName);
        if (!File.Exists(fileName))
            return;

        try
        {
            Messager.Log.Debug($"Reading {logName}...");
            var text = File.ReadAllText(fileName);
            var jObj = JObject.Parse(text);
            if (jObj["Version"]?.Value<int>() is not { } version)
                throw new Exception("No version provided.");

            if (version != CurrentVersion && HandleVersionMigration(logName, jObj, version))
                return;

            LoadData(jObj);
        }
        catch (Exception ex)
        {
            Messager.NotificationMessage(ex, $"Error reading {logName}, reverting to default.",
                $"Error reading {logName}", NotificationType.Error);
        }
    }

    /// <summary> Handle the migration of old versions of this file. If the version can not be migrated, an exception should be thrown. The default implementation throws an exception for all versions. </summary>
    /// <param name="logName"> The name of this type of file for logging purposes. </param>
    /// <param name="data"> The parsed JSON data of this file. </param>
    /// <param name="version"> The version of the file. </param>
    /// <returns> False if the regular LoadData should still be called, true if the migration incorporated the loading. Failure to migrate should cause an exception. </returns>
    /// <remarks> This is not called if <paramref name="version"/> is the same as <see cref="CurrentVersion"/>, or no version could be parsed. </remarks>
    protected virtual bool HandleVersionMigration(string logName, JObject data, int version)
        => throw new Exception($"{logName} Version {version} is outdated and can not be migrated.");
}
