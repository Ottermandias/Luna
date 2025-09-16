namespace Luna;

/// <summary> Triggered whenever the specific file system changes. </summary>
/// <param name="name"> The name of the event. </param>
/// <param name="log"> A logger. </param>
public sealed class FileSystemChanged(string name, Logger log) : EventBase<FileSystemChanged.Arguments, uint>(name, log)
{
    /// <summary> Arguments for a FileSystemChanged event. </summary>
    /// <param name="Type"> The type of change. </param>
    /// <param name="ChangedObject"> The node that was changed. </param>
    /// <param name="PreviousParent"> The prior parent of the moved node, if any. </param>
    /// <param name="NewParent"> The new parent of the moved node, if any. </param>
    public readonly record struct Arguments(
        FileSystemChangeType Type,
        IFileSystemNode ChangedObject,
        IFileSystemFolder? PreviousParent,
        IFileSystemFolder? NewParent);
}
