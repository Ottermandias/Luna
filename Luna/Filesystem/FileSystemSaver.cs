using System.Text.Json;
using System.Text.Json.Serialization;

namespace Luna;

/// <summary> Shared base class for file system savers. </summary>
public abstract partial class FileSystemSaver : IDisposable
{
    /// <summary> The version of the generic files saved by this service. </summary>
    public const int CurrentVersion = 1;

    /// <summary> The logger to use for logging. </summary>
    protected readonly LunaLogger Log;

    /// <summary> The parent file system to monitor. </summary>
    protected readonly BaseFileSystem FileSystem;

    /// <summary> The function to return valid sort modes for folders. </summary>
    protected abstract ISortMode? ParseSortMode(string name);

    /// <summary> The method to save local data for a data node when its containing folder or sort order name change. </summary>
    /// <param name="value"> The value with the changed path. </param>
    /// <remarks> This should handle the entire save process for the data node. </remarks>
    protected abstract void SaveDataValue(IFileSystemValue value);

    /// <summary> Create the basic file system saver. </summary>
    protected FileSystemSaver(LunaLogger log, BaseFileSystem fileSystem)
    {
        Log        = log;
        FileSystem = fileSystem;
        FileSystem.DataNodeChanged.Subscribe(OnDataNodeChanged, uint.MinValue);
    }

    /// <summary> Load a single file containing a version and a list of nodes. </summary>
    /// <param name="filePath"> The path to the file. </param>
    /// <returns> The node data on success, empty node data on failure. </returns>
    protected NodeData LoadNodeFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return new NodeData();

