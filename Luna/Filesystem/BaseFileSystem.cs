namespace Luna;

/// <summary> The base file system class. </summary>
/// <param name="comparer"> The comparer to use to compare names with. </param>
public class BaseFileSystem(string name, Logger log, IComparer<ReadOnlySpan<char>>? comparer = null)
{
    /// <summary> The event invoked when anything in the file system changes. </summary>
    public readonly FileSystemChanged Changed = new($"{name}Changed", log);

    /// <summary> The event invoked when a data node updates its full path with a new value. </summary>
    public readonly DataNodePathChange DataNodeChanged = new($"{name}DataNodeChanged", log);

    /// <inheritdoc cref="Root"/>
    private readonly FileSystemFolder _root = FileSystemNode.CreateRoot();

    /// <summary> The continuous id counter that is incremented and applied whenever a new node is created. </summary>
    private FileSystemIdentifier _idCounter = FileSystemIdentifier.Zero;

    /// <summary> The name comparer that compares two nodes by their names only. </summary>
    private readonly NameComparer _nameComparer = new(comparer ?? new OrdinalSpanComparer());

    /// <summary> Check whether two strings are equal according to this file system's comparer. </summary>
    public bool Equal(ReadOnlySpan<char> lhs, ReadOnlySpan<char> rhs)
        => _nameComparer.BaseComparer.Compare(lhs, rhs) is 0;

    /// <summary> The root folder in which this file system is contained. </summary>
    public IFileSystemFolder Root
        => _root;

    /// <summary> Clear all nodes from the file system and reset the identifier counter. </summary>
    public void Clear()
    {
        _root.Children.Clear();
        _root.TotalDataNodes   = 0;
        _root.TotalDescendants = 0;
        _idCounter             = FileSystemIdentifier.Zero;
    }

    /// <summary> Change the lock state of an item and invoke a change for it if it actually changes. </summary>
    /// <returns> True on change, false if nothing changed. </returns>
    public bool ChangeLockState(IFileSystemNode node, bool value)
    {
        if (node.Locked == value)
            return false;

        ((FileSystemNode)node).SetLocked(value);
        Changed.Invoke(new FileSystemChanged.Arguments(FileSystemChangeType.LockedChange, node, null, null));
        return true;
    }

    /// <summary> Change the lock state of an item and invoke a change for it if it actually changes. </summary>
    /// <returns> True on change, false if nothing changed. </returns>
    public bool ChangeExpandedState(IFileSystemFolder folder, bool value)
    {
        if (folder.Expanded == value)
            return false;

        ((FileSystemFolder)folder).SetExpanded(value);
        Changed.Invoke(new FileSystemChanged.Arguments(FileSystemChangeType.ExpandedChange, folder, null, null));
        return true;
    }

    /// <summary> Find a specific child by its path from Root. </summary>
    /// <param name="fullPath"> The full path from root. </param>
    /// <param name="child"> The furthest existing folder in the path. </param>
    /// <returns> True if the folder was found, and false if not. </returns>
    public bool Find(ReadOnlySpan<char> fullPath, out IFileSystemNode child)
    {
        var folder = _root;
        child = _root;
        do
        {
            fullPath = fullPath.SplitDirectory(out var part);
            var idx = Search(folder, part);
            if (idx < 0)
            {
                child = folder;
                return false;
            }

            child = folder.Children[idx];
            if (child is not FileSystemFolder f)
                return fullPath.IsEmpty;

            folder = f;
        } while (!fullPath.IsEmpty);

        return true;
    }

    /// <summary> Create a new data node. </summary>
    /// <typeparam name="T"> The type of data for the node. </typeparam>
    /// <param name="parent"> The parent folder to create the data node in. </param>
    /// <param name="name"> The name to assign the data node. </param>
    /// <param name="data"> The data object associated with the node. </param>
    /// <returns> The newly created data node and its index in <paramref name="parent"/>. </returns>
    /// <exception cref="Exception"> Throws if a child of that name already exists in parent. </exception>
    public (IFileSystemData, int) CreateDataNode<T>(IFileSystemFolder parent, ReadOnlySpan<char> name, T data)
        where T : class, IFileSystemValue<T>
    {
        var node = new FileSystemData<T>(_idCounter + 1u, data)
        {
            Parent = (FileSystemFolder)parent,
        };
        UpdateFullPath(node, name, true);
        if (SetChild(node.Parent, node, out var idx) is Result.ItemExists)
            throw new Exception($"Could not add data node {node.Name} to {parent.FullPath}: Child of that name already exists.");


        ++_idCounter;
        data.Node = node;
        Changed.Invoke(new FileSystemChanged.Arguments(FileSystemChangeType.DataAdded, node, null, parent));
        return (node, idx);
    }

