namespace Luna;

/// <summary> Possible changes that can happen to a file system. </summary>
public enum FileSystemChangeType
{
    /// <summary> An arbitrary object was renamed without being moved. </summary>
    ObjectRenamed,

    /// <summary> An arbitrary object was deleted. </summary>
    ObjectRemoved,

    /// <summary> A new, empty folder was added. </summary>
    FolderAdded,

    /// <summary> A new non-folder object was added. </summary>
    DataAdded,

    /// <summary> An arbitrary object was moved. </summary>
    ObjectMoved,

    /// <summary> A folder was fully merged into another folder. </summary>
    FolderMerged,

    /// <summary> A folder was partially merged into another folder. </summary>
    PartialMerge,

    /// <summary> The filesystem is being reloaded. </summary>
    ReloadStarting,

    /// <summary> The filesystem was reloaded completely. </summary>
    Reload,

    /// <summary> The <see cref="PathFlags.Locked"/> state of an object was changed. </summary>
    LockedChange,

    /// <summary> The <see cref="PathFlags.Expanded"/> state of an object was changed. </summary>
    ExpandedChange,

    /// <summary> The <see cref="PathFlags.Selected"/> state of an object was changed. </summary>
    SelectedChange,

    /// <summary> The <see cref="IFileSystemFolder.FilterExpanded"/> state of a folder was changed. </summary>
    FilterExpandedChange,
}
