namespace Luna;

/// <summary> A read-only interface representing a data-containing object in the file system. </summary>
public interface IFileSystemData : IFileSystemNode
{
    /// <summary> The data object contained by the leaf. </summary>
    public IFileSystemValue Value { get; }

    /// <summary> Get <see cref="Value"/> as a specific type, if it matches. </summary>
    public T? GetValue<T>() where T : class
        => Value as T;
}

/// <summary> A read-only interface representing a data-containing object of specific type in the file system. </summary>
public interface IFileSystemData<out T> : IFileSystemData
    where T : class, IFileSystemValue<T>
{
    /// <summary> The data object contained by the leaf with its concrete type. </summary>
    public new T Value { get; }

    /// <inheritdoc/>
    IFileSystemValue IFileSystemData.Value
        => Value;
}