    /// <summary> Create a new data node with a uniquely deduplicated name. </summary>
    /// <typeparam name="T"> The type of data for the node. </typeparam>
    /// <param name="parent"> The parent folder to create the data node in. </param>
    /// <param name="name"> The base name to assign the data node. </param>
    /// <param name="data"> The data object associated with the node. </param>
    /// <returns> The newly created data node and its index in <paramref name="parent"/>. </returns>
    public (IFileSystemData, int) CreateDuplicateDataNode<T>(IFileSystemFolder parent, ReadOnlySpan<char> name, T data)
        where T : class, IFileSystemValue<T>
    {
        name = name.FixName();
        while (Search((FileSystemFolder)parent, name) >= 0)
            name = name.IncrementDuplicate();
        return CreateDataNode(parent, name, data);
    }

    /// <summary> Create a new folder. </summary>
    /// <param name="parent"> The parent to create the folder in. </param>
    /// <param name="name"> The name of the new folder, which will be stripped of trailing and leading whitespace, and all '/' will be replaced by '\'. </param>
    /// <returns> The created folder and its index in its parent. </returns>
    /// <exception cref="Exception"> Throws if a child of that name already exists in <paramref name="parent"/>, even if it is a folder. </exception>
    public (IFileSystemFolder, int) CreateFolder(IFileSystemFolder parent, ReadOnlySpan<char> name)
    {
        var folder = new FileSystemFolder(_idCounter + 1u)
        {
            Parent = (FileSystemFolder)parent,
        };
        UpdateFullPath(folder, name, true);
        if (SetChild(folder.Parent, folder, out var idx) is Result.ItemExists)
            throw new Exception($"Could not add folder {folder.Name} to {parent.FullPath}: Child of that name already exists.");

        ++_idCounter;
        Changed.Invoke(new FileSystemChanged.Arguments(FileSystemChangeType.FolderAdded, folder, null, parent));
        return (folder, idx);
    }

    /// <summary> Finds or create a folder within a given parent. </summary>
    /// <param name="parent"> The parent to search for the existing folder or to create it in. </param>
    /// <param name="name"> The name of the new or existing folder, which will be stripped of trailing and leading whitespace, and all '/' will be replaced by '\'. </param>
    /// <returns> The existing or created folder and its index in the parent. </returns>
    /// <exception cref="Exception"> Throws if a child of that name already exists in <paramref name="parent"/> but is not a folder. </exception>
    public (IFileSystemFolder, int) FindOrCreateFolder(IFileSystemFolder parent, ReadOnlySpan<char> name)
    {
        var folder = new FileSystemFolder(_idCounter + 1u)
        {
            Parent = (FileSystemFolder)parent,
        };
        UpdateFullPath(folder, name, true);
        if (SetChild((FileSystemFolder)parent, folder, out var idx) is Result.ItemExists)
        {
            if (parent.Children[idx] is FileSystemFolder f)
                return (f, idx);

            throw new Exception(
                $"Could not add folder {folder.Name} to {parent.FullPath}: Child of that name already exists, but is not a folder.");
        }

        ++_idCounter;
        Changed.Invoke(new FileSystemChanged.Arguments(FileSystemChangeType.FolderAdded, folder, null, parent));
        return (folder, idx);
    }

    /// <summary> Split a path into successive subfolders of root and find or create the topmost folder. </summary>
    /// <param name="path"> The path to ensure the existence of all folders for. </param>
    /// <returns> The topmost folder. </returns>
    /// <exception cref="Exception"> Throws if a folder can not be found or created due to a non-folder child of that name already existing. </exception>
    public IFileSystemFolder FindOrCreateAllFolders(ReadOnlySpan<char> path)
    {
        var (res, folder) = CreateAllFolders(path);
        switch (res)
        {
            case Result.Success:
                Changed.Invoke(new FileSystemChanged.Arguments(FileSystemChangeType.FolderAdded, folder, null, folder.Parent));
                break;
            case Result.ItemExists:
                throw new Exception(
                    $"Could not create new folder for {path}: {folder.FullPath} already contains an object with a required name.");
            case Result.PartialSuccess:
                Changed.Invoke(new FileSystemChanged.Arguments(FileSystemChangeType.FolderAdded, folder, null, folder.Parent));
                throw new Exception(
                    $"Could not create all new folders for {path}: {folder.FullPath} already contains an object with a required name.");
        }

        return folder;
    }

