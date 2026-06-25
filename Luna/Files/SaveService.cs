namespace Luna;

/// <summary> The available strategies for creating backup files. </summary>
public enum BackupMode
{
    /// <summary> Files are deleted or overwritten without backing them up in any way. </summary>
    NoBackups = 0,

    /// <summary> Any file that is deleted or would be overwritten is moved to [filename].[extension].bak. This file is overwritten if it exists. </summary>
    SingleBackup = 1,

    /// <summary> Any file that is deleted or would be overwritten is moved to [filename]_[yyyyMMddHHmmssfff].[extension].bak. These files are overwritten if a collision occurs. </summary>
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

    /// <summary> The number of backup files to keep per original file in <see cref="BackupMode.TimestampedBackup"/> mode. </summary>
    /// <remarks> If this is <see cref="int.MaxValue"/> or less than 1 it is treated as unlimited. </remarks>
    public int BackupLimit { get; set; } = 3;

    /// <summary> The logger to use. </summary>
    public readonly LunaLogger Log = log;

    /// <summary> The framework event handler to use. </summary>
    protected readonly FrameworkManager Framework = framework;

    /// <inheritdoc cref="AtomicWriteWithBackup(LunaLogger,string,Action{string},BackupMode,int)"/>
    public void AtomicWriteWithBackup(string filePath, Action<string> writeFile)
        => AtomicWriteWithBackup(Log, filePath, writeFile, BackupMode, BackupLimit);

    /// <inheritdoc cref="DeleteWithBackup(LunaLogger,string,BackupMode,int)"/>
    public void DeleteWithBackup(string filePath)
        => DeleteWithBackup(Log, filePath, BackupMode, BackupLimit);

    /// <summary> Delete all files ending in .bak in a given directory and its subdirectories. </summary>
    /// <param name="log"> The logger to use. </param>
    /// <param name="directoryPath"> The path to the topmost directory from which to search. </param>
    /// <param name="predicate"> An optional predicate. If this is passed, only backup files for which it returns true are deleted. </param>
    public static void CleanAllBackups(LunaLogger log, string directoryPath, Func<string, bool>? predicate = null)
    {
        var deleted = 0;
        var failed  = 0;
        var skipped = 0;
        if (!Directory.Exists(directoryPath))
            return;

        foreach (var file in Directory.EnumerateFiles(directoryPath, "*.bak", SearchOption.AllDirectories))
        {
            try
            {
                if (predicate?.Invoke(file) ?? true)
                {
                    File.Delete(file);
                    ++deleted;
                    log.Verbose($"Deleted backup file {file} during cleanup.");
                }
                else
                {
                    ++skipped;
                    log.Verbose($"Skipped deleting backup file {file} during cleanup due to predicate.");
                }
            }
            catch (Exception ex)
            {
                ++failed;
                log.Error($"Failed to delete backup file {file}:\n{ex}");
            }
        }

        log.Information(
            $"Cleanup of backup files found {deleted + failed + skipped} backup files, deleted {deleted}, skipped {skipped}, and failed to delete {failed} of them.");
    }

    /// <summary> Safely move a file to a backup location or delete it, depending on mode. </summary>
    /// <param name="log"> The logger to use. </param>
    /// <param name="filePath"> The file to delete or move. </param>
    /// <param name="mode"> The backup mode to use. </param>
    /// <param name="backupLimit"> The number of backups of this file to keep if using <see cref="BackupMode.TimestampedBackup"/>. Older backups will be deleted when this is surpassed. </param>
    public static void DeleteWithBackup(LunaLogger log, string filePath, BackupMode mode = BackupMode.NoBackups, int backupLimit = 3)
    {
        if (filePath.Length is 0)
            return;

        if (!File.Exists(filePath))
            return;

        var threadPrefix = GetThreadPrefix();
        try
        {
            log.Information($"{threadPrefix}Deleting {filePath}...");
            // NoBackups deletes the file, while it is moved in CreateBackup and thus does not need to be deleted.
            if (mode is BackupMode.NoBackups)
                File.Delete(filePath);
            else
                CreateBackup(log, mode, backupLimit, threadPrefix, "file", filePath, filePath, out _);
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
    /// <param name="backupLimit"> The number of backups of this file to keep if using <see cref="BackupMode.TimestampedBackup"/>. Older backups will be deleted when this is surpassed. </param>
    /// <remarks> The delegate must fully write and flush the temporary file if durability against process or system failure is required. </remarks>
    public static void AtomicWriteWithBackup(LunaLogger log, string filePath, Action<string> writeFile,
        BackupMode mode = BackupMode.TimestampedBackup, int backupLimit = 3)
    {
        if (filePath.Length is 0)
            throw new ArgumentException($"{filePath} can not be empty.", nameof(filePath));

        var exists       = File.Exists(filePath);
        var tmpPath      = GetTempPath(filePath);
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
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    directoryCreated = true;
                }
            }

            writeFile(tmpPath);
        }
        catch (Exception ex)
        {
            log.Error($"{threadPrefix}Failed to write temporary file {tmpPath}:\n{ex}");
            if (File.Exists(tmpPath))
                try
                {
                    File.Delete(tmpPath);
                }
                catch (Exception ex2)
                {
                    log.Error($"{threadPrefix}Failed to delete temporary file {tmpPath} after failure to write file:\n{ex2}");
                }

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

        if (exists)
        {
            // Then move the existing data to a backup, if expected.
            // For stability, we use a guaranteed backup path for File.Replace, then delete if no backup should be kept.
            var backupPath       = GetBackupName(mode, filePath);
            var actualBackupPath = backupPath ?? filePath + $"_temp_{Guid.NewGuid():N}.bak";
            if (ReplaceSafely(log, tmpPath, filePath, actualBackupPath, threadPrefix, backupPath is null))
                CleanBackups(log, mode, backupLimit, threadPrefix, filePath);
        }
        else
        {
            MoveSafely(log, tmpPath, filePath, threadPrefix);
        }
    }


