namespace Luna;

/// <summary> An interface that needs to be implemented by types that are used as value types for a file system. </summary>
public interface IFileSystemValue
{
    /// <summary> Get the actual display name for this value. </summary>
    public string DisplayName { get; }

    /// <summary> The full path to use for the data node. </summary>
    public DataPath Path { get; }

    /// <summary> A unique identifier for this value. </summary>
    public string Identifier { get; }

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
