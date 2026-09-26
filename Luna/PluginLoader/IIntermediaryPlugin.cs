namespace Luna;

/// <summary> The intermediary plugin handling plugin validation and plugin initialization failures. </summary>
/// <typeparam name="TPlugin"> The main plugin definition. </typeparam>
public interface IIntermediaryPlugin<TPlugin> : IAsyncDisposable
    where TPlugin : class, IPluginDefinition<TPlugin>, IAsyncDisposable
{
    /// <summary> The loader responsible for loading this intermediary plugin. </summary>
    public PluginLoader<TPlugin> Parent { get; }

    /// <summary> Validate whether the plugin is in a valid environment and version to load. </summary>
    /// <param name="cancel"> A cancellation token. </param>
    /// <returns> True if the plugin can be loaded immediately, false if the intermediary plugin should stay loaded. </returns>
    /// <remarks>
    ///   This should prepare any display for validation failures if this intermediary supports displaying them. <br/>
    ///   If this throws, the exception is thrown back to the PluginLoader and the plugin will fail to load.
    /// </remarks>
    public Task<bool> ValidateAsync(CancellationToken cancel);

    /// <summary> Invoked later if <see cref="ValidateAsync"/> returned false. </summary>
    /// <param name="cancel"> A cancellation token. </param>
    /// <returns> An awaiter. </returns>
    public Task OnValidationFailedAsync(CancellationToken cancel);

    /// <summary> Invoked by the <see cref="Parent"/> loader if initialization of the main plugin fails. </summary>
    /// <param name="arguments"> The arguments passed by the loader. </param>
    /// <param name="cancel"> A cancellation token. </param>
    /// <returns> Whether the exception was handled by the intermediary or should be rethrown by the loader. </returns>
    public Task<PluginInitializationFailure.FailureHandling> OnPluginInitializationFailedAsync(PluginInitializationFailure arguments,
        CancellationToken cancel);

    /// <summary> Initialize the intermediary plugin. </summary>
    /// <param name="cancel"> A cancellation token. </param>
    /// <returns> An awaiter. </returns>
    public async Task Initialize(CancellationToken cancel)
    {
        cancel.ThrowIfCancellationRequested();
        bool pluginValidation;
        try
        {
            pluginValidation = await ValidateAsync(cancel).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Parent.Log.Error($"Plugin construction failed during validation:\n{ex}");
            throw;
        }

        cancel.ThrowIfCancellationRequested();
        // If the validation succeeds, immediately initialize the main plugin.
        if (pluginValidation)
        {
            // Initialization failures are forwarded to the intermediary.
            // The intermediary may handle or propagate them.
            await Parent.InitializePluginAsync(cancel).ConfigureAwait(false);
            return;
        }

        cancel.ThrowIfCancellationRequested();
        // If it does not succeed, handle the validation failure.
        try
        {
            await OnValidationFailedAsync(cancel).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Parent.Log.Error($"Plugin construction failed when setting up intermediary for unsuccessful validation:\n{ex}");
            throw;
        }
    }
}