            var text = File.ReadAllText(filePath);
            return System.Text.Json.JsonSerializer.Deserialize(text, SourceGenerationContext.Default.NodeData) ?? new NodeData();
        }
        catch (Exception ex)
        {
            Log.Error($"Could not load {filePath}:\n{ex}");
            return new NodeData();
        }
    }

    /// <summary> Load a single file containing a version, folders, separators and their data. </summary>
    /// <param name="filePath"> The path to the file. </param>
    /// <returns> The organization data on success, empty data on failure. </returns>
    protected Organization LoadOrganizationFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return new Organization();

            var text = File.ReadAllText(filePath);
            return System.Text.Json.JsonSerializer.Deserialize(text, SourceGenerationContext.Default.Organization) ?? new Organization();
        }
        catch (Exception ex)
        {
            Log.Error($"Could not load {filePath}:\n{ex}");
            return new Organization();
        }
    }

    public virtual void Dispose()
        => FileSystem.DataNodeChanged.Unsubscribe(OnDataNodeChanged);

    private void OnDataNodeChanged(in DataNodePathChange.Arguments arguments)
        => SaveDataValue(arguments.ChangedNode.Value);

    protected class MigrationData
    {
        public Dictionary<string, string> Data         = [];
        public List<string>               EmptyFolders = [];
        public List<string>               LockedPaths  = [];
    }

    protected class BaseFile
    {
        /// <summary> The version of the file. </summary>
        public int Version = CurrentVersion;
    }

    protected class NodeData : BaseFile
    {
        /// <summary> The list of nodes in the file. </summary>
        public List<string> Nodes = [];
    }

    protected class Organization : BaseFile
    {
        /// <summary> The list of folders in the file. </summary>
        public Dictionary<string, FolderData> Folders = [];

        /// <summary> The list of separators in the file. </summary>
        public Dictionary<string, SeparatorData> Separators = [];

        /// <summary> Folder data. </summary>
        public readonly record struct FolderData(uint? ExpandedColor, uint? CollapsedColor, string? SortMode)
        {
            /// <summary> Empty folder data. </summary>
            public static readonly FolderData Empty = new(null, null, null);
        }

        /// <summary> Separator data. </summary>
        public readonly record struct SeparatorData(uint? Color, bool Folder, long CreationDate);
    }


    [JsonSourceGenerationOptions(WriteIndented = true, AllowTrailingCommas = true, IncludeFields = true, NewLine = "\n",
        IndentCharacter = ' ',   IndentSize = 4)]
    [JsonSerializable(typeof(NodeData))]
    [JsonSerializable(typeof(Organization))]
    [JsonSerializable(typeof(MigrationData))]
    protected partial class SourceGenerationContext : JsonSerializerContext;
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

    /// <summary> The file path to save the currently selected nodes. </summary>
    /// <param name="provider"> The file path provider passed by the save service. </param>
    /// <returns> The full path to save the file containing all currently selected nodes in this file system. </returns>
    protected abstract string SelectionFile(TProvider provider);

    /// <summary> The file path to save the additional organization options and nodes. </summary>
    /// <param name="provider"> The file path provider passed by the save service. </param>
    /// <returns> The full path to save the file containing all additional organization options and nodes in this file system. </returns>
    protected abstract string OrganizationFile(TProvider provider);

    /// <summary> The file path for the (outdated) empty folder files data. </summary>
    /// <param name="provider"> The file path provider passed by the save service. </param>
    /// <returns> The full path to save the file containing all currently empty folders in this file system. </returns>
    protected virtual string EmptyFoldersMigrationFile(TProvider provider)
        => string.Empty;

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
    protected FileSystemSaver(LunaLogger log, BaseFileSystem fileSystem, TSaveService saveService)
        : base(log, fileSystem)
    {
        SaveService = saveService;
        FileSystem.Changed.Subscribe(OnFileSystemChange, uint.MinValue);
    }

    /// <summary> The delay between attempts to save locked nodes. </summary>
    public TimeSpan LockedDelay = TimeSpan.FromSeconds(1);

    /// <summary> The delay between attempts to save organization data. </summary>
    public TimeSpan OrganizationDelay = TimeSpan.FromSeconds(5);

    /// <summary> The delay between attempts to save selected nodes. </summary>
    public TimeSpan SelectedDelay = TimeSpan.FromSeconds(30);

    /// <summary> The delay between attempts to save expanded folders. </summary>
    public TimeSpan ExpandedDelay = TimeSpan.FromSeconds(30);

    /// <summary> Load the file system data from its files. </summary>
    public virtual void Load()
    {
        FileSystem.Changed.Unsubscribe(OnFileSystemChange);
        FileSystem.Clear();
        MigrateOldFileSystem();
        CreateDataNodes();
        HandleOrganization();
        MigrateEmptyFolders();
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
                    SaveService.DelaySave(new LockedData(this), LockedDelay);
                if (arguments.ChangedObject.Selected)
                    SaveService.DelaySave(new SelectedData(this), SelectedDelay);
                if (arguments.ChangedObject is IFileSystemFolder folder)
                {
                    SaveService.DelaySave(new OrganizationData(this), OrganizationDelay);
                    if (folder.Expanded)
                        SaveService.DelaySave(new ExpandedData(this), ExpandedDelay);
                }
                else if (arguments.ChangedObject is IFileSystemSeparator)
                {
                    SaveService.DelaySave(new OrganizationData(this), OrganizationDelay);
                }

                break;
            case FileSystemChangeType.FolderAdded:
            case FileSystemChangeType.SeparatorAdded:
            case FileSystemChangeType.SeparatorChanged:
            case FileSystemChangeType.FolderChanged:
                SaveService.DelaySave(new OrganizationData(this), OrganizationDelay);
                break;
            case FileSystemChangeType.ObjectMoved:
                if (arguments.ChangedObject.Locked)
                    SaveService.DelaySave(new LockedData(this), LockedDelay);
                if (arguments.ChangedObject.Selected)
                    SaveService.DelaySave(new SelectedData(this), SelectedDelay);
                SaveService.DelaySave(new ExpandedData(this), ExpandedDelay);
                if (arguments.ChangedObject is not IFileSystemData)
                    SaveService.DelaySave(new OrganizationData(this), OrganizationDelay);
                break;
            case FileSystemChangeType.FolderMerged:
            case FileSystemChangeType.PartialMerge:
            case FileSystemChangeType.Reload:
                SaveService.DelaySave(new SelectedData(this),     SelectedDelay);
                SaveService.DelaySave(new LockedData(this),       LockedDelay);
                SaveService.DelaySave(new OrganizationData(this), OrganizationDelay);
                SaveService.DelaySave(new ExpandedData(this),     ExpandedDelay);
                break;
            case FileSystemChangeType.LockedChange:   SaveService.DelaySave(new LockedData(this),   LockedDelay); break;
            case FileSystemChangeType.ExpandedChange: SaveService.DelaySave(new ExpandedData(this), ExpandedDelay); break;
            case FileSystemChangeType.SelectedChange: SaveService.DelaySave(new SelectedData(this), SelectedDelay); break;
        }
    }

    /// <summary> Apply a folder saved in the file system by its path. </summary>
    /// <param name="path"> The path of the empty folder. </param>
    /// <param name="folderData"> Additional data for the folder. </param>
    /// <returns> True when the folder could be applied, false if it could not be created. </returns>
    /// <remarks> Logs failures. </remarks>
    protected bool ApplyFolder(string path, in Organization.FolderData folderData)
    {
        try
        {
            var folder = (FileSystemFolder)FileSystem.FindOrCreateAllFolders(path);
            folder.ExpandedColor  = folderData.ExpandedColor.HasValue ? new Rgba32(folderData.ExpandedColor.Value) : ColorParameter.Default;
            folder.CollapsedColor = folderData.CollapsedColor.HasValue ? new Rgba32(folderData.CollapsedColor.Value) : ColorParameter.Default;
            if (folderData.SortMode is not null)
            {
                if (ParseSortMode(folderData.SortMode) is { } sortMode)
                    folder.SortMode = sortMode;
                else
                    Log.Debug($"Could not apply unknown sort mode {folderData.SortMode} to folder {path}.");
            }
        }
        catch (Exception ex)
        {
            Log.Debug($"Could not create folder {path}:\n{ex}");
            return false;
        }

        return true;
    }

    /// <summary> Apply a separator saved in the file system. </summary>
    /// <param name="path"> The sort order path for the separator. </param>
    /// <param name="color"> The color for the separator line. </param>
    /// <param name="timestamp"> The timestamp associated with the separator. </param>
    /// <param name="isFolder"> Whether the separator behaves like a folder or like an item. </param>
    /// <returns> True when the separator could be applied. </returns>
    protected bool ApplySeparator(string path, uint? color, long timestamp, bool isFolder)
    {
        var name       = path.AsSpan();
        var folderPath = ReadOnlySpan<char>.Empty;
        var index      = path.LastIndexOf('/');
        if (index >= 0)
        {
            name       = index == path.Length - 1 ? string.Empty : path.AsSpan(index + 1);
            folderPath = path.AsSpan(0, index);
        }

        try
        {
            var folder = folderPath.Length is 0 ? FileSystem.Root : FileSystem.FindOrCreateAllFolders(folderPath);
            FileSystem.CreateSeparator(folder, name, color ?? ColorParameter.Default, timestamp, isFolder);
        }
        catch (Exception ex)
        {
            Log.Debug($"Could not create separator {path}:\n{ex}");
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
            if (node is FileSystemFolder folder)
            {
                folder.SetExpanded(true);
                return true;
            }

            Log.Debug($"The object {path} saved as expanded folder was not a folder..");
        }
        else
        {
            Log.Debug($"Could not find folder {path} saved as expanded.");
        }

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
            var data = System.Text.Json.JsonSerializer.Deserialize(text, SourceGenerationContext.Default.MigrationData) ?? new MigrationData();
            ret = true;

            _storedLockedPaths = data.LockedPaths;
            foreach (var folder in data.EmptyFolders)
                ApplyFolder(folder, Organization.FolderData.Empty);

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

    private void HandleOrganization()
    {
        var organization = LoadOrganizationFile(OrganizationFile(SaveService.FileNames));
        if (organization.Version is not CurrentVersion)
        {
            Log.Error($"Invalid version of organization file {organization.Version}.");
        }
        else
        {
            var changes = false;
            foreach (var (folder, data) in organization.Folders)
                changes |= ApplyFolder(folder, data);

            foreach (var (separator, data) in organization.Separators)
                changes |= ApplySeparator(separator, data.Color, data.CreationDate, data.Folder);

            if (changes)
                SaveService.DelaySave(new OrganizationData(this));
        }
    }

    private void HandleLockedNodes()
    {
        var lockedNodes = LoadNodeFile(LockedFile(SaveService.FileNames));
        var changes     = false;
        if (_storedLockedPaths is not null)
        {
            changes            = _storedLockedPaths.Aggregate(changes, (current, path) => current | ApplyLockedNode(path, false));
            _storedLockedPaths = null;
        }

        if (lockedNodes.Version is not CurrentVersion)
            Log.Error($"Invalid version of locked nodes file {lockedNodes.Version}.");
        else
            changes = lockedNodes.Nodes.Aggregate(changes, (current, path) => current | !ApplyLockedNode(path, false));

        if (changes)
            SaveService.DelaySave(new LockedData(this));
    }

    private void HandleSelectedNodes()
    {
        var selectedNodes = LoadNodeFile(SelectionFile(SaveService.FileNames));
        var changes       = false;
        if (_storedSelectedPaths is not null)
        {
            changes              = _storedSelectedPaths.Aggregate(changes, (current, path) => current | ApplySelectedNode(path, false));
            _storedSelectedPaths = null;
        }

        if (selectedNodes.Version is not CurrentVersion)
            Log.Error($"Invalid version of selected nodes file {selectedNodes.Version}.");
        else
            changes = selectedNodes.Nodes.Aggregate(changes, (current, path) => current | !ApplySelectedNode(path, false));

        if (changes)
            SaveService.DelaySave(new SelectedData(this));
    }

    private void HandleExpandedFolders()
    {
        var expandedFolders = LoadNodeFile(ExpandedFile(SaveService.FileNames));
        if (expandedFolders.Version is not CurrentVersion)
        {
            Log.Error($"Invalid version of expanded folders file {expandedFolders.Version}.");
        }
        else
        {
            var changes = false;
            foreach (var path in expandedFolders.Nodes)
                changes |= !ApplyExpandedFolders(path);

            if (changes)
                SaveService.DelaySave(new ExpandedData(this));
        }
    }

    private void MigrateEmptyFolders()
    {
        var file = EmptyFoldersMigrationFile(SaveService.FileNames);
        if (!File.Exists(file))
            return;

        var emptyFolders = LoadNodeFile(file);
        if (emptyFolders.Version is not CurrentVersion)
            Log.Error($"Invalid version of empty folders file {emptyFolders.Version}.");
        else
            foreach (var path in emptyFolders.Nodes)
                ApplyFolder(path, Organization.FolderData.Empty);

        SaveService.ImmediateSaveSync(new OrganizationData(this));
        SaveService.ImmediateDeleteSync(new EmptyFoldersMigrationFiles(this));
    }

    private List<string>? _storedLockedPaths;
    private List<string>? _storedSelectedPaths;


    private readonly struct LockedData(FileSystemSaver<TSaveService, TProvider> saver) : ISavable<TProvider>
    {
        public string ToFilePath(TProvider fileNames)
            => saver.LockedFile(fileNames);

        public void Save(Stream stream)
        {
            using var j = new Utf8JsonWriter(stream, JsonFunctions.WriterOptions);
            j.WriteStartObject();
            j.WriteNumber("Version"u8, CurrentVersion);
            j.WritePropertyName("Nodes"u8);
            j.WriteStartArray();
            foreach (var node in saver.FileSystem.Root.GetDescendants().Where(n => n.Locked))
                j.WriteStringValue(node is IFileSystemData d ? $":{d.Value.Identifier}" : node.FullPath);
            j.WriteEndArray();
            j.WriteEndObject();
        }
    }

    private readonly struct SelectedData(FileSystemSaver<TSaveService, TProvider> saver) : ISavable<TProvider>
    {
        public string ToFilePath(TProvider fileNames)
            => saver.SelectionFile(fileNames);

        public void Save(Stream stream)
        {
            using var j = new Utf8JsonWriter(stream, JsonFunctions.WriterOptions);
            j.WriteStartObject();
            j.WriteNumber("Version"u8, CurrentVersion);
            j.WritePropertyName("Nodes"u8);
            j.WriteStartArray();
            foreach (var node in saver.FileSystem.Selection.OrderedNodes)
                j.WriteStringValue(node is IFileSystemData d ? $":{d.Value.Identifier}" : node.FullPath);
            j.WriteEndArray();
            j.WriteEndObject();
        }
    }

    private readonly struct ExpandedData(FileSystemSaver<TSaveService, TProvider> saver) : ISavable<TProvider>
    {
        public string ToFilePath(TProvider fileNames)
            => saver.ExpandedFile(fileNames);

        public void Save(Stream stream)
        {
            using var j = new Utf8JsonWriter(stream, JsonFunctions.WriterOptions);
            j.WriteStartObject();
            j.WriteNumber("Version"u8, CurrentVersion);
            j.WritePropertyName("Nodes"u8);
            j.WriteStartArray();
            foreach (var folder in saver.FileSystem.Root.GetDescendants().Where(n => n is IFileSystemFolder { Expanded: true }))
                j.WriteStringValue(folder.FullPath);
            j.WriteEndArray();
            j.WriteEndObject();
        }
    }

    private readonly struct OrganizationData(FileSystemSaver<TSaveService, TProvider> saver) : ISavable<TProvider>
    {
        public string ToFilePath(TProvider fileNames)
            => saver.OrganizationFile(fileNames);

        public void Save(Stream stream)
        {
            using var j = new Utf8JsonWriter(stream, JsonFunctions.WriterOptions);
            j.WriteStartObject();
            j.WriteNumber("Version"u8, CurrentVersion);
            j.WriteStartObject("Folders"u8);
            foreach (var folder in saver.FileSystem.Root.GetDescendants().OfType<FileSystemFolder>())
            {
                j.WriteStartObject(folder.FullPath);
                if (!folder.ExpandedColor.IsDefault)
                    j.WriteNumber("ExpandedColor"u8, folder.ExpandedColor.Color!.Value.Color);
                if (!folder.CollapsedColor.IsDefault)
                    j.WriteNumber("CollapsedColor"u8, folder.CollapsedColor.Color!.Value.Color);
                if (folder.SortMode is not null)
                    j.WriteString("SortMode"u8, folder.SortMode.GetType().Name);
                j.WriteEndObject();
            }

            j.WriteEndObject();

            j.WriteStartObject("Separators"u8);
            foreach (var separator in saver.FileSystem.Root.GetDescendants().OfType<FileSystemSeparator>())
            {
                j.WriteStartObject(separator.FullPath);
                if (separator.IsFolder)
                    j.WriteBoolean("Folder"u8, separator.IsFolder);
                if (!separator.Color.IsDefault)
                    j.WriteNumber("Color"u8, separator.Color.Color!.Value.Color);
                j.WriteNumber("CreationDate"u8, separator.CreationDate);
                j.WriteEndObject();
            }

            j.WriteEndObject();

            j.WriteEndObject();
        }
    }

    private readonly struct EmptyFoldersMigrationFiles(FileSystemSaver<TSaveService, TProvider> saver) : ISavable<TProvider>
    {
        public string ToFilePath(TProvider fileNames)
            => saver.EmptyFoldersMigrationFile(fileNames);

        public void Save(Stream stream)
        { }
    }
}