    /// <summary> Move and rename a node to a new path. </summary>
    /// <param name="node"> The node to move. </param>
    /// <param name="newPath"> The new path to move the node to. </param>
    /// <exception cref="Exception"> Throws if the new path is empty, not all folders in the path could be found/created or the node could not be named. </exception>
    public void RenameAndMove(IFileSystemNode node, string newPath)
    {
        if (newPath.Length is 0)
            throw new Exception($"Could not change path of {node.FullPath} to an empty path.");

        var oldPath = node.FullPath;
        if (newPath == oldPath)
            return;

        var (res, folder) = CreateAllFoldersAndFile(newPath, out var fileName);
        var oldParent = node.Parent;
        switch (res)
        {
            case Result.Success:
                MoveChild((FileSystemNode)node, folder, out _, out _, fileName); // Can not fail since the parent folder is new.
                Changed.Invoke(new FileSystemChanged.Arguments(FileSystemChangeType.ObjectMoved, node, oldParent, folder));
                break;
            case Result.SuccessNothingDone:
                res = MoveChild((FileSystemNode)node, folder, out _, out _, fileName);
                if (res is Result.ItemExists)
                    throw new Exception($"Could not move {oldPath} to {newPath}: An object of name {fileName} already exists.");

                Changed.Invoke(new FileSystemChanged.Arguments(FileSystemChangeType.ObjectMoved, node, oldParent, folder));
                return;
            case Result.ItemExists:
                throw new Exception(
                    $"Could not create {newPath} for {oldPath}: A pre-existing folder contained an object of the same name as a required folder.");
        }
    }

    /// <summary> Move and rename a node to a new path, appending duplicate numbering to the new name if necessary. </summary>
    /// <param name="node"> The node to move. </param>
    /// <param name="newPath"> The new path to move the node to. </param>
    /// <exception cref="Exception"> Throws if the new path is empty, not all folders in the path could be found/created or the node could not be named. </exception>
    public void RenameAndMoveWithDuplicates(IFileSystemNode node, string newPath)
    {
        if (newPath.Length is 0)
            throw new Exception($"Could not change path of {node.FullPath} to an empty path.");

        var oldPath = node.FullPath;
        if (newPath == oldPath)
            return;

        var (res, folder) = CreateAllFoldersAndFile(newPath, out var fileName);
        var oldParent = node.Parent;
        switch (res)
        {
            case Result.Success:
                MoveChild((FileSystemNode)node, folder, out _, out _, fileName); // Can not fail since the parent folder is new.
                Changed.Invoke(new FileSystemChanged.Arguments(FileSystemChangeType.ObjectMoved, node, oldParent, folder));
                break;
            case Result.SuccessNothingDone:
                while (true)
                {
                    res = MoveChild((FileSystemNode)node, folder, out _, out _, fileName);
                    // The other failure results can not happen in this case.
                    if (res is Result.ItemExists)
                    {
                        fileName = fileName.IncrementDuplicate();
                        continue;
                    }

                    Changed.Invoke(new FileSystemChanged.Arguments(FileSystemChangeType.ObjectMoved, node, oldParent, folder));
                    return;
                }
            case Result.ItemExists:
                throw new Exception(
                    $"Could not create {newPath} for {oldPath}: A pre-existing folder contained an object of the same name as a required folder.");
        }
    }

    /// <summary> Rename a node. </summary>
    /// <param name="node"> The node to rename. </param>
    /// <param name="newName"> The new name for the node. </param>
    /// <exception cref="Exception"> Throws if <paramref name="node"/> is Root or an item of that name already exists in its parent. </exception>
    public void Rename(IFileSystemNode node, ReadOnlySpan<char> newName)
    {
        switch (RenameChild((FileSystemNode)node, newName))
        {
            case Result.InvalidOperation: throw new Exception("Can not rename root directory.");
            case Result.ItemExists:
                throw new Exception(
                    $"Could not rename {node.Name} to {newName}: Child of that name already exists in {node.Parent!.FullPath}.");
            case Result.Success:
                Changed.Invoke(new FileSystemChanged.Arguments(FileSystemChangeType.ObjectRenamed, node, node.Parent, node.Parent));
                return;
        }
    }

