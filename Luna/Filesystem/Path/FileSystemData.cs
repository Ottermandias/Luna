namespace Luna;

/// <summary> A basic shared data node for a specified data type that can be associated with it. </summary>
/// <inheritdoc cref="FileSystemNode"/>
/// <typeparam name="TValue"> The type of the value, which needs to be able to be associated with this node. </typeparam>
/// <param name="value"> The value contained by this node. </param>
internal sealed class FileSystemData<TValue>(FileSystemIdentifier identifier, TValue value)
    : FileSystemNode(identifier), IFileSystemData<TValue> where TValue : class, IFileSystemValue<TValue>
{
    /// <summary> The value contained by this node. </summary>
    public TValue Value { get; } = value;

    /// <inheritdoc/>
    public override string FullPath
    {
        get => Value.Path.CurrentPath;
        internal set => Value.Path.CurrentPath = value;
    }
}
