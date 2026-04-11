namespace Luna;

/// <summary> A separator dummy for a file system rendering. </summary>
public interface IFileSystemSeparator : IFileSystemNode
{
    /// <summary> The color to render the separator in. If this is default, the tree line color will be used. </summary>
    public ColorParameter Color { get; }

    /// <summary> An optional creation date timestamp for separators. </summary>
    public long CreationDate { get; }
}
