namespace Luna;

/// <summary> A listing for a file to be backed up with all necessary functions. </summary>
public interface IBackupFile
{
    /// <summary> Get whether the file to be backed up exists. </summary>
    public bool Exists { get; }

    /// <summary> Get the full path to the file to be backed up. </summary>
    public string Path { get; }

    /// <summary> Check whether the content of the file to be backed up is bytewise identical to the content provided by the stream. </summary>
    /// <param name="other"> The file to compare against. </param>
    /// <returns> True if the file is bytewise identical to the stream. </returns>
    public bool Equals(Stream other);

    /// <summary> Create a new entry for the file in the given zip archive. </summary>
    /// <param name="archive"> The archive to add an entry to. </param>
    /// <param name="rootDirectory"> The root directory, relative to which the entry path in the archive is stored. </param>
    public void CreateEntry(ZipArchive archive, string rootDirectory);

    /// <summary> Compare two streams per byte and return if they are equal. </summary>
    [SkipLocalsInit]
    public static unsafe bool Equals(Stream lhs, Stream rhs)
    {
        const int  bufferSize = 1024;
        Span<byte> bufferLhs  = stackalloc byte[bufferSize];
        Span<byte> bufferRhs  = stackalloc byte[bufferSize];
        while (true)
        {
            var bytesLhs = lhs.ReadAtLeast(bufferLhs, bufferSize, false);
            var bytesRhs = rhs.ReadAtLeast(bufferRhs, bufferSize, false);
            if (bytesLhs != bytesRhs || !bufferLhs.SequenceEqual(bufferRhs))
                return false;
            if (bytesLhs < bufferSize)
                return true;
        }
    }
}

/// <summary> A default listing for files to be backed up. </summary>
/// <param name="path"> The full path to the file to be backed up. </param>
public sealed class DefaultBackupFile(string path) : IBackupFile
{
    /// <inheritdoc/>
    public bool Exists
        => File.Exists(path);

    /// <inheritdoc/>
    public string Path
        => path;

    /// <inheritdoc/>
    public bool Equals(Stream other)
    {
        using var currentData = File.Open(Path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return IBackupFile.Equals(currentData, other);
    }

    /// <inheritdoc/>
    public void CreateEntry(ZipArchive archive, string rootDirectory)
        => archive.CreateEntryFromFile(Path, System.IO.Path.GetRelativePath(rootDirectory, Path), CompressionLevel.Optimal);
}