    /// <returns> True if <see cref="BackupMode.NoBackups"/> is used or the backup was successful, false otherwise. </returns>
    protected static bool CreateBackup(LunaLogger log, BackupMode mode, int keptBackups, string threadPrefix, string typeName, string logName,
        string name, out string? backupName)
    {
        backupName = GetBackupName(mode, name);
        if (backupName is null)
            return true;

        try
        {
            log.Debug($"{threadPrefix}Backing up existing {typeName} {logName}...");
            File.Move(name, backupName, true);
            CleanBackups(log, mode, keptBackups, threadPrefix, name);
            return true;
        }
        catch (Exception ex)
        {
            log.Error($"{threadPrefix}Unable to create backup for {name} at {backupName} before overwriting:\n{ex}");
            return false;
        }
    }

    /// <summary> Clean all older timestamped backup files if there are too many. </summary>
    /// <param name="log"> The logger to use. </param>
    /// <param name="mode"> The backup mode to use. If this is <see cref="BackupMode.NoBackups"/>, nothing is done. </param>
    /// <param name="keptBackups"> The number of concurrent backups of a specific file to keep. </param>
    /// <param name="threadPrefix"> The thread prefix for the log. </param>
    /// <param name="name"> The full original file path of the backed up file. </param>
    protected static void CleanBackups(LunaLogger log, BackupMode mode, int keptBackups, string threadPrefix, string name)
    {
        if (mode is not BackupMode.TimestampedBackup)
            return;

        if (keptBackups is int.MaxValue or <= 0)
            return;

        var directory = Path.GetDirectoryName(name);
        if (string.IsNullOrEmpty(directory))
            directory = ".";

        var pattern = $"{Path.GetFileNameWithoutExtension(name.AsSpan())}_*{Path.GetExtension(name.AsSpan())}.bak";
        var files   = Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly).Order(StringComparer.Ordinal).ToList();
        var cleanup = files.Count - keptBackups;
        if (cleanup <= 0)
            return;

