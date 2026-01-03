namespace Luna;

/// <summary> Shared base class for file system savers. </summary>
public abstract class FileSystemSaver : IDisposable
{
    /// <summary> The version of the generic files saved by this service. </summary>
    public const int CurrentVersion = 1;

    /// <summary> The logger to use for logging. </summary>
    protected readonly Logger Log;

    /// <summary> The parent file system to monitor. </summary>
    protected readonly BaseFileSystem FileSystem;

    /// <summary> The method to save local data for a data node when its containing folder or sort order name change. </summary>
    /// <param name="value"> The value with the changed path. </param>
    /// <remarks> This should handle the entire save process for the data node. </remarks>
    protected abstract void SaveDataValue(IFileSystemValue value);

    /// <summary> Create the basic file system saver. </summary>
    protected FileSystemSaver(Logger log, BaseFileSystem fileSystem)
    {
        Log        = log;
        FileSystem = fileSystem;
        FileSystem.DataNodeChanged.Subscribe(OnDataNodeChanged, uint.MinValue);
    }

    /// <summary> Load a single file containing a version and a list of nodes. </summary>
    /// <param name="filePath"> The path to the file. </param>
    /// <returns> The node data on success, empty node data on failure. </returns>
    protected NodeData LoadFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return new NodeData();

            var text = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<NodeData>(text) ?? new NodeData();
        }
        catch (Exception ex)
        {
            Log.Error($"Could not load {filePath}:\n{ex}");
            return new NodeData();
        }
    }

    /// <summary> A versioned list of nodes. </summary>
    protected class NodeData
    {
        /// <summary> The version of the file. </summary>
        public int Version = CurrentVersion;

        /// <summary> The list of nodes in the file. </summary>
        public List<string> Nodes = [];
    }

    public virtual void Dispose()
        => FileSystem.DataNodeChanged.Unsubscribe(OnDataNodeChanged);

    private void OnDataNodeChanged(in DataNodePathChange.Arguments arguments)
        => SaveDataValue(arguments.ChangedNode.Value);
}

