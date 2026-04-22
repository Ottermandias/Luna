using System.Text.Json;

namespace Luna;

/// <summary> An object that can be parsed from JSON. </summary>
public interface IJsonParsable
{
    /// <summary> Method invoked when a JSON recovery has happened in <see cref="ReadJson{TRet}"/>. </summary>
    /// <param name="filePath"> The file path affected. </param>
    /// <param name="flags"> The recovery methods used. </param>
    public virtual static void OnRecovery(string filePath, JsonRecoveryFlags flags)
    { }

    /// <summary> Try to read an UTF8-encoded JSON file while using recovery strategies on failure. </summary>
    /// <typeparam name="TRet"> The type of the data we are trying to parse. </typeparam>
    /// <param name="filePath"> The full path to the file. </param>
    /// <param name="replace"> Whether to back up and replace the file if recovery is triggered and successful. </param>
    /// <returns> The parsed object. </returns>
    public static TRet ReadJson<TRet>(string filePath, bool replace = true) where TRet : IJsonParsable<TRet>
    {
        // Read the data as UTF8 bytes.
        var bytes = JsonFunctions.ReadUtf8Bytes(filePath, out var originalData);
        try
        {
            // Try to parse the data with the return types Read function.
            var reader = new Utf8JsonReader(bytes.Span, JsonFunctions.ReaderOptions);
            return TRet.Read(ref reader);
        }
        // Catch only JSON exceptions to try and recover.
        catch (JsonException ex)
        {
            try
            {
                // Try to recover valid JSON from the read data.
                var (recovered, _, recoveries) = JsonFunctions.RecoverBytes(originalData, true, JsonRecoveryFlags.Safe);
                // Try to parse the recovered JSON.
                var reader = new Utf8JsonReader(recovered, JsonFunctions.ReaderOptions);
                var ret    = TRet.Read(ref reader);
                // Log the recovery of the file.
                TRet.OnRecovery(filePath, recoveries);

                // If replacement is enabled, try to replace the file with the recovered data.
                if (replace)
                    WriteRecoveredData(recovered, filePath);
                return ret;
            }
            // If any step here fails, ignore it and re-throw the original JSON exception.
            catch
            {
                throw ex;
            }
        }
    }

    /// <summary> Safely replace a file with the given data while creating a timestamped backup. </summary>
    /// <param name="data"> The data to write to the new file. </param>
    /// <param name="filePath"> The original file path. </param>
    /// <remarks> Does not throw or log on failures. </remarks>
    private static void WriteRecoveredData(byte[] data, string filePath)
    {
        var tmpPath    = Path.ChangeExtension(filePath, ".tmp");
        var backupPath = Path.ChangeExtension(filePath, $"_{DateTime.Now:yyyyMMddHHmmss}.json.bak");
        try
        {
            // Write the recovered data first to a temporary file.
            File.WriteAllBytes(tmpPath, data);

            // Then move the existing data to a timestamped backup
            File.Move(filePath, backupPath, true);

            // Then move the temporary file to the actual file path.
            File.Move(tmpPath, filePath, true);
        }
        catch
        {
            // On failures, try to clean up the temporary file and move the backup back. Neither of those should generally happen.
            try
            {
                File.Delete(tmpPath);
                File.Move(backupPath, filePath);
            }
            catch
            {
                // Ignored
            }
        }
    }
}

/// <inheritdoc/>
public interface IJsonParsable<out TSelf> : IJsonParsable
{
    /// <summary> Read and return this object from JSON. </summary>
    /// <param name="reader"> The JSON reader. </param>
    /// <returns> The read and parsed object. </returns>
    public abstract static TSelf Read(ref Utf8JsonReader reader);
}