        log.Debug(
            $"{threadPrefix}Found {files.Count} backups for {name} with {keptBackups} backups to be kept, cleaning up {cleanup}...");
        foreach (var file in files.Take(cleanup))
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception ex)
            {
                log.Error($"{threadPrefix}Failed to delete surplus backup of {name} at {file}:\n{ex}");
            }
        }
    }

    /// <summary> Atomically replace the file at <paramref name="targetPath"/> with <paramref name="tmpPath"/>, while writing it to <paramref name="actualBackupPath"/>. </summary>
    /// <param name="log"> The logger to use. </param>
    /// <param name="tmpPath"> The path to the temporary file supposed to overwrite the target. </param>
    /// <param name="targetPath"> The path to the target to be overwritten. </param>
    /// <param name="actualBackupPath"> The path to a backup file the target is moved to. </param>
    /// <param name="threadPrefix"> The thread prefix for the logger. </param>
    /// <param name="temporary"> Whether the <paramref name="actualBackupPath"/> should be deleted after successful replacement. </param>
    /// <returns> True if the file was successfully replaced. </returns>
    /// <remarks>
    ///   If the backup is temporary and the deletion fails, this will be logged but still return true as the replacement was successful. <br/>
    ///   If the replacement fails, this will try to delete the temporary file and log this, regardless of success or error.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static bool ReplaceSafely(LunaLogger log, string tmpPath, string targetPath, string actualBackupPath, string threadPrefix,
        bool temporary)
    {
        log.Debug(
            $"{threadPrefix}Replacing {targetPath} with {tmpPath}, using {(temporary ? "temporary" : "permanent")} backup at {actualBackupPath}...");
        try
        {
            File.Replace(tmpPath, targetPath, actualBackupPath, true);
            if (!temporary)
                return true;

            try
            {
                File.Delete(actualBackupPath);
            }
            catch (Exception ex)
            {
                log.Error($"{threadPrefix}Failed to delete temporary backup {actualBackupPath} after file replacement:\n{ex}");
            }

            return true;
        }
        catch (Exception ex)
        {
            log.Error($"{threadPrefix}Failed to replace {targetPath} with {tmpPath}:\n{ex}");
            try
            {
                File.Delete(tmpPath);
            }
            catch (Exception e2)
            {
                log.Error($"{threadPrefix}Failed to delete temporary file {tmpPath} after failing to replace {targetPath}:\n{e2}");
            }

            return false;
        }
    }

    /// <summary> Move a temporary file to its non-existent destination. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static bool MoveSafely(LunaLogger log, string tmpPath, string filePath, string threadPrefix)
    {
        try
        {
            File.Move(tmpPath, filePath, false);
            return true;
        }
        catch (Exception ex)
        {
            log.Error($"{threadPrefix}Failed to move temporary file {tmpPath} to first-time target {filePath}:\n{ex}");
            try
            {
                File.Delete(tmpPath);
            }
            catch (Exception ex2)
            {
                log.Error($"{threadPrefix}Failed to delete temporary file {tmpPath} after failing to move it to target {filePath}:\n{ex2}");
            }
        }

        return false;
    }

    /// <summary> Get the log prefix of the current thread ID. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static string GetThreadPrefix()
        => $"[{Environment.CurrentManagedThreadId}] ";

    /// <summary> Get the appropriate name for a backup file given a file path and the backup mode. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static string? GetBackupName(BackupMode mode, string original)
        => mode switch
        {
            BackupMode.NoBackups         => null,
            BackupMode.SingleBackup      => original + ".bak",
            BackupMode.TimestampedBackup => GetTimeBackup(original),
            _                            => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
        };

    /// <summary> Create a temporary file path for a given path. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static string GetTempPath(string path)
        => $"{path}_{Guid.NewGuid():N}.tmp";

    /// <summary> Get a backup path with the current timestamp. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static string GetTimeBackup(string original)
    {
        var extension = Path.GetExtension(original.AsSpan());
        var path      = original.AsSpan(0, original.Length - extension.Length);
        return $"{path}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{extension}.bak";
    }
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
            default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
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
        => Flush();

    /// <summary> Flush all currently queued saves. </summary>
    /// <remarks> Save and delete actions must not synchronously call back into this service. </remarks>
    public virtual void Flush()
    {
        lock (_saveTaskLock)
        {
            try
            {
                _saveTask?.Wait();
            }
            catch (Exception ex)
            {
                Log.Error($"Failure in the save task:\n{ex}");
                throw;
            }

            _saveTask = null;
        }
    }

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
            var tmpPath      = GetTempPath(name);
            var createdFile  = false;
            try
            {
                if (name.Length is 0)
                    throw new Exception("Invalid object returned empty filename.");

                var fileExisted = File.Exists(name);
                Log.Debug($"{threadPrefix}Saving {typeName} {logName} {(fileExisted ? "using secure write" : "for the first time")}...");
                var file = new FileInfo(tmpPath);

                // Create all required directories to write the file.
                file.Directory?.Create();

                // Open the new or temporary file as a stream and write to it.
                using (var s = file.Open(FileMode.CreateNew))
                {
                    createdFile = true;
                    value.Save(s);
                    s.Flush(true);
                }

                if (fileExisted)
                {
                    // If we wrote to a temporary file, move the fully written file to replace the original file when done.
                    var backupPath       = GetBackupName(BackupMode, name);
                    var actualBackupPath = backupPath ?? name + $"_temp_{Guid.NewGuid():N}.bak";
                    if (ReplaceSafely(Log, tmpPath, name, actualBackupPath, threadPrefix, backupPath is null))
                        CleanBackups(Log, BackupMode, BackupLimit, threadPrefix, name);
                }
                else
                {
                    MoveSafely(Log, tmpPath, name, threadPrefix);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{threadPrefix}Could not save {typeName} {logName}:\n{ex}");
                if (createdFile && File.Exists(tmpPath))
                    try
                    {
                        File.Delete(tmpPath);
                    }
                    catch (Exception ex2)
                    {
                        Log.Error($"{threadPrefix}Could not delete temporary {typeName} file after failing to save:\n{ex2}");
                    }
            }
        }
    }

    /// <summary> Immediately trigger a save and wait for the save to complete. </summary>
    /// <param name="value"> The file to save. </param>
    /// <remarks> This will also finish all previously triggered file saves and prevent queueing of new ones until it is done. </remarks>
    public void ImmediateSaveSync<TSavable>(in TSavable value) where TSavable : ISavable<T>
    {
        ImmediateSave(value);
        Flush();
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
                if (BackupMode is BackupMode.NoBackups)
                    File.Delete(name);
                else
                    CreateBackup(Log, BackupMode, BackupLimit, threadPrefix, typeName, logName, name, out _);
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
        Flush();
    }
}