/// <summary> A general save service for any changes occuring in a file system. </summary>
/// <typeparam name="TSaveService"> The save service to use. </typeparam>
/// <typeparam name="TProvider"> The file path provider to use. </typeparam>
public abstract class FileSystemSaver<TSaveService, TProvider> : FileSystemSaver
    where TSaveService : BaseSaveService<TProvider>
    where TProvider : BaseFilePathProvider
{
    /// <summary> The save service to use for saving files. </summary>
    protected readonly TSaveService SaveService;

    /// <summary> The file path for the locked node files data. </summary>
    /// <param name="provider"> The file path provider passed by the save service. </param>
    /// <returns> The full path to save the file containing all locked nodes in this file system. </returns>
    protected abstract string LockedFile(TProvider provider);

    /// <summary> The file path for the expanded folder files data. </summary>
    /// <param name="provider"> The file path provider passed by the save service. </param>
    /// <returns> The full path to save the file containing all currently expanded folders in this file system. </returns>
    protected abstract string ExpandedFile(TProvider provider);

    /// <summary> The file path for the empty folder files data. </summary>
    /// <param name="provider"> The file path provider passed by the save service. </param>
    /// <returns> The full path to save the file containing all currently empty folders in this file system. </returns>
    protected abstract string EmptyFoldersFile(TProvider provider);

    /// <summary> The file path to save the currently selected nodes. </summary>
    /// <param name="provider"> The file path provider passed by the save service. </param>
    /// <returns> The full path to save the file containing all currently selected nodes in this file system. </returns>
    protected abstract string SelectionFile(TProvider provider);

    /// <summary> The file path for an old file system save file to migrate. </summary>
    /// <param name="provider"> The file path provider passed by the save service. </param>
    /// <returns> The full path to the old file system save file. If this is empty, no migration is attempted. </returns>
    protected virtual string MigrationFile(TProvider provider)
        => string.Empty;

    /// <summary> Get actual data values from the stored identifiers. </summary>
    /// <param name="identifier"> The identifier to search for. </param>
    /// <param name="value"> On success the data value. </param>
    /// <returns> True on success. </returns>
    protected abstract bool GetValueFromIdentifier(ReadOnlySpan<char> identifier, [NotNullWhen(true)] out IFileSystemValue? value);

    /// <summary> Create all data nodes in the file system. </summary>
    protected abstract void CreateDataNodes();

    /// <summary> Creates a new file system saver instance and subscribe to the necessary events. </summary>
    protected FileSystemSaver(Logger log, BaseFileSystem fileSystem, TSaveService saveService)
        : base(log, fileSystem)
    {
        SaveService = saveService;
        FileSystem.Changed.Subscribe(OnFileSystemChange, uint.MinValue);
    }

    /// <summary> The delay between attempts to save locked nodes. </summary>
    public TimeSpan LockedDelay      = TimeSpan.FromSeconds(1);

    /// <summary> The delay between attempts to save empty folders. </summary>
    public TimeSpan EmptyFolderDelay = TimeSpan.FromSeconds(5);

    /// <summary> The delay between attempts to save selected nodes. </summary>
    public TimeSpan SelectedDelay    = TimeSpan.FromSeconds(30);

    /// <summary> The delay between attempts to save expanded folders. </summary>
    public TimeSpan ExpandedDelay    = TimeSpan.FromSeconds(30);

    /// <summary> Load the file system data from its files. </summary>
    public virtual void Load()
    {
        FileSystem.Changed.Unsubscribe(OnFileSystemChange);
        FileSystem.Clear();
        MigrateOldFileSystem();
        CreateDataNodes();
        HandleEmptyFolders();
        HandleLockedNodes();
        HandleExpandedFolders();
        HandleSelectedNodes();
        FileSystem.Selection.SetData();
        FileSystem.Changed.Subscribe(OnFileSystemChange, uint.MinValue);
    }

    /// <summary> Save the corresponding files when the file system changes. </summary>
    /// <param name="arguments"> The arguments for the change. </param>
    private void OnFileSystemChange(in FileSystemChanged.Arguments arguments)
    {
        switch (arguments.Type)
        {
            case FileSystemChangeType.ObjectRenamed:
            case FileSystemChangeType.ObjectRemoved:
                if (arguments.ChangedObject.Locked)
                    SaveService.DelaySave(new LockedFiles(this), LockedDelay);
                if (arguments.ChangedObject.Selected)
                    SaveService.DelaySave(new SelectedFiles(this), SelectedDelay);
                if (arguments.ChangedObject is IFileSystemFolder folder)
                {
                    if (folder.Children.Count is 0)
                        SaveService.DelaySave(new EmptyFoldersFiles(this), EmptyFolderDelay);
                    if (folder.Expanded)
                        SaveService.DelaySave(new ExpandedFiles(this), ExpandedDelay);
                }

                break;
            case FileSystemChangeType.FolderAdded: SaveService.DelaySave(new EmptyFoldersFiles(this), EmptyFolderDelay); break;
            case FileSystemChangeType.DataAdded:
                if (arguments.ChangedObject.Parent!.Children.Count is 1)
                    SaveService.DelaySave(new EmptyFoldersFiles(this), EmptyFolderDelay);
                break;
            case FileSystemChangeType.ObjectMoved:
                if (arguments.ChangedObject.Locked)
                    SaveService.DelaySave(new LockedFiles(this), LockedDelay);
                if (arguments.ChangedObject.Selected)
                    SaveService.DelaySave(new SelectedFiles(this), SelectedDelay);
                SaveService.DelaySave(new EmptyFoldersFiles(this), EmptyFolderDelay);
                SaveService.DelaySave(new ExpandedFiles(this), ExpandedDelay);
                break;
            case FileSystemChangeType.FolderMerged:
            case FileSystemChangeType.PartialMerge:
            case FileSystemChangeType.Reload:
                SaveService.DelaySave(new SelectedFiles(this), SelectedDelay);
                SaveService.DelaySave(new LockedFiles(this), LockedDelay);
                SaveService.DelaySave(new EmptyFoldersFiles(this), EmptyFolderDelay);
                SaveService.DelaySave(new ExpandedFiles(this), ExpandedDelay);
                break;
            case FileSystemChangeType.LockedChange:   SaveService.DelaySave(new LockedFiles(this), LockedDelay); break;
            case FileSystemChangeType.ExpandedChange: SaveService.DelaySave(new ExpandedFiles(this), ExpandedDelay); break;
            case FileSystemChangeType.SelectedChange: SaveService.DelaySave(new SelectedFiles(this), SelectedDelay); break;
        }
    }

    /// <summary> Apply an empty folder saved in the file system by its path. </summary>
    /// <param name="path"> The path of the empty folder. </param>
    /// <returns> True when the empty folder could be applied, false if it is not empty or could not be created. </returns>
    /// <remarks> Logs failures. </remarks>
    protected bool ApplyEmptyFolder(string path)
    {
        try
        {
            var folder = FileSystem.FindOrCreateAllFolders(path);
            if (folder.Children.Count is not 0)
            {
                Log.Debug($"Folder {path} saved as empty is not empty anymore.");
                return false;
            }
        }
        catch (Exception ex)
        {
            Log.Debug($"Could not create empty folder {path}:\n{ex}");
            return false;
        }

        return true;
    }

    /// <summary> Apply a locked node saved in the file system by its path. </summary>
    /// <param name="path"> The path of the locked node. </param>
    /// <param name="ignoreData"> Whether only folders should be applied. </param>
    /// <returns> True when the locked node could be applied, false if the node did not exist. </returns>
    /// <remarks> Logs failures. </remarks>
    protected bool ApplyLockedNode(string path, bool ignoreData)
    {
        if (path.StartsWith(':'))
        {
            if (ignoreData)
                return false;

            var identifier = path.AsSpan(1);
            if (GetValueFromIdentifier(identifier, out var value))
                (value.Node as FileSystemNode)?.SetLocked(true);
        }
        else if (FileSystem.Find(path, out var node))
        {
            ((FileSystemNode)node).SetLocked(true);
            return true;
        }

        Log.Debug($"Could not find node {path} saved as locked.");
        return false;
    }

    /// <summary> Apply a selected node saved in the file system by its path. </summary>
    /// <param name="path"> The path of the selected node. </param>
    /// <param name="ignoreData"> Whether only folders should be applied. </param>
    /// <returns> True when the selected node could be applied, false if the node did not exist. </returns>
    /// <remarks> Logs failures. </remarks>
    protected bool ApplySelectedNode(string path, bool ignoreData)
    {
        if (path.StartsWith(':'))
        {
            if (ignoreData)
                return false;

            var identifier = path.AsSpan(1);
            if (GetValueFromIdentifier(identifier, out var value))
                (value.Node as FileSystemNode)?.SetSelected(true);
        }
        else if (FileSystem.Find(path, out var node))
        {
            ((FileSystemNode)node).SetSelected(true);
            return true;
        }

        Log.Debug($"Could not find node {path} saved as selected.");
        return false;
    }

    /// <summary> Apply an expanded folder saved in the file system by its path. </summary>
    /// <param name="path"> The path of the expanded folder. </param>
    /// <returns> True when the expanded folder could be applied, false if the folder did not exist. </returns>
    /// <remarks> Logs failures. </remarks>
    protected bool ApplyExpandedFolders(string path)
    {
        if (FileSystem.Find(path, out var node))
        {
            ((FileSystemFolder)node).SetExpanded(true);
            return true;
        }

        Log.Debug($"Could not find folder {path} saved as expanded.");
        return false;
    }

    /// <summary> Used to migrate an old file system file to the new file system structure, if <see cref="MigrationFile"/> returns a path. </summary>
    /// <returns> True if the file was found and read. </returns>
    protected bool MigrateOldFileSystem()
    {
        var oldFileSystemFile = MigrationFile(SaveService.FileNames);
        if (oldFileSystemFile.Length is 0 || !File.Exists(oldFileSystemFile))
            return false;

        Log.Information($"Migrating {oldFileSystemFile} to new file system...");
        var ret = false;
        try
        {
            var text = File.ReadAllText(oldFileSystemFile);
            var data = JsonConvert.DeserializeObject<MigrationData>(text) ?? new MigrationData();
            ret = true;

            _storedLockedPaths = data.LockedPaths;
            foreach (var folder in data.EmptyFolders)
                ApplyEmptyFolder(folder);

            foreach (var (identifier, path) in data.Data)
            {
                if (!GetValueFromIdentifier(identifier, out var value))
                {
                    Log.Warning($"Data Value {identifier} with path {path} could not be found.");
                    continue;
                }

                ApplyMigrationToData(value, path);
            }

            File.Move(oldFileSystemFile, oldFileSystemFile + ".bak", true);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to fully migrate {oldFileSystemFile} to new file system:\n{ex}");
        }

        return ret;
    }

    protected void ApplyMigrationToData(IFileSystemValue value, string path)
    {
        var sortOrderName = path.GetBaseName(value.DisplayName, out var folderName);
        var save          = false;
        if (!folderName.Equals(value.Path.Folder, StringComparison.Ordinal))
        {
            value.Path.Folder = folderName.ToString();
            save              = true;
        }

        if (sortOrderName.Length is 0)
        {
            if (value.Path.SortName is not null)
            {
                value.Path.SortName = null;
                save                = true;
            }
        }
        else if (value.Path.SortName is null || !folderName.Equals(value.Path.SortName, StringComparison.Ordinal))
        {
            value.Path.SortName = sortOrderName.ToString();
            save                = true;
        }

        if (save)
            SaveDataValue(value);
    }

    public override void Dispose()
    {
        base.Dispose();
        FileSystem.Changed.Unsubscribe(OnFileSystemChange);
    }

    private void HandleEmptyFolders()
    {
        var emptyFolders = LoadFile(EmptyFoldersFile(SaveService.FileNames));
        if (emptyFolders.Version is not CurrentVersion)
        {
            Log.Error($"Invalid version of empty folders file {emptyFolders.Version}");
        }
        else
        {
            var changes = false;
            foreach (var path in emptyFolders.Nodes)
                changes |= !ApplyEmptyFolder(path);

            if (changes)
                SaveService.DelaySave(new EmptyFoldersFiles(this));
        }
    }

    private void HandleLockedNodes()
    {
        var lockedNodes = LoadFile(LockedFile(SaveService.FileNames));
        var changes     = false;
        if (_storedLockedPaths is not null)
        {
            changes            = _storedLockedPaths.Aggregate(changes, (current, path) => current | ApplyLockedNode(path, false));
            _storedLockedPaths = null;
        }

        if (lockedNodes.Version is not CurrentVersion)
            Log.Error($"Invalid version of locked nodes file {lockedNodes.Version}");
        else
            changes = lockedNodes.Nodes.Aggregate(changes, (current, path) => current | !ApplyLockedNode(path, false));

        if (changes)
            SaveService.DelaySave(new LockedFiles(this));
    }

    private void HandleSelectedNodes()
    {
        var selectedNodes = LoadFile(SelectionFile(SaveService.FileNames));
        var changes       = false;
        if (_storedSelectedPaths is not null)
        {
            changes              = _storedSelectedPaths.Aggregate(changes, (current, path) => current | ApplySelectedNode(path, false));
            _storedSelectedPaths = null;
        }

        if (selectedNodes.Version is not CurrentVersion)
            Log.Error($"Invalid version of selected nodes file {selectedNodes.Version}");
        else
            changes = selectedNodes.Nodes.Aggregate(changes, (current, path) => current | !ApplySelectedNode(path, false));

        if (changes)
            SaveService.DelaySave(new SelectedFiles(this));
    }

    private void HandleExpandedFolders()
    {
        var expandedFolders = LoadFile(ExpandedFile(SaveService.FileNames));
        if (expandedFolders.Version is not CurrentVersion)
        {
            Log.Error($"Invalid version of expanded folders file {expandedFolders.Version}");
        }
        else
        {
            var changes = false;
            foreach (var path in expandedFolders.Nodes)
                changes |= !ApplyExpandedFolders(path);

            if (changes)
                SaveService.DelaySave(new ExpandedFiles(this));
        }
    }

    private readonly struct LockedFiles(FileSystemSaver<TSaveService, TProvider> saver) : ISavable<TProvider>
    {
        public string ToFilePath(TProvider fileNames)
            => saver.LockedFile(fileNames);

        public void Save(StreamWriter writer)
        {
            using var jWriter = new JsonTextWriter(writer);
            jWriter.Formatting = Formatting.Indented;
            jWriter.WriteStartObject();
            jWriter.WritePropertyName("Version");
            jWriter.WriteValue(CurrentVersion);
            jWriter.WritePropertyName("Nodes");
            jWriter.WriteStartArray();
            foreach (var node in saver.FileSystem.Root.GetDescendants().Where(n => n.Locked))
                jWriter.WriteValue(node is IFileSystemData d ? $":{d.Value.Identifier}" : node.FullPath);
            jWriter.WriteEndArray();
            jWriter.WriteEndObject();
        }
    }

    private readonly struct SelectedFiles(FileSystemSaver<TSaveService, TProvider> saver) : ISavable<TProvider>
    {
        public string ToFilePath(TProvider fileNames)
            => saver.SelectionFile(fileNames);

        public void Save(StreamWriter writer)
        {
            using var jWriter = new JsonTextWriter(writer);
            jWriter.Formatting = Formatting.Indented;
            jWriter.WriteStartObject();
            jWriter.WritePropertyName("Version");
            jWriter.WriteValue(CurrentVersion);
            jWriter.WritePropertyName("Nodes");
            jWriter.WriteStartArray();
            foreach (var node in saver.FileSystem.Selection.OrderedNodes)
                jWriter.WriteValue(node is IFileSystemData d ? $":{d.Value.Identifier}" : node.FullPath);
            jWriter.WriteEndArray();
            jWriter.WriteEndObject();
        }
    }

    private readonly struct ExpandedFiles(FileSystemSaver<TSaveService, TProvider> saver) : ISavable<TProvider>
    {
        public string ToFilePath(TProvider fileNames)
            => saver.ExpandedFile(fileNames);

        public void Save(StreamWriter writer)
        {
            using var jWriter = new JsonTextWriter(writer);
            jWriter.Formatting = Formatting.Indented;
            jWriter.WriteStartObject();
            jWriter.WritePropertyName("Version");
            jWriter.WriteValue(CurrentVersion);
            jWriter.WritePropertyName("Nodes");
            jWriter.WriteStartArray();
            foreach (var folder in saver.FileSystem.Root.GetDescendants().Where(n => n is IFileSystemFolder { Expanded: true }))
                jWriter.WriteValue(folder.FullPath);
            jWriter.WriteEndArray();
            jWriter.WriteEndObject();
        }
    }

    private readonly struct EmptyFoldersFiles(FileSystemSaver<TSaveService, TProvider> saver) : ISavable<TProvider>
    {
        public string ToFilePath(TProvider fileNames)
            => saver.EmptyFoldersFile(fileNames);

        public void Save(StreamWriter writer)
        {
            using var jWriter = new JsonTextWriter(writer);
            jWriter.Formatting = Formatting.Indented;
            jWriter.WriteStartObject();
            jWriter.WritePropertyName("Version");
            jWriter.WriteValue(CurrentVersion);
            jWriter.WritePropertyName("Nodes");
            jWriter.WriteStartArray();
            foreach (var folder in saver.FileSystem.Root.GetDescendants().Where(n => n is IFileSystemFolder { Children.Count: 0 }))
                jWriter.WriteValue(folder.FullPath);
            jWriter.WriteEndArray();
            jWriter.WriteEndObject();
        }
    }

    private List<string>? _storedLockedPaths;
    private List<string>? _storedSelectedPaths;

    private class MigrationData
    {
        public Dictionary<string, string> Data         = [];
        public List<string>               EmptyFolders = [];
        public List<string>               LockedPaths  = [];
    }
}