    /// <summary> Rename a node to a new name or an appended duplicate version of it. </summary>
    /// <param name="node"> The node to rename. </param>
    /// <param name="newName"> The new name for the node. </param>
    /// <exception cref="Exception"> Throws if <paramref name="node"/> is Root.</exception>
    public void RenameWithDuplicates(IFileSystemNode node, ReadOnlySpan<char> newName)
    {
        while (true)
        {
            switch (RenameChild((FileSystemNode)node, newName))
            {
                case Result.InvalidOperation: throw new Exception("Can not rename root directory.");
                case Result.ItemExists:
                    newName = newName.IncrementDuplicate();
                    continue;
                case Result.Success:
                case Result.SuccessNothingDone:
                    Changed.Invoke(new FileSystemChanged.Arguments(FileSystemChangeType.ObjectRenamed, node, node.Parent, node.Parent));
                    return;
            }
        }
    }

    /// <summary> Delete a node from the file system. </summary>
    /// <param name="node"> The node to delete. </param>
    /// <exception cref="Exception"> Throws if <paramref name="node"/>> is Root. </exception>
    public void Delete(IFileSystemNode node)
    {
        switch (RemoveChild((FileSystemNode)node))
        {
            case Result.InvalidOperation: throw new Exception("Can not delete root directory.");
            case Result.Success:
                if (node is IFileSystemData l)
                    l.Value.Node = null;
                Changed.Invoke(new FileSystemChanged.Arguments(FileSystemChangeType.ObjectRemoved, node, node.Parent, null));
                return;
        }
    }

    /// <summary> Move a node to a new parent. </summary>
    /// <param name="node"> The node to move. </param>
    /// <param name="newParent"> The new parent to move the node to. </param>
    /// <exception cref="Exception"> Throws if <paramref name="node"/>> is Root, <paramref name="newParent"/> is a descendant of <paramref name="node"/> or a child with the same name already exists in <paramref name="newParent"/> and is not a folder. </exception>
    /// <remarks> If a child of child's name already exists in newParent and is a folder, it will try to merge child into this folder instead. </remarks>
    public void Move(IFileSystemNode node, IFileSystemFolder newParent)
    {
        switch (MoveChild((FileSystemNode)node, (FileSystemFolder)newParent, out var oldParent, out var newIdx))
        {
            case Result.Success:
                Changed.Invoke(new FileSystemChanged.Arguments(FileSystemChangeType.ObjectMoved, node, oldParent, newParent));
                break;
            case Result.SuccessNothingDone: return;
            case Result.InvalidOperation:   throw new Exception("Can not move root directory.");
            case Result.CircularReference:
                throw new Exception($"Can not move {node.FullPath} into {newParent.FullPath} since folders can not contain themselves.");
            case Result.ItemExists:
                if (node is not FileSystemFolder childFolder || newParent.Children[newIdx] is not FileSystemFolder preFolder)
                    throw new Exception(
                        $"Can not move {node.Name} into {newParent.FullPath} because {newParent.Children[newIdx].FullPath} already exists.");

                Merge(childFolder, preFolder);
                return;
        }
    }

    /// <summary> Merge all children from one folder into the other. </summary>
    /// <param name="from"> The folder to move children from. </param>
    /// <param name="to"> The folder to move nodes to. </param>
    /// <exception cref="Exception"> Throws if <paramref name="from"/> is Root, or if no children could be moved at all. </exception>
    /// <remarks>
    ///   If all children can be moved, <paramref name="from"/> is deleted. <br/>
    ///   If some children can not be moved, <paramref name="from"/> and the unmoved children are kept where they are.
    /// </remarks>
    public void Merge(IFileSystemFolder from, IFileSystemFolder to)
    {
        switch (MergeFolders((FileSystemFolder)from, (FileSystemFolder)to))
        {
            case Result.SuccessNothingDone: return;
            case Result.InvalidOperation:   throw new Exception($"Can not merge root directory into {to.FullPath}.");
            case Result.Success:
                Changed.Invoke(new FileSystemChanged.Arguments(FileSystemChangeType.FolderMerged, from, from, to));
                return;
            case Result.PartialSuccess:
                Changed.Invoke(new FileSystemChanged.Arguments(FileSystemChangeType.PartialMerge, from, from, to));
                return;
            case Result.NoSuccess:
                throw new Exception(
                    $"Could not merge {from.FullPath} into {to.FullPath} because all children already existed in the target.");
        }
    }

