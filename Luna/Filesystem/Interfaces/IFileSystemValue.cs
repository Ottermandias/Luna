namespace Luna;

/// <summary> An interface that needs to be implemented by types that are used as value types for a file system. </summary>
public interface IFileSystemValue
{
    /// <summary> The full path to use for the data node. </summary>
    public string FullPath { get; set; }

    /// <summary> The file system node containing this value, if any. </summary>
    public IFileSystemData? Node { get; set; }
}

/// <summary> An interface that needs to be implemented by types that are used as value types for a file system. </summary>
public interface IFileSystemValue<TSelf> : IFileSystemValue
    where TSelf : class, IFileSystemValue<TSelf>
{
    /// <summary> The file system node containing this value, if any. </summary>
    public new IFileSystemData<TSelf>? Node { get; set; }

    /// <inheritdoc/>
    IFileSystemData? IFileSystemValue.Node
    {
        get => Node;
        set => Node = value as IFileSystemData<TSelf>;
    }
}
