namespace Luna;

/// <summary> An <see cref="IIntermediaryPlugin{TPlugin}"/> that does nothing and just launches the main plugin and re-throws load failures. </summary>
/// <typeparam name="TPlugin"> The main plugin definition. </typeparam>
/// <param name="parent"> The loader responsible for loading this intermediary plugin. </param>
public sealed class NopIntermediary<TPlugin>(PluginLoader<TPlugin> parent) : IIntermediaryPlugin<TPlugin>
    where TPlugin : class, IPluginDefinition<TPlugin>, IAsyncDisposable
{
    /// <inheritdoc/>
    public PluginLoader<TPlugin> Parent
        => parent;

    /// <inheritdoc/>
    public Task<bool> ValidateAsync(CancellationToken cancel)
        => Task.FromResult(true);

    /// <inheritdoc/>
    public Task OnValidationFailedAsync(CancellationToken cancel)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<PluginInitializationFailure.FailureHandling> OnPluginInitializationFailedAsync(PluginInitializationFailure arguments,
        CancellationToken cancel)
        => Task.FromResult(PluginInitializationFailure.FailureHandling.Rethrow);

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;
}