    #region internal

    private enum Result
    {
        Success,
        SuccessNothingDone,
        InvalidOperation,
        ItemExists,
        PartialSuccess,
        CircularReference,
        NoSuccess,
    }

    /// <summary> Find a child-index inside a folder using the given comparer. </summary>
    /// <returns></returns>
    private int Search(FileSystemFolder parent, ReadOnlySpan<char> name)
        => CollectionsMarshal.AsSpan(parent.Children).BinarySearch(new SearchNode(_nameComparer, name));

    /// <summary> Try to rename a node inside its parent. </summary>
    /// <param name="node"> The node to rename. </param>
    /// <param name="newName"> The intended new name for the node. </param>
    /// <returns>
    ///   <list>
    ///     <item>
    ///       <term> <see cref="Result.InvalidOperation"/> </term>
    ///       <description> <paramref name="node"/> is Root. </description>
    ///     </item>
    ///     <item>
    ///       <term> <see cref="Result.SuccessNothingDone"/> </term>
    ///       <description> the fixed <paramref name="newName"/>is identical to <paramref name="node"/>'s old name. </description>
    ///     </item>
    ///     <item>
    ///       <term> <see cref="Result.ItemExists"/> </term>
    ///       <description> an item of the fixed, intended name already exists in <paramref name="node"/>'s parent. </description>
    ///     </item>
    ///     <item>
    ///       <term> <see cref="Result.Success"/> </term>
    ///       <description> <paramref name="node"/> was successfully renamed. </description>
    ///     </item>
    ///   </list>
    /// </returns>
    /// <remarks> Will fix the name. </remarks>
    private Result RenameChild(FileSystemNode node, ReadOnlySpan<char> newName)
    {
        if (node.Parent is null)
            return Result.InvalidOperation;

        newName = newName.FixName();
        if (_nameComparer.BaseComparer.Compare(newName, node.Name) is 0)
            return Result.SuccessNothingDone;

        var newIdx = Search(node.Parent, newName);
        if (newIdx >= 0)
        {
            if (newIdx != node.IndexInParent)
                return Result.ItemExists;

            UpdateFullPath(node, newName, false);
            return Result.Success;
        }

        newIdx = ~newIdx;
        if (newIdx > node.IndexInParent)
        {
            for (var i = node.IndexInParent + 1; i < newIdx; ++i)
                node.Parent.Children[i].UpdateIndex(i - 1);
            --newIdx;
        }
        else
        {
            for (var i = newIdx; i < node.IndexInParent; ++i)
                node.Parent.Children[i].UpdateIndex(i + 1);
        }

        node.Parent.Children.Move(node.IndexInParent, newIdx);
        node.UpdateIndex(newIdx);
        UpdateFullPath(node, newName, false);
        return Result.Success;
    }


