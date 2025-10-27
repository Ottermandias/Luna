namespace Luna;

/// <summary> The base for an automatic backup service. </summary>
/// <typeparam name="TFilePathProvider"> The file path provider. </typeparam>
public abstract class BaseBackupService<TFilePathProvider> : IAsyncService, IDisposable
    where TFilePathProvider : BaseFilePathProvider
{
    protected readonly Logger                  Log;
    protected readonly TFilePathProvider       Provider;
    protected readonly CancellationTokenSource Cancel = new();

    /// <summary> Create an automatic backup. </summary>
    protected BaseBackupService(Logger log, TFilePathProvider provider)
    {
        Log      = log;
        Provider = provider;
        var files = Provider.GetBackupFiles();
        if (files.Count > 0)
            Awaiter = Task.Run(() => Backup.CreateAutomaticBackup(Log, new DirectoryInfo(provider.ConfigurationDirectory), files, Cancel.Token),
                Cancel.Token);
        else
            Awaiter = Task.CompletedTask;
    }

    /// <summary> Generate a named, permanent backup of the current file state. </summary>
    /// <param name="name"> The name to use for the backup. </param>
    /// <param name="additionalFiles"> Additional files to add to the migration backup that may not be in the backup files anymore. </param>
    public virtual void CreateMigrationBackup(string name, params IEnumerable<string> additionalFiles)
        => Backup.CreatePermanentBackup(Log, new DirectoryInfo(Provider.ConfigurationDirectory), Provider.GetBackupFiles().Concat(additionalFiles.Select(s => new FileInfo(s))).ToList(), name);

    /// <inheritdoc/>
    public Task Awaiter { get; }

    /// <inheritdoc/>
    public bool Finished
        => Awaiter.IsCompletedSuccessfully;

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~BaseBackupService()
        => Dispose(false);

    protected virtual void Dispose(bool disposing)
    {
        Cancel.Cancel();
        Cancel.Dispose();
        Awaiter.Dispose();
    }
}
