using System.Text.Json;

namespace Luna;

/// <summary> An object that can be parsed from JSON. </summary>
public interface IJsonParsable
{
    /// <summary> Method invoked when a JSON recovery has happened in <see cref="ReadJson{TRet}"/>. </summary>
    /// <param name="log"> The logger to use. </param>
    /// <param name="filePath"> The file path affected. </param>
    /// <param name="flags"> The recovery methods used. </param>
    /// <param name="userInput"> An additional, optional user-defined object passed to the other functions. </param>
    public virtual static void OnRecovery(LunaLogger log, string filePath, JsonRecoveryFlags flags, object? userInput = null)
    {
        var sb = new StringBuilder("Recovered valid JSON from ");
        sb.Append(filePath);
        sb.Append(" using ");
        flags.AddToString(sb);
        sb.Append('.');
        log.Warning(sb.ToString());
    }

    /// <summary> Try to read an UTF8-encoded JSON file while using recovery strategies on failure. </summary>
    /// <typeparam name="TRet"> The type of the data we are trying to parse. </typeparam>
    /// <param name="saveService"> The service used to save the recovered data. </param>
    /// <param name="filePath"> The full path to the file. </param>
    /// <param name="replace"> Whether to back up and replace the file if recovery is triggered and successful. </param>
    /// <param name="userInput"> An additional, optional user-defined object passed to the other functions. </param>
    /// <returns> The parsed object. </returns>
    public static TRet ReadJson<TRet>(BaseSaveService saveService, string filePath, bool replace = true, object? userInput = null)
        where TRet : IJsonParsable<TRet>, allows ref struct
    {
        // Read the data as UTF8 bytes.
        var bytes = JsonFunctions.ReadUtf8Bytes(filePath, out var originalData);
        try
        {
            // Try to parse the data with the return types Read function.
            var reader = new Utf8JsonReader(bytes.Span, JsonFunctions.ReaderOptions);
            return TRet.Read(ref reader, filePath, userInput);
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
                var ret    = TRet.Read(ref reader, filePath, userInput);
                // Log the recovery of the file.
                TRet.OnRecovery(saveService.Log, filePath, recoveries, userInput);

                // If replacement is enabled, try to replace the file with the recovered data.
                if (replace)
                    saveService.AtomicWriteWithBackup(filePath, f => File.WriteAllBytes(f, recovered));
                return ret;
            }
            // If any step here fails, ignore it and re-throw the original JSON exception.
            catch
            {
                throw ex;
            }
        }
    }
}

/// <inheritdoc/>
public interface IJsonParsable<out TSelf> : IJsonParsable where TSelf : allows ref struct
{
    /// <summary> Read and return this object from JSON. </summary>
    /// <param name="reader"> The JSON reader. </param>
    /// <param name="filePath"> The path to the file being read. </param>
    /// <param name="userInput"> An additional, optional user-defined object passed from the wrapper function. </param>
    /// <returns> The read and parsed object. </returns>
    public abstract static TSelf Read(scoped ref Utf8JsonReader reader, string filePath, object? userInput = null);
}
