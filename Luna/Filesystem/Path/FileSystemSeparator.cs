namespace Luna;

internal sealed class FileSystemSeparator(FileSystemIdentifier identifier)
    : FileSystemNode(identifier), IFileSystemSeparator
{
    public override string         FullPath     { get; internal set; } = string.Empty;
    public          long           CreationDate { get; internal set; }
    public          ColorParameter Color        { get; internal set; }

    public bool IsFolder;

    /// <inheritdoc/>
    public override bool BehavesLikeFolder
        => IsFolder;
}
