namespace Luna;

/// <summary> The available strategies for creating backup files. </summary>
public enum BackupMode
{
    /// <summary> Files are deleted or overwritten without backing them up in any way. </summary>
    NoBackups = 0,

    /// <summary> Any file that is deleted or would be overwritten is moved to [filename].[extension].bak. This file is overwritten if it exists. </summary>
    SingleBackup = 1,

    /// <summary> Any file that is deleted or would be overwritten is moved to [filename]_[yyyyMMddhhmmss].[extension].bak. These files are overwritten should it exist. </summary>
    TimestampedBackup = 2,
}

public class BaseSaveService(LunaLogger log, FrameworkManager framework)
{
    /// <summary> An encoding that omits the BOM. </summary>
    protected static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);

    /// <summary> The default delay when using <see cref="SaveType.Delay"/> without specifying a custom delay. </summary>
#if DEBUG
    protected static readonly TimeSpan StandardDelay = TimeSpan.FromSeconds(2);
#else
    protected static readonly TimeSpan StandardDelay = TimeSpan.FromSeconds(10);
#endif

    /// <summary> Whether this save service should back up files before deleting or overwriting them, and in what way. </summary>
    public BackupMode BackupMode { get; init; } = BackupMode.TimestampedBackup;

    /// <summary> The logger to use. </summary>
    public readonly LunaLogger Log = log;

    /// <summary> The framework event handler to use. </summary>
    protected readonly FrameworkManager Framework = framework;

    /// <inheritdoc cref="WriteWithBackup(LunaLogger,string,Action{string},BackupMode)"/>
    public void WriteWithBackup(string filePath, Action<string> writeFile)
        => WriteWithBackup(Log, filePath, writeFile, BackupMode);

    /// <inheritdoc cref="DeleteWithBackup(LunaLogger,string,BackupMode)"/>
    public void DeleteWithBackup(string filePath)
        => DeleteWithBackup(Log, filePath, BackupMode);

    /// <summary> Safely move a file to a backup location or delete it, depending on mode. </summary>
    /// <param name="log"> The logger to use. </param>
    /// <param name="filePath"> The file to delete or move. </param>
    /// <param name="mode"> The backup mode to use. </param>
    public static void DeleteWithBackup(LunaLogger log, string filePath, BackupMode mode = BackupMode.NoBackups)
    {
        if (filePath.Length is 0)
            return;

        if (!File.Exists(filePath))
            return;

        var threadPrefix = GetThreadPrefix();
        try
        {
            log.Information($"{threadPrefix}Deleting {filePath}...");
            if (CreateBackup(log, mode, threadPrefix, "file", filePath, filePath, out var backup) && backup is not null)
                File.Delete(filePath);
        }
        catch (Exception ex)
        {
            log.Error($"{threadPrefix}Could not delete file {filePath}:\n{ex}");
        }
    }

    /// <summary> Safely write data to a file using temporary files and creating backups, logging everything. </summary>
    /// <param name="log"> The logger to use. </param>
    /// <param name="filePath"> The original file path to write to. </param>
    /// <param name="writeFile"> The function that writes the file. </param>
    /// <param name="mode"> The backup mode to use. </param>
    public static void WriteWithBackup(LunaLogger log, string filePath, Action<string> writeFile,
        BackupMode mode = BackupMode.TimestampedBackup)
    {
        if (filePath.Length is 0)
            throw new ArgumentException($"{filePath} can not be empty.", nameof(filePath));

        var exists       = File.Exists(filePath);
        var tmpPath      = exists ? Path.ChangeExtension(filePath, ".tmp") : filePath;
        var threadPrefix = GetThreadPrefix();

        // Write the recovered data first to a temporary file.
        string? directory        = null;
        var     directoryCreated = false;
        try
        {
            log.Debug(exists
                ? $"{threadPrefix}Writing temporary file {tmpPath}..."
                : $"{threadPrefix}Writing file {tmpPath} for the first time...");
            if (!exists)
            {
                directory = Path.GetDirectoryName(tmpPath);
                if (directory is not null)
                {
                    Directory.CreateDirectory(directory);
                    directoryCreated = true;
                }
            }

            writeFile(tmpPath);
        }
        catch (Exception ex)
        {
            log.Error(exists
                ? $"{threadPrefix}Failed to write temporary file {tmpPath}:\n{ex}"
                : $"{threadPrefix}Failed to write file {tmpPath}:\n{ex}");
            if (!directoryCreated)
                return;

            try
            {
                Directory.Delete(directory!);
            }
            catch (Exception ex2)
            {
                log.Error($"{threadPrefix}Failed to remove newly created directory {directory} after failure to write file:\n{ex2}");
            }

            return;
        }

        // If the file did not exist beforehand, we do not need to create backups or move files.
        if (!exists)
            return;

        // Then move the existing data to a backup, if expected.
        var backupPath = GetBackupName(mode, filePath);
        if (backupPath is not null)
        {
            log.Information($"{threadPrefix}Moving {filePath} to backup at {backupPath}...");
            try
            {
                File.Move(filePath, backupPath, true);
            }
            catch (Exception e)
            {
                log.Error($"{threadPrefix}Failed to move {filePath} to backup:\n{e}");
                try
                {
                    File.Delete(tmpPath);
                }
                catch (Exception e2)
                {
                    log.Error($"{threadPrefix}Failed to delete temporary file {tmpPath} after failing to create backup:\n{e2}");
                    return;
                }
            }
        }

        try
        {
            // Then move the temporary file to the actual file path.
            // This may only overwrite if we have no backups.
            File.Move(tmpPath, filePath, mode is BackupMode.NoBackups);
        }
        catch (Exception e3)
        {
            log.Error($"{threadPrefix}Failed to move temporary file {tmpPath} to {filePath}:\n{e3}");
            // On failures, try to clean up the temporary file and move the backup back. Neither of those should generally happen.
            if (backupPath is not null)
                try
                {
                    log.Debug(
                        $"{threadPrefix}Moving created backup {backupPath} back to {filePath} after failure to move temporary file {tmpPath}...");
                    File.Move(backupPath, filePath, true);
                }
                catch (Exception e4)
                {
                    log.Error($"{threadPrefix}Failed to move backup {backupPath} back to {filePath}:\n{e4}");
                }

            try
            {
                File.Delete(tmpPath);
            }
            catch (Exception e5)
            {
                log.Error($"{threadPrefix}Failed to delete temporary file {tmpPath} after failure to move it:\n{e5}");
            }
        }
    }

    /// <summary> Move a file to its backup location. </summary>
    /// <param name="log"> The logger to use. </param>
    /// <param name="mode"> The backup mode to use. If this is <see cref="BackupMode.NoBackups"/>, this just returns true. </param>
    /// <param name="threadPrefix"> The thread prefix for the log. </param>
    /// <param name="typeName"> The name of the backed up object type. </param>
    /// <param name="logName"> The name of the backed up value for logging. </param>
    /// <param name="name"> The full original file path of the backed up file. </param>
    /// <param name="backupName"> The path the backed up file is moved to on success, <c>null</c> if <see cref="BackupMode.NoBackups"/> is used. </param>
    /// <returns> True if <see cref="BackupMode.NoBackups"/> is used or the backup was successful, false otherwise. </returns>
    protected static bool CreateBackup(LunaLogger log, BackupMode mode, string threadPrefix, string typeName, string logName, string name,
        out string? backupName)
    {
        backupName = GetBackupName(mode, name);
        if (backupName is null)
            return true;

        try
        {
            log.Debug($"{threadPrefix}Backing up existing {typeName} {logName}...");
            File.Move(name, backupName, true);
            return true;
        }
        catch (Exception ex)
        {
            log.Error($"{threadPrefix}Unable to create backup for {name} at {backupName} before overwriting:\n{ex}");
            return false;
        }
    }

    /// <summary> Get the log prefix of the current thread ID. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static string GetThreadPrefix()
        => $"[{Environment.CurrentManagedThreadId}] ";

    /// <summary> Get the appropriate name for a backup file given a file path and the backup mode. </summary>
    private static string? GetBackupName(BackupMode mode, string original)
        => mode switch
        {
            BackupMode.NoBackups         => null,
            BackupMode.SingleBackup      => original + ".bak",
            BackupMode.TimestampedBackup => Path.ChangeExtension(original, $"_{DateTime.Now:yyyyMMddhhmmss}{Path.GetExtension(original)}.bak"),
            _                            => null,
        };
}