    /// <summary> Try to move a node to a new parent, while optionally renaming it. </summary>
    /// <param name="node"> The node to move and rename. </param>
    /// <param name="newParent"> The target folder to move the node to. </param>
    /// <param name="oldParent"> The parent of the moved node before moving. </param>
    /// <param name="newIdx"> The index of the node inside its new parent after moving on success. </param>
    /// <param name="newName"> If this is non-empty, a new name to rename the node to. </param>
    /// <returns>
    ///   <list>
    ///     <item>
    ///       <term> <see cref="Result.InvalidOperation"/> </term>
    ///       <description> <paramref name="node"/> is Root. </description>
    ///     </item>
    ///     <item>
    ///       <term> <see cref="Result.SuccessNothingDone"/> </term>
    ///       <description> <paramref name="newParent"/> is the same as <paramref name="node"/>'s current parent and <paramref name="newName"/> is empty or the fixed <paramref name="newName"/>is identical to <paramref name="node"/>'s old name. </description>
    ///     </item>
    ///     <item>
    ///       <term> <see cref="Result.ItemExists"/> </term>
    ///       <description> an item of the fixed, intended name already exists in <paramref name="newParent"/>'. </description>
    ///     </item>
    ///     <item>
    ///       <term> <see cref="Result.CircularReference"/> </term>
    ///       <description> <paramref name="newParent"/> is a descendant of <paramref name="node"/>. </description>
    ///     </item>
    ///     <item>
    ///       <term> <see cref="Result.Success"/> </term>
    ///       <description> <paramref name="node"/> was successfully moved and renamed. </description>
    ///     </item>
    ///   </list>
    /// </returns>
    /// <remarks> Will fix the name if it is set. </remarks>
    private Result MoveChild(FileSystemNode node, FileSystemFolder newParent, out FileSystemFolder oldParent, out int newIdx,
        ReadOnlySpan<char> newName = default)
    {
        newIdx = 0;
        if (node.Name.Length is 0)
        {
            oldParent = _root;
            return Result.InvalidOperation;
        }

        oldParent = node.Parent!;
        if (newParent == oldParent || newParent == node)
            return newName.IsEmpty ? Result.SuccessNothingDone : RenameChild(node, newName);

        if (!CheckHeritage(newParent, node))
            return Result.CircularReference;

        var actualNewName = newName.IsEmpty ? node.Name : newName.FixName();
        newIdx = Search(newParent, actualNewName);
        if (newIdx >= 0)
            return Result.ItemExists;

        RemoveChild(oldParent, node, Search(oldParent, node.Name));
        newIdx = ~newIdx;
        UpdateFullPath(node, actualNewName, false);
        SetChild(newParent, node, newIdx);
        return Result.Success;
    }

    /// <summary> Try to create all folders in the given path as successive subfolders beginning from Root. </summary>
    /// <param name="fullPath"> The full path of folders to create. The last item is also treated as a folder. </param>
    /// <returns>
    ///   The topmost available folder and
    ///   <list>
    ///     <item>
    ///       <term> <see cref="Result.ItemExists"/> </term>
    ///       <description> the first part of the path already exists as a child of Root but is not a folder. </description>
    ///     </item>
    ///     <item>
    ///       <term> <see cref="Result.SuccessNothingDone"/> </term>
    ///       <description> the full path already exists as folders.  </description>
    ///     </item>
    ///     <item>
    ///       <term> <see cref="Result.Success"/> </term>
    ///       <description> all folders exist and at least one was newly created. </description>
    ///     </item>
    ///   </list>
    /// </returns>
    private (Result, FileSystemFolder) CreateAllFolders(ReadOnlySpan<char> fullPath)
    {
        var last   = _root;
        var result = Result.SuccessNothingDone;
        do
        {
            fullPath = fullPath.SplitDirectory(out var name);
            var folder = new FileSystemFolder(_idCounter + 1u) { Parent = last };
            UpdateFullPath(folder, name, true);
            var midResult = SetChild(last, folder, out var idx);
            if (midResult is Result.ItemExists)
            {
                if (last.Children[idx] is not FileSystemFolder f)
                    return (Result.ItemExists, last);

                last = f;
            }
            else
            {
                ++_idCounter;
                result = Result.Success;
                last   = folder;
            }
        } while (!fullPath.IsEmpty);

        return (result, last);
    }

    /// <summary> Split off a final file name from the full path before creating all folders in it like in <see cref="CreateAllFolders"/>. </summary>
    /// <param name="fullPath"> The full path of folders to create. The last part is skipped. </param>
    /// <param name="fileName"> The last part of the path as a file name. </param>
    /// <returns>
    ///   The topmost available folder and
    ///   <list>
    ///     <item>
    ///       <term> <see cref="Result.ItemExists"/> </term>
    ///       <description> any part of the path already exists in its parent but was not a folder. Does not check for existence of the final file. </description>
    ///     </item>
    ///     <item>
    ///       <term> <see cref="Result.SuccessNothingDone"/> </term>
    ///       <description> the full path already exists as folders or did not contain any '/'.  </description>
    ///     </item>
    ///     <item>
    ///       <term> <see cref="Result.Success"/> </term>
    ///       <description> all folders exist and at least one was newly created. </description>
    ///     </item>
    ///   </list>
    /// </returns>
    private (Result, FileSystemFolder) CreateAllFoldersAndFile(ReadOnlySpan<char> fullPath, out ReadOnlySpan<char> fileName)
    {
        if (fullPath.Length == 0)
        {
            fileName = string.Empty;
            return (Result.SuccessNothingDone, _root);
        }

        var slash = fullPath.IndexOf('/');
        if (slash < 0)
        {
            fileName = fullPath;
            return (Result.SuccessNothingDone, _root);
        }

        fileName = fullPath[(slash + 1)..];
        fullPath = fullPath[..slash];
        return CreateAllFolders(fullPath);
    }


