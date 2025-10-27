namespace Luna;

/// <summary> A container for the paths stored in file system values. </summary>
public sealed class DataPath
{
    /// <summary> The actual full path as used by the file system. </summary>
    public string CurrentPath = string.Empty;

    /// <summary> The containing folder as saved to disk, should be updated when moved. </summary>
    public string Folder = string.Empty;

    /// <summary> The sort name for the value as saved on disk. If this is null, the display name of the value is used. Should be updated when renamed. </summary>
    public string? SortName = null;

    /// <summary> Get whether the data path is a defaulted path, i.e. not in any folder and no defined sort name. </summary>
    public bool IsDefault
        => Folder.Length is 0 && SortName is null;

    /// <summary> Update the <see cref="Folder"/> and <see cref="SortName"/> properties of a node's value by its <see cref="CurrentPath"/>. </summary>
    /// <param name="system"> The file system this node lives in, required for comparisons. </param>
    /// <param name="node"> The data node to update. </param>
    /// <returns> True if <see cref="Folder"/> or <see cref="SortName"/> have changed. </returns>
    public bool UpdateByNode(BaseFileSystem system, IFileSystemData node)
    {
        if (node.IsRoot)
            return false;

        // Handle the folder.
        var ret = false;
        if (node.Parent!.IsRoot)
        {
            ret    = Folder.Length is not 0;
            Folder = string.Empty;
        }
        else
        {
            ret    = !system.Equal(Folder, node.Parent.FullPath);
            Folder = node.Parent.FullPath;
        }

        // Handle empty names.
        var name = node.Name;
        if (name is "<None>")
        {
            if (node.Value.DisplayName.Length is 0)
            {
                ret      |= SortName is not null;
                SortName =  null;
                return ret;
            }

            if (SortName?.Length is 0)
                return ret;

            SortName = string.Empty;
            return true;
        }

        // Handle non-duplicate names first, so that manually duplicated names work.
        if (name.Equals(node.Value.DisplayName, StringComparison.Ordinal))
        {
            ret      |= SortName is not null;
            SortName =  null;
            return ret;
        }

        if (SortName is not null && name.Equals(SortName, StringComparison.Ordinal))
            return ret;

        // Handle duplicate names.
        if (name.IsDuplicateName(out var baseName, out var number))
        {
            if (baseName.Equals(node.Value.DisplayName, StringComparison.Ordinal))
            {
                ret      |= SortName is not null;
                SortName =  null;
                return ret;
            }

            if (SortName is not null && baseName.Equals(SortName, StringComparison.Ordinal))
                return ret;

            SortName = baseName.ToString();
            return true;
        }

        SortName = name.ToString();
        return true;
    }

    /// <summary> Get the intended full path based on the current <see cref="SortName"/> and <see cref="Folder"/>. </summary>
    /// <param name="displayName"> The display name to use when <see cref="SortName"/> is unset. </param>
    /// <returns> The default full path without considering duplicates. </returns>
    public string GetIntendedPath(string displayName)
    {
        if (Folder.Length is 0)
            return GetIntendedName(displayName);

        return $"{Folder}/{GetIntendedName(displayName)}";
    }

    /// <summary> Get the intended node name. </summary>
    /// <param name="displayName"> The display name to use when <see cref="SortName"/> is unset. </param>
    /// <returns> The node name without considering duplicates. </returns>
    public string GetIntendedName(string displayName)
        => SortName ?? displayName.FixName();
}
