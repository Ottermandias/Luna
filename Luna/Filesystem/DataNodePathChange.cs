namespace Luna;

/// <summary> Triggered when the full path of a data node is changed. </summary>
/// <param name="name"> The name of the event. </param>
/// <param name="log"> A logger. </param>
public sealed class DataNodePathChange(string name, Logger log) : EventBase<DataNodePathChange.Arguments, uint>(name, log)
{
    /// <summary> Arguments for a DataNodePathChange event. </summary>
    /// <param name="ChangedNode"> The node that has been changed. </param>
    /// <param name="OldPath"> The path before the change. </param>
    public readonly record struct Arguments(IFileSystemData ChangedNode, string OldPath);
}