/// <summary> A service to simplify file-saving and deleting across the application. </summary>
/// <typeparam name="T"> The type of the provider of file paths for this service. </typeparam>
/// <param name="log"> The logger to use. </param>
/// <param name="framework"> The framework event handler to use. </param>
/// <param name="fileNames"> The file path provider for saving <see cref="ISavable{T}"/>. </param>
/// <param name="awaiter"> An optional awaiter that is waited before any file writes can take place. </param>
public abstract class BaseSaveService<T>(LunaLogger log, FrameworkManager framework, T fileNames, Task? awaiter = null)
    : BaseSaveService(log, framework), IDisposable
    where T : BaseFilePathProvider
{
    /// <summary> The provider of file paths for this service. </summary>
    public readonly T FileNames = fileNames;

    /// <summary> The currently running save task, if any. </summary>
    private Task? _saveTask = awaiter;

    /// <summary> A lock to handle appending to the save task. </summary>
    private readonly Lock _saveTaskLock = new();

    /// <summary> Save an object according to type. </summary>
    /// <param name="type"> The save type to invoke. </param>
    /// <param name="value"> The file to save. </param>
    public void Save<TSavable>(SaveType type, in TSavable value) where TSavable : ISavable<T>
    {
        switch (type)
        {
            case SaveType.Queue:
                QueueSave(value);
                return;
            case SaveType.Delay:
                DelaySave(value);
                return;
            case SaveType.Immediate:
                ImmediateSave(value);
                return;
            case SaveType.ImmediateSync:
                ImmediateSaveSync(value);
                return;
        }
    }

    /// <summary> Queue a file save for the next available framework tick. </summary>
    /// <param name="value"> The file to save. </param>
    public void QueueSave<TSavable>(TSavable value) where TSavable : ISavable<T>
    {
        var file = value.ToFilePath(FileNames);
        Framework.RegisterOnTick($"{value.GetType().Name} ## {file}", () => { ImmediateSave(value); });
    }

    /// <summary> Queue a delayed file save with the standard delay after the delay is over. </summary>
    /// <param name="value"> The file to save. </param>
    public void DelaySave<TSavable>(in TSavable value) where TSavable : ISavable<T>
        => DelaySave(value, StandardDelay);

    /// <summary> Queue a delayed file save for after the delay is over. </summary>
    /// <param name="value"> The file to save. </param>
    /// <param name="delay"> The custom delay to wait before actually triggering the save. </param>
    public void DelaySave<TSavable>(TSavable value, TimeSpan delay) where TSavable : ISavable<T>
    {
        var file = value.ToFilePath(FileNames);
        Framework.RegisterDelayed($"{value.GetType().Name} ## {file}", () => { ImmediateSave(value); }, delay);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    /// <summary> Dispose by waiting for the save task to finish. </summary>
    /// <param name="disposing"> Whether this is called from <see cref="Dispose"/> or a finalizer. </param>
    protected virtual void Dispose(bool disposing)
    {
        lock (_saveTaskLock)
        {
            _saveTask?.Wait();
            _saveTask = null;
        }
    }

    ~BaseSaveService()
        => Dispose(false);

    /// <summary> Immediately trigger a save on the service's save thread. </summary>
    /// <param name="value"> The file to save. </param>
    public void ImmediateSave<TSavable>(TSavable value) where TSavable : ISavable<T>
    {
        var name = value.ToFilePath(FileNames);
        // Lock the object before replacing it.
        // We only want one thread saving files at the same time.
        lock (_saveTaskLock)
        {
            _saveTask = _saveTask is null || _saveTask.IsCompleted
                ? Task.Run(SaveAction)
                : _saveTask.ContinueWith(_ => SaveAction(), TaskScheduler.Default);
        }

        return;

        void SaveAction()
        {
            var logName      = value.LogName(name);
            var typeName     = value.TypeName;
            var threadPrefix = GetThreadPrefix();
            try
            {
                if (name.Length is 0)
                    throw new Exception("Invalid object returned empty filename.");

                // Check if the file written to already exists, and if it does, write to a temporary file first.
                var fileExisted = File.Exists(name);

                Log.Debug($"{threadPrefix}Saving {typeName} {logName} {(fileExisted ? "using secure write" : "for the first time")}...");
                var firstName = fileExisted ? name + ".tmp" : name;
                var file      = new FileInfo(firstName);

                // Create all required directories to write the file.
                file.Directory?.Create();

                // Open the new or temporary file as a stream and write to it.
                using (var s = file.Exists ? file.Open(FileMode.Truncate) : file.Open(FileMode.CreateNew))
                {
                    value.Save(s);
                }

                // If we wrote to a temporary file, move the fully written file to replace the original file when done.
                if (fileExisted)
                {
                    if (!CreateBackup(Log, BackupMode, threadPrefix, typeName, logName, name, out var backup))
                        try
                        {
                            File.Delete(firstName);
                        }
                        catch (Exception e)
                        {
                            Log.Error($"{threadPrefix}Could not delete temporary file {firstName} after failure to create backup:\n{e}");
                        }
                    else
                        try
                        {
                            File.Move(file.FullName, name, true);
                        }
                        catch (Exception e)
                        {
                            Log.Error($"{threadPrefix}Could not move temporary file {firstName} to {name} after writing backup:\n{e}");
                            if (backup is not null)
                                try
                                {
                                    File.Move(backup, name, true);
                                }
                                catch (Exception e2)
                                {
                                    Log.Error(
                                        $"{threadPrefix}Could not move backup file {backup} back to {name} after failing to move temporary file:\n{e2}");
                                }

                            try
                            {
                                File.Delete(firstName);
                            }
                            catch (Exception e2)
                            {
                                Log.Error(
                                    $"{threadPrefix}Could not delete temporary file {firstName} after failing to move it to {name}:\n{e2}");
                            }
                        }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{threadPrefix}Could not save {typeName} {logName}:\n{ex}");
            }
        }
    }

    /// <summary> Immediately trigger a save and wait for the save to complete. </summary>
    /// <param name="value"> The file to save. </param>
    /// <remarks> This will also finish all previously triggered file saves and prevent queueing of new ones until it is done. </remarks>
    public void ImmediateSaveSync<TSavable>(in TSavable value) where TSavable : ISavable<T>
    {
        ImmediateSave(value);
        lock (_saveTaskLock)
        {
            _saveTask?.Wait();
        }
    }

    /// <summary> Immediately delete a file on the service's file thread. </summary>
    /// <param name="value"> The file to delete. </param>
    public void ImmediateDelete<TSavable>(in TSavable value) where TSavable : ISavable<T>
    {
        var name     = value.ToFilePath(FileNames);
        var typeName = value.GetType().Name;
        var logName  = value.LogName(name);
        lock (_saveTaskLock)
        {
            _saveTask = _saveTask is null || _saveTask.IsCompleted
                ? Task.Run(DeleteAction)
                : _saveTask.ContinueWith(_ => DeleteAction(), TaskScheduler.Default);
        }

        return;

        void DeleteAction()
        {
            var threadPrefix = GetThreadPrefix();
            try
            {
                if (name.Length is 0)
                    throw new Exception("Invalid object returned empty filename.");

                if (!File.Exists(name))
                    return;

                Log.Information($"{threadPrefix}Deleting {typeName} {logName}...");
                if (BackupMode is BackupMode.NoBackups && CreateBackup(Log, BackupMode, threadPrefix, typeName, logName, name, out _))
                    File.Delete(name);
            }
            catch (Exception ex)
            {
                Log.Error($"{threadPrefix}Could not delete {typeName} {logName}:\n{ex}");
            }
        }
    }

    /// <summary> Immediately delete a file on the service's file thread and wait for the deletion to complete. </summary>
    /// <param name="value"> The file to delete. </param>
    /// <remarks> This will also finish all previously triggered file saves and deletes and prevent queueing of new ones until it is done. </remarks>
    public void ImmediateDeleteSync<TSavable>(in TSavable value) where TSavable : ISavable<T>
    {
        ImmediateDelete(value);
        lock (_saveTaskLock)
        {
            _saveTask?.Wait();
        }
    }
}
