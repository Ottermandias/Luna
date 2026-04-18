namespace Luna;

/// <summary> Provides a button to toggle sorting as a folder on or off. </summary>
/// <param name="fileSystem"> The parent file system. </param>
public sealed class SeparatorSortAsFolderButton(BaseFileSystem fileSystem) : BaseButton<IFileSystemSeparator>
{
    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label(in IFileSystemSeparator data)
        => data.BehavesLikeFolder ? "Sort As a Data Entry"u8 : "Sort As a Folder"u8;

    /// <inheritdoc/>
    public override void OnClick(in IFileSystemSeparator data)
        => fileSystem.ChangeSeparator(data, !data.BehavesLikeFolder);
}
