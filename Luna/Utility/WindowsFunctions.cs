namespace Luna;

/// <summary> Utility functions to access or query Windows-specific state or functionality. </summary>
public static partial class WindowsFunctions
{
    /// <summary> Try to obtain the list of Quick Access folders from your windows system. </summary>
    /// <param name="folders"> A list of Quick Access folder names and their full paths. </param>
    /// <returns> True if the data could be fetched, in which case the list may still be empty, false otherwise. </returns>
    public static bool GetQuickAccessFolders(out List<(string Name, string Path)> folders)
    {
        folders = [];
        try
        {
            var shellAppType = Type.GetTypeFromProgID("Shell.Application");
            if (shellAppType == null)
                return false;

            var shell = Activator.CreateInstance(shellAppType);

            var obj = shellAppType.InvokeMember("NameSpace", BindingFlags.InvokeMethod, null, shell,
                ["shell:::{679f85cb-0220-4080-b29b-5540cc05aab6}"]);
            if (obj == null)
                return false;

            foreach (var fi in ((dynamic)obj).Items())
            {
                if (!fi.IsLink && !fi.IsFolder)
                    continue;

                folders.Add((fi.Name, fi.Path));
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary> Try to obtain the Downloads folder from your windows system. </summary>
    /// <param name="folder"> On success, the full path of the Downloads folder. </param>
    /// <returns> True on success. </returns>
    public static bool GetDownloadsFolder(out string folder)
    {
        try
        {
            var guid = new Guid("374DE290-123F-4565-9164-39C4925E467B");
            GetKnownFolderPath(guid, 0, nint.Zero, out folder);
            return folder.Length > 0;
        }
        catch
        {
            folder = string.Empty;
            return false;
        }
    }

    /// <summary> Efficiently obtain the total size, in bytes, of all files contained within the specified directory and its subdirectories. </summary>
    /// <param name="path"> The path to the directory whose total file size is to be calculated. </param>
    /// <returns> The total byte size of all files within the specified directory and its subdirectories. </returns>
    /// <remarks> This can deal with long file paths (more than 260 characters) and circular links. </remarks>
    public static unsafe long GetDirectorySize(string path)
    {
        var ret                = 0L;
        var root               = NormalizeLongPath(Path.GetFullPath(path));
        var directoriesToVisit = new Stack<string>();
        var visited            = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        directoriesToVisit.Push(root);
        visited.Add(root);

        while (directoriesToVisit.TryPop(out var folder))
        {
            fixed (char* target = folder)
            {
                Win32FindData data;
                using var handle = FindFirstFileExW(target, FindExInfoLevels.FindExInfoBasic, &data, FindExSearchOps.FindExSearchNameMatch,
                    nint.Zero,                              FindFirstExAttributes.LargeFetch);

                if (!handle)
                    continue;

                do
                {
                    var nameSpan = data.Name;
                    if (nameSpan is "" or "." or "..")
                        continue;

                    if (data.Attributes.HasFlag(FileAttributes.Directory))
                    {
                        var child = NormalizeLongPath(folder, nameSpan);
                        if (visited.Add(child))
                            directoriesToVisit.Push(child);
                    }
                    else
                    {
                        var size = ((long)data.FileSizeHigh << 32) | data.FileSizeLow;
                        ret += size;
                    }
                } while (FindNextFileW(handle, &data));
            }
        }

        return ret;
    }

    /// <summary> Normalize a given path so that it supports long paths, and ends in a wildcard search. </summary>
    private static string NormalizeLongPath(ReadOnlySpan<char> path)
    {
        const string longPath = @"\\?\";
        if (path.StartsWith(longPath))
            return string.Concat(path, @"\*");

        if (path.StartsWith(@"\\"))
            return string.Concat(@"\\?\UNC\", path[2..], @"\*");

        return string.Concat(longPath, path, @"\*");
    }

    /// <summary> Normalize a pair of folder path and folder name so that they support long paths and end in a wildcard search. </summary>
    private static string NormalizeLongPath(ReadOnlySpan<char> parent, ReadOnlySpan<char> child)
    {
        // Remove the trailing \*.
        parent = parent[..^2];
        const string longPath = @"\\?\";
        if (parent.StartsWith(longPath))
            return string.Concat(parent, @"\", child, @"\*");

        // No concat overload with 5 parameters is available, 
        // but a single component of a path can not exceed 260 length,
        // so this is fine without allocation.
        Span<char> concatDetour = stackalloc char[262];
        concatDetour[0] = '\\';
        child.CopyTo(concatDetour[1..]);
        concatDetour = concatDetour[..(child.Length + 1)];
        if (parent.StartsWith(@"\\"))
            return string.Concat(@"\\?\UNC\", parent[2..], concatDetour, @"\*");

        return string.Concat(longPath, parent, concatDetour, @"\*");
    }

    /// <summary> A disposable handle that ensures correct closure. </summary>
    private readonly ref struct SafeFindHandle(nint handle) : IDisposable
    {
        public readonly nint Handle = handle;

        public static implicit operator bool(SafeFindHandle handle)
            => handle.Handle != nint.Zero && handle.Handle != -1;

        public void Dispose()
            => FindClose(Handle);
    }


    [LibraryImport("shell32", EntryPoint = "SHGetKnownFolderPath")]
    private static partial int GetKnownFolderPath(in Guid rfid, uint dwFlags, nint hToken, [MarshalAs(UnmanagedType.LPWStr)] out string ret);

    private enum FindExInfoLevels
    {
        FindExInfoBasic = 1,
    }

    private enum FindExSearchOps
    {
        FindExSearchNameMatch = 0,
    }

    private enum FindFirstExAttributes
    {
        LargeFetch = 2,
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Win32FindData
    {
        public FileAttributes Attributes;
        public uint           CreationTimeHigh;
        public uint           CreationTimeLow;
        public uint           LastAccessTimeHigh;
        public uint           LastAccessTimeLow;
        public uint           LastWriteTimeHigh;
        public uint           LastWriteTimeLow;
        public uint           FileSizeHigh;
        public uint           FileSizeLow;
        public uint           Reserved1;
        public uint           Reserved2;
        public Data           FileName;

        public unsafe ReadOnlySpan<char> Name
            => MemoryMarshal.CreateReadOnlySpanFromNullTerminated((char*)Unsafe.AsPointer(ref FileName[0]));

        [InlineArray(274)]
        public struct Data
        {
            private char _first;
        }
    }

    [LibraryImport("kernel32.dll")]
    private static unsafe partial SafeFindHandle FindFirstFileExW(char* fileName, FindExInfoLevels infoLevelId, Win32FindData* output,
        FindExSearchOps searchOp, nint searchFilter, FindFirstExAttributes additionalFlags);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool FindNextFileW(SafeFindHandle handle, Win32FindData* output);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool FindClose(nint handle);
}
