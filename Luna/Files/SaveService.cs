namespace Luna.Files;

/// <summary> A service to simplify file-saving and deleting across the application. </summary>
/// <typeparam name="T"> The type of the provider of file paths for this service. </typeparam>
/// <param name="log"> The logger to use. </param>
/// <param name="framework"> The framework event handler to use. </param>
/// <param name="fileNames"> The file path provider for saving <see cref="ISavable{T}"/>. </param>
/// <param name="awaiter"> An optional awaiter that is waited before any file writes can take place. </param>
public abstract class BaseSaveService<T>(Logger log, FrameworkManager framework, T fileNames, Task? awaiter = null) : IDisposable
    where T : BaseFilePathProvider
{
    /// <summary> The default delay when using <see cref="SaveType.Delay"/> without specifying a custom delay. </summary>
#if DEBUG
    private static readonly TimeSpan StandardDelay = TimeSpan.FromSeconds(2);
#else
    private static readonly TimeSpan StandardDelay = TimeSpan.FromSeconds(10);
#endif

    /// <summary> The logger to use. </summary>
    protected readonly Logger Log = log;

    /// <summary> The framework event handler to use. </summary>
    protected readonly FrameworkManager Framework = framework;

    /// <summary> The provider of file paths for this service. </summary>
    public readonly T FileNames = fileNames;

    /// <summary> The currently running save task, if any. </summary>
    private Task? _saveTask = awaiter;

    /// <summary> A lock to handle appending to the save task. </summary>
    private readonly Lock _saveTaskLock = new();

    /// <summary> Save an object according to type. </summary>
    /// <param name="type"> The save type to invoke. </param>
    /// <param name="value"> The file to save. </param>
    public void Save(SaveType type, in ISavable<T> value)
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
    public void QueueSave(ISavable<T> value)
    {
        var file = value.ToFilePath(FileNames);
        Framework.RegisterOnTick($"{value.GetType().Name} ## {file}", () => { ImmediateSave(value); });
    }

    /// <summary> Queue a delayed file save with the standard delay after the delay is over. </summary>
    /// <param name="value"> The file to save. </param>
    public void DelaySave(ISavable<T> value)
        => DelaySave(value, StandardDelay);

    /// <summary> Queue a delayed file save for after the delay is over. </summary>
    /// <param name="value"> The file to save. </param>
    /// <param name="delay"> The custom delay to wait before actually triggering the save. </param>
    public void DelaySave(ISavable<T> value, TimeSpan delay)
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
    public void ImmediateSave(ISavable<T> value)
    {
        var name = value.ToFilePath(FileNames);
        // Lock the object before replacing it.
        // We only want one thread saving files at the same time.
        lock (_saveTaskLock)
        {
            _saveTask = _saveTask == null || _saveTask.IsCompleted
                ? Task.Run(SaveAction)
                : _saveTask.ContinueWith(_ => SaveAction(), TaskScheduler.Default);
        }

        return;

        void SaveAction()
        {
            try
            {
                if (name.Length == 0)
                    throw new Exception("Invalid object returned empty filename.");

                // Check if the file written to already exists, and if it does, write to a temporary file first.
                var secureWrite = File.Exists(name);
                var firstName   = secureWrite ? name + ".tmp" : name;
                Log.Debug(
                    $"{GetThreadPrefix()}Saving {value.TypeName} {value.LogName(name)} {(secureWrite ? "using secure write" : "for the first time")}...");
                var file = new FileInfo(firstName);

                // Create all required directories to write the file.
                file.Directory?.Create();

                // Open the new or temporary file as a stream and write to it.
                using (var s = file.Exists ? file.Open(FileMode.Truncate) : file.Open(FileMode.CreateNew))
                {
                    using var w = new StreamWriter(s, Encoding.UTF8);
                    value.Save(w);
                }

                // If we wrote to a temporary file, move the fully written file to replace the original file when done.
                if (secureWrite)
                    File.Move(file.FullName, name, true);
            }
            catch (Exception ex)
            {
                Log.Error($"{GetThreadPrefix()}Could not save {value.GetType().Name} {value.LogName(name)}:\n{ex}");
            }
        }
    }

    /// <summary> Immediately trigger a save and wait for the save to complete. </summary>
    /// <param name="value"> The file to save. </param>
    /// <remarks> This will also finish all previously triggered file saves and prevent queueing of new ones until it is done. </remarks>
    public void ImmediateSaveSync(ISavable<T> value)
    {
        ImmediateSave(value);
        lock (_saveTaskLock)
        {
            _saveTask?.Wait();
        }
    }

    /// <summary> Immediately delete a file on the service's file thread. </summary>
    /// <param name="value"> The file to delete. </param>
    public void ImmediateDelete(ISavable<T> value)
    {
        var name = value.ToFilePath(FileNames);
        lock (_saveTaskLock)
        {
            _saveTask = _saveTask == null || _saveTask.IsCompleted
                ? Task.Run(DeleteAction)
                : _saveTask.ContinueWith(_ => DeleteAction(), TaskScheduler.Default);
        }

        return;

        void DeleteAction()
        {
            try
            {
                if (name.Length == 0)
                    throw new Exception("Invalid object returned empty filename.");

                if (!File.Exists(name))
                    return;

                Log.Information($"{GetThreadPrefix()}Deleting {value.GetType().Name} {value.LogName(name)}...");
                File.Delete(name);
            }
            catch (Exception ex)
            {
                Log.Error($"{GetThreadPrefix()}Could not delete {value.GetType().Name} {value.LogName(name)}:\n{ex}");
            }
        }
    }

    /// <summary> Immediately delete a file on the service's file thread and wait for the deletion to complete. </summary>
    /// <param name="value"> The file to delete. </param>
    /// <remarks> This will also finish all previously triggered file saves and deletes and prevent queueing of new ones until it is done. </remarks>
    public void ImmediateDeleteSync(ISavable<T> value)
    {
        ImmediateDelete(value);
        lock (_saveTaskLock)
        {
            _saveTask?.Wait();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetThreadPrefix()
        => $"[{Environment.CurrentManagedThreadId}] ";
}
