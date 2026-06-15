namespace Luna;

/// <summary> Extensions for files and the file system. </summary>
public static class FileExtensions
{
    /// <summary> Recursively enumerate all non-hidden files. </summary>
    public static List<FileInfo> EnumerateNonHiddenFiles(this DirectoryInfo topDir)
        => EnumerateNonHiddenFilesFiltered(topDir, null);

    /// <summary> Recursively enumerate all non-hidden files that fulfill the given filter. </summary>
    public static List<FileInfo> EnumerateNonHiddenFilesFiltered(this DirectoryInfo topDir, Func<FileInfo, bool>? filter)
    {
        var ret = new List<FileInfo>();
        foreach (var info in topDir.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly))
            EnumerateNonHiddenFilesRecurse(ret, info, filter);
        return ret;
    }

    /// <summary> Return whether a file or directory is hidden. </summary>
    public static bool IsHidden(this FileSystemInfo file)
        => file.Attributes.HasFlag(FileAttributes.Hidden);

    extension(Path)
    {
        /// <summary> Combine a base directory and a relative path in a way that does not allow the resulting path to lie outside the base directory. </summary>
        /// <param name="directory"> The base directory. </param>
        /// <param name="relativePath"> The relative path. </param>
        /// <param name="comparison"> The comparison method to use to check whether the resulting path lies in the base directory. </param>
        /// <returns> The normalized full path. </returns>
        /// <exception cref="UnauthorizedAccessException"> If the resulting full path is outside the base directory. </exception>
        /// <remarks> Does not prevent junctions or links. </remarks>
        public static string CombineSafely(string directory, string relativePath,
            StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            var normalizedDirectory = Path.GetFullPath(directory);
            if (!normalizedDirectory.EndsWith(Path.DirectorySeparatorChar))
                normalizedDirectory += Path.DirectorySeparatorChar;
            var normalizedPath = Path.GetFullPath(relativePath, normalizedDirectory);
            if (!normalizedPath.StartsWith(normalizedDirectory, comparison))
                throw new UnauthorizedAccessException($"Path '{relativePath}' escapes base directory '{directory}'.");

            return normalizedPath;
        }
    }

    private static void EnumerateNonHiddenFilesRecurse(List<FileInfo> files, FileSystemInfo info, Func<FileInfo, bool>? filter)
    {
        if (info.IsHidden())
            return;

        switch (info)
        {
            case FileInfo file when filter?.Invoke(file) ?? true:
                files.Add(file);
                return;
            case DirectoryInfo dir:
            {
                foreach (var info2 in dir.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly))
                    EnumerateNonHiddenFilesRecurse(files, info2, filter);
                return;
            }
        }
    }
}
