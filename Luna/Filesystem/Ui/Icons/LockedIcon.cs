namespace Luna;

/// <summary> A locked icon to show in a file system drawer. </summary>
/// <param name="fileSystem"> The parent file system. </param>
public readonly struct LockedIcon(BaseFileSystem fileSystem) : IStatusIcon<AwesomeIcon, IFileSystemNode>
{
    /// <inheritdoc/>
    public bool Visible(in IFileSystemNode data)
        => data.Locked;

    /// <inheritdoc/>
    public AwesomeIcon Icon(in IFileSystemNode data)
        => LunaStyle.LockedIcon;

    /// <inheritdoc/>
    public void OnClick(in IFileSystemNode data)
        => fileSystem.ChangeLockState(data, false);

    /// <inheritdoc/>
    public Vector4 HoveredColor(in IFileSystemNode data)
        => Im.Style[ImGuiColor.TextDisabled];
}
