namespace Luna;

/// <summary> Provides a button to delete the given separator. </summary>
/// <param name="fileSystem"> The parent file system. </param>
public sealed class SeparatorDeleteButton(BaseFileSystem fileSystem) : BaseButton<IFileSystemSeparator>
{
    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label(in IFileSystemSeparator data)
        => "Delete Separator"u8;

    /// <inheritdoc/>
    public override void OnClick(in IFileSystemSeparator data)
        => fileSystem.Delete(data);
}