    private static void ApplyDescendantChanges(FileSystemFolder parent, FileSystemNode child, int idx, bool removed)
    {
        var (descendants, leaves) = (child, removed) switch
        {
            (FileSystemFolder f, false) => (f.TotalDescendants + 1, f.TotalDataNodes),
            (FileSystemFolder f, true)  => (-f.TotalDescendants - 1, -f.TotalDataNodes),
            (_, true)                   => (-1, -1),
            _                           => (1, 1),
        };

        for (var i = idx; i < parent.Children.Count; i++)
            parent.Children[i].UpdateIndex(i);

        while (true)
        {
            parent.TotalDescendants += descendants;
            parent.TotalDataNodes   += leaves;
            if (parent.IsRoot)
                break;

            parent = parent.Parent!;
        }
    }

    /// <summary> Remove a child at position idx from its parent. Does not change child.Parent. </summary>
    private static void RemoveChild(FileSystemFolder parent, FileSystemNode child, int idx)
    {
        parent.Children.RemoveAt(idx);
        ApplyDescendantChanges(parent, child, idx, true);
    }

    /// <summary> Add a child to its new parent at position idx. </summary>
    private static void SetChild(FileSystemFolder parent, FileSystemNode child, int idx)
    {
        parent.Children.Insert(idx, child);
        child.Parent = parent;
        child.UpdateDepth();
        ApplyDescendantChanges(parent, child, idx, false);
    }

    /// <summary> Add a child to its new parent and return its new idx. </summary>
    /// <returns> ItemExists if a child of that name already exists in parent or Success otherwise. </returns>
    private Result SetChild(FileSystemFolder parent, FileSystemNode child, out int idx)
    {
        idx = Search(parent, child.Name);
        if (idx >= 0)
            return Result.ItemExists;

        idx = ~idx;
        SetChild(parent, child, idx);
        return Result.Success;
    }

    /// <summary> Remove a child from its parent. </summary>
    /// <param name="child"> The child to remove. </param>
    /// <returns>
    ///   <list>
    ///     <item>
    ///       <term> <see cref="Result.InvalidOperation"/> </term>
    ///       <description> <paramref name="child"/> is Root. </description>
    ///     </item>
    ///     <item>
    ///       <term> <see cref="Result.SuccessNothingDone"/> </term>
    ///       <description> <paramref name="child"/> is not set as a child of its parent. Should never happen. </description>
    ///     </item>
    ///     <item>
    ///       <term> <see cref="Result.Success"/> </term>
    ///       <description> <paramref name="child"/> was successfully removed from its parent. Does not change child.Parent. </description>
    ///     </item>
    ///   </list>
    /// </returns>
    private Result RemoveChild(FileSystemNode child)
    {
        if (child.IsRoot)
            return Result.InvalidOperation;

        var idx = Search(child.Parent!, child.Name);
        if (idx < 0)
            return Result.SuccessNothingDone;

        RemoveChild(child.Parent!, child, idx);
        return Result.Success;
    }

