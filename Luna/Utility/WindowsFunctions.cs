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

    [LibraryImport("shell32", EntryPoint = "SHGetKnownFolderPath")]
    private static partial int GetKnownFolderPath(in Guid rfid, uint dwFlags, nint hToken, [MarshalAs(UnmanagedType.LPWStr)] out string ret);
}
