namespace Luna;

/// <summary> Utility functions for file system handling. </summary>
public static class FileSystemUtility
{
    /// <summary> Fix a name to be usable as a folder or object name for a filesystem. </summary>
    /// <remarks>
    ///   A filesystem name may not contain forward-slashes, as they are used to split paths. <br/>
    ///   The empty string as name signifies the root, so it can also not be used.
    /// </remarks>
    public static string FixName(this string name)
        => FixName(name.AsSpan()).ToString();

    /// <summary> Given a full path string, return the base name and folder to save. </summary>
    /// <param name="fullPath"> The full filesystem path of a node. </param>
    /// <param name="dataName"> The name of the data object to validate the base name. </param>
    /// <param name="folder"> The returned folder name or the empty string if the node does not lie in a folder. </param>
    /// <returns>
    ///   The base name, which is empty if the node's display name corresponds to <paramref name="dataName"/>
    ///   after fixing the latter and removing duplicate markers from the former,
    ///   and otherwise the display name stripped of its duplicate markers.
    /// </returns>
    public static ReadOnlySpan<char> GetBaseName(this ReadOnlySpan<char> fullPath, ReadOnlySpan<char> dataName, out ReadOnlySpan<char> folder)
    {
        
        var nodeName = fullPath;
        folder = ReadOnlySpan<char>.Empty;

        var nameIdx = fullPath.LastIndexOf('/');
        if (nameIdx >= 0)
        {
            folder = nodeName[..nameIdx];
            ++nameIdx;
            nodeName   = nameIdx == fullPath.Length ? ReadOnlySpan<char>.Empty : nodeName[nameIdx..];
        }

        var fixedData = dataName.FixName();
        if (fixedData.SequenceEqual(nodeName))
            return ReadOnlySpan<char>.Empty;

        IsDuplicateName(nodeName, out nodeName, out _);
        if (fixedData.SequenceEqual(nodeName))
            return ReadOnlySpan<char>.Empty;

        return nodeName;
    }

    /// <inheritdoc cref="FixName(string)"/>
    public static ReadOnlySpan<char> FixName(this ReadOnlySpan<char> name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length is 0)
            return "<None>";

        var idx = trimmed.IndexOf('/');
        if (idx < 0)
            return trimmed;

        return string.Create(trimmed.Length, name, (c, state) => { state.Replace(c, '/', '\\'); });
    }

    /// <summary> Check if a string is a duplicated string with appended number. </summary>
    /// <param name="name"> The name to check. </param>
    /// <param name="baseName"> If the string is duplicated, the baseName without " (number)". </param>
    /// <param name="number"> If the string is duplicated, the duplicated number. </param>
    /// <returns> True if the string is duplicated and the output values are filled. </returns>
    public static bool IsDuplicateName(this ReadOnlySpan<char> name, out ReadOnlySpan<char> baseName, out int number)
    {
        // Duplicates should have the form '[Text] ([Number])'
        // Check for trailing ')'.
        if (name.EndsWith(')'))
        {
            // Check for opening '(', non-empty content and prior ' '.
            var idx = name.LastIndexOf('(');
            if (idx >= 2 && idx != name.Length - 2 && name[idx - 1] is ' ')
            {
                // Check if the content can be parsed to a non-negative integer.
                var potentialNumber = name[(idx + 1)..^1];
                if (uint.TryParse(potentialNumber, out var successfulNumber))
                {
                    number   = (int)successfulNumber;
                    baseName = name[..(idx - 1)];
                    return true;
                }
            }
        }

        baseName = name;
        number   = 0;
        return false;
    }

    /// <inheritdoc cref="IsDuplicateName(ReadOnlySpan{char},out ReadOnlySpan{char},out int)"/>
    public static bool IsDuplicateName(this string name, out ReadOnlySpan<char> baseName, out int number)
        => IsDuplicateName(name.AsSpan(), out baseName, out number);

    /// <summary> Obtain a unique file name with appended numbering if the file or directory name exists already.
    /// </summary>
    /// <param name="name"> The name of the file with or without already appended number. </param>
    /// <param name="maxDuplicates"> The maximum amount of duplicates to try for.</param>
    /// <returns>
    ///   The base string with the correct appended number to have a unique name. <br/>
    ///   An empty string if the given string is empty or if the maximum amount of accepted duplicates is reached.
    /// </returns>
    public static string ObtainUniqueFile(this string name, int maxDuplicates = int.MaxValue)
        => ObtainUniqueString(name, Path.Exists, maxDuplicates);

    /// <summary> Obtain a unique string with appended numbering if the name is not unique as determined by the predicate. </summary>
    /// <param name="name"> The name of the file with or without already appended number. </param>
    /// <param name="isDuplicate"> The function used to check whether a string exists already. </param>
    /// <param name="maxDuplicates"> The maximum amount of duplicates to try for.</param>
    /// <returns>
    ///   The base string with the correct appended number to have a unique name. <br/>
    ///   An empty string if the given string is empty or if the maximum amount of accepted duplicates is reached.
    /// </returns>
    public static string ObtainUniqueString(this string name, Predicate<string> isDuplicate, int maxDuplicates = int.MaxValue)
    {
        if (name.Length is 0 || !isDuplicate(name))
            return name;

        if (!name.IsDuplicateName(out var baseName, out _))
            baseName = name;

        var idx     = 2;
        var newName = $"{baseName} ({idx})";
        while (isDuplicate(newName))
        {
            newName = $"{baseName} ({++idx})";
            if (idx == maxDuplicates)
                return string.Empty;
        }

        return newName;
    }

    /// <summary> Increment the duplication part of a given name. </summary>
    /// <param name="name"> The given name. </param>
    /// <returns> The name with the number incremented by 1 if it is a duplicate name already, otherwise the name with ' (2)' appended. </returns>
    public static string IncrementDuplicate(this ReadOnlySpan<char> name)
        => name.IsDuplicateName(out var baseName, out var idx) ? $"{baseName} ({idx + 1})" : $"{name} (2)";

    /// <inheritdoc cref="IncrementDuplicate(ReadOnlySpan{char})"/>
    public static string IncrementDuplicate(this string name)
        => name.AsSpan().IncrementDuplicate();

    /// <summary> Split a path string into directories. </summary>
    /// <remarks> Empty entries will be skipped. </remarks>
    public static string[] SplitDirectories(this string path)
        => path.Split('/', StringSplitOptions.RemoveEmptyEntries);

    /// <summary> Split a full path into its first part and the remaining path, while skipping empty or whitespace entries. </summary>
    /// <param name="path"> The full path to split the first directory off. </param>
    /// <param name="firstPart"> The first non-empty split-off and trimmed part. </param>
    /// <returns> The remaining path after splitting the parts off until a non-empty part is found and trimming whitespace. </returns>
    public static ReadOnlySpan<char> SplitDirectory(this ReadOnlySpan<char> path, out ReadOnlySpan<char> firstPart)
    {
        do
        {
            var firstSplit = path.IndexOf('/');
            if (firstSplit < 0)
            {
                firstPart = path;
                return [];
            }

            firstPart = path[..firstSplit].Trim();
            if (firstSplit == path.Length - 1)
                path = [];
            else
                path = path[(firstSplit + 1)..].TrimStart();
        } while (firstPart.Length is 0 && path.Length > 0);

        return path;
    }
}