    /// <summary> Try to merge all children <paramref name="from"/> into <paramref name="to"/> and remove <paramref name="from"/> if it is empty at the end. </summary>
    /// <param name="from"> The folder to move children away from. </param>
    /// <param name="to"> The folder to move children into. </param>
    /// <returns>
    ///   <list>
    ///     <item>
    ///       <term> <see cref="Result.InvalidOperation"/> </term>
    ///       <description> <paramref name="from"/> is Root. </description>
    ///     </item>
    ///     <item>
    ///       <term> <see cref="Result.SuccessNothingDone"/> </term>
    ///       <description> <paramref name="from"/> is the same as <paramref name="to"/>. </description>
    ///     </item>
    ///     <item>
    ///       <term> <see cref="Result.CircularReference"/> </term>
    ///       <description> <paramref name="to"/> is a descendant of <paramref name="from"/>. </description>
    ///     </item>
    ///     <item>
    ///       <term> <see cref="Result.PartialSuccess"/> </term>
    ///       <description> some, but not all, children were successfully moved and <paramref name="from"/> was not deleted. </description>
    ///     </item>
    ///     <item>
    ///       <term> <see cref="Result.NoSuccess"/> </term>
    ///       <description> no items could be moved because children of their names already existed in <paramref name="to"/>. </description>
    ///     </item>
    ///     <item>
    ///       <term> <see cref="Result.Success"/> </term>
    ///       <description> all children were successfully moved and <paramref name="from"/> was deleted. </description>
    ///     </item>
    ///   </list>
    /// </returns>
    private Result MergeFolders(FileSystemFolder from, FileSystemFolder to)
    {
        if (from == to)
            return Result.SuccessNothingDone;
        if (from.IsRoot)
            return Result.InvalidOperation;
        if (!CheckHeritage(to, from))
            return Result.CircularReference;

        var result = from.Children.Count is 0 ? Result.Success : Result.NoSuccess;
        for (var i = 0; i < from.Children.Count;)
        {
            (i, result) = MoveChild(from.Children[i], to, out _, out _) == Result.Success
                ? (i, result is Result.NoSuccess ? i is 0 ? Result.Success : Result.PartialSuccess : result)
                : (i + 1, result is Result.Success ? Result.PartialSuccess : result);
        }

        return result is Result.Success ? RemoveChild(from) : result;
    }

    /// <summary> Check that child is not contained in potentialParent. </summary>
    /// <returns> True if potentialParent is not anywhere up the tree from child, false otherwise. </returns>
    private static bool CheckHeritage(FileSystemFolder potentialParent, IFileSystemNode child)
    {
        var parent = potentialParent;
        while (parent!.Name.Length > 0)
        {
            if (parent == child)
                return false;

            parent = parent.Parent;
        }

        return true;
    }

    /// <summary> Update the full path and name for the current parent. </summary>
    private void UpdateFullPath(FileSystemNode node)
    {
        if (node.IsRoot)
            return;

        var oldPath = node.FullPath;
        if (node.Parent!.IsRoot)
        {
            if (node.FullPath.Length is 0)
            {
                node.FullPath   = "<None>";
                node.NameOffset = 0;
            }
            else if (node.NameOffset is not 0)
            {
                node.FullPath   = node.Name.FixName().ToString();
                node.NameOffset = 0;
            }
        }
        else
        {
            node.FullPath   = $"{node.Parent.FullPath}/{node.Name.FixName()}";
            node.NameOffset = node.Parent.FullPath.Length + 1;
        }

        if (oldPath == node.FullPath)
            return;

        switch (node)
        {
            case IFileSystemData n:
                if (n.Value.Path.UpdateByNode(this, n))
                    DataNodeChanged.Invoke(new DataNodePathChange.Arguments(n, oldPath));
                break;
            case FileSystemFolder f:
            {
                foreach (var child in f.Children)
                    UpdateFullPath(child);
                break;
            }
        }
    }

    /// <summary> Rename this node and update the full path for the new name and the current parent. </summary>
    internal virtual void UpdateFullPath(FileSystemNode node, ReadOnlySpan<char> newName, bool fixName)
    {
        if (node.IsRoot)
            return;

        var oldPath = node.FullPath;
        if (node.Parent!.IsRoot)
        {
            node.FullPath   = fixName ? newName.FixName().ToString() : newName.ToString();
            node.NameOffset = 0;
        }
        else
        {
            node.FullPath   = $"{node.Parent.FullPath}/{(fixName ? newName.FixName() : newName)}";
            node.NameOffset = node.Parent.FullPath.Length + 1;
        }

        if (oldPath == node.FullPath)
            return;

        switch (node)
        {
            case IFileSystemData n:
                if (n.Value.Path.UpdateByNode(this, n))
                    DataNodeChanged.Invoke(new DataNodePathChange.Arguments(n, oldPath));
                break;
            case FileSystemFolder f:
            {
                foreach (var child in f.Children)
                    UpdateFullPath(child);
                break;
            }
        }
    }

    #endregion
}
