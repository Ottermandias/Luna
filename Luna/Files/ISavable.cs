namespace Luna;

/// <summary> Any file type that we want to save via SaveService needs to implement this. </summary>
public interface ISavable<in T> where T : BaseFilePathProvider
{
    /// <summary> Get The full file path of a given object. </summary>
    public string ToFilePath(T fileNames);

    /// <summary> Write the objects data to the given stream writer. </summary>
    public void Save(StreamWriter writer);

    /// <summary> An arbitrary message printed to Debug before saving. </summary>
    public string LogName(string fileName)
        => fileName;

    /// <summary> The displayed name of the type. </summary>
    public string TypeName
        => GetType().Name;
}
