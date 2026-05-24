using System.Collections.Immutable;
using Dalamud.Plugin.Services;

namespace Luna.DirectX;

/// <summary>
///   A graph of image processing effects and dependencies between them.
///   Handles execution planning and thread hopping.
/// </summary>
public class EffectGraph : ISet<IEffect>, IDisposable
{
    private readonly HashSet<IEffect> _effects;

    private ImmutableArray<IEffect> _effectPlan = [];

    private Execution? _currentExecution;

    /// <summary> Whether this effect graph is currently running. </summary>
    public bool Running
        => _currentExecution is { Task.IsCompleted: false };

    /// <summary> A task representing the current execution, if there is one, otherwise the last. </summary>
    public Task CurrentExecutionTask
        => _currentExecution is null ? Task.CompletedTask : _currentExecution.Task;

    /// <inheritdoc/>
    public int Count
        => _effects.Count;

    /// <inheritdoc/>
    bool ICollection<IEffect>.IsReadOnly
        => false;

    /// <summary> Creates a new empty effect graph. </summary>
    public EffectGraph()
        => _effects = [];

    /// <summary> Creates a new empty effect graph, with reserved space for a given number of effects. </summary>
    /// <param name="capacity"> The number of effects for which to reserve space. </param>
    public EffectGraph(int capacity)
        => _effects = new HashSet<IEffect>(capacity);

    /// <summary> Creates a new effect graph, containing the given effects. </summary>
    /// <param name="effects"> Effects to add to the graph. </param>
    public EffectGraph(IEnumerable<IEffect> effects)
        => _effects = [..effects];

    ~EffectGraph()
        => Dispose(false);

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary> Releases the resources used by this object. </summary>
    /// <param name="disposing"> True if called explicitly, false if garbage collected. </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        foreach (var effect in _effects)
        {
            if (effect is IDisposable disposable)
                disposable.Dispose();
        }

        _effects.Clear();
        _effectPlan = [];
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public IEnumerator<IEffect> GetEnumerator()
        => _effects.GetEnumerator();

    /// <inheritdoc/>
    void ICollection<IEffect>.Add(IEffect item)
        => Add(item);

    /// <inheritdoc/>
    public bool Add(IEffect item)
    {
        if (!_effects.Add(item))
            return false;

        _effectPlan = default;
        return true;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _effects.Clear();
        _effectPlan = [];
    }

    /// <inheritdoc/>
    public bool Contains(IEffect item)
        => _effects.Contains(item);

    /// <inheritdoc/>
    public void CopyTo(IEffect[] array, int arrayIndex)
        => _effects.CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public bool Remove(IEffect item)
    {
        if (!_effects.Remove(item))
            return false;

        _effectPlan = default;
        return true;
    }

    /// <inheritdoc/>
    public void ExceptWith(IEnumerable<IEffect> other)
    {
        var count = _effects.Count;
        _effects.ExceptWith(Unwrap(other));
        if (_effects.Count != count)
            _effectPlan = default;
    }

    /// <inheritdoc/>
    public void IntersectWith(IEnumerable<IEffect> other)
    {
        var count = _effects.Count;
        _effects.IntersectWith(Unwrap(other));
        if (_effects.Count != count)
            _effectPlan = default;
    }

    /// <inheritdoc/>
    public bool IsProperSubsetOf(IEnumerable<IEffect> other)
        => _effects.IsProperSubsetOf(Unwrap(other));

    /// <inheritdoc/>
    public bool IsProperSupersetOf(IEnumerable<IEffect> other)
        => _effects.IsProperSupersetOf(Unwrap(other));

    /// <inheritdoc/>
    public bool IsSubsetOf(IEnumerable<IEffect> other)
        => _effects.IsSubsetOf(Unwrap(other));

    /// <inheritdoc/>
    public bool IsSupersetOf(IEnumerable<IEffect> other)
        => _effects.IsSubsetOf(Unwrap(other));

    /// <inheritdoc/>
    public bool Overlaps(IEnumerable<IEffect> other)
        => _effects.Overlaps(Unwrap(other));

    /// <inheritdoc/>
    public bool SetEquals(IEnumerable<IEffect> other)
        => _effects.SetEquals(Unwrap(other));

    /// <inheritdoc/>
    public void SymmetricExceptWith(IEnumerable<IEffect> other)
    {
        if (other.TryGetNonEnumeratedCount(out var count) && count is 0)
            return;

        _effects.SymmetricExceptWith(Unwrap(other));
        _effectPlan = default;
    }

    /// <inheritdoc/>
    public void UnionWith(IEnumerable<IEffect> other)
    {
        var count = _effects.Count;
        _effects.UnionWith(Unwrap(other));
        if (_effects.Count != count)
            _effectPlan = default;
    }

    private static IEnumerable<IEffect> Unwrap(IEnumerable<IEffect> effects)
        => effects is EffectGraph graph ? graph._effects : effects;

    /// <summary> Invalidates the current execution plan of this graph. </summary>
    /// <remarks> This is automatically done when adding or removing effects, but must be called manually when changing dependencies. </remarks>
    public void InvalidatePlan()
        => _effectPlan = default;

    /// <summary> Runs the effects in this graph, in an order that ensures dependencies are met. </summary>
    /// <param name="framework"> Dalamud's framework service. </param>
    /// <param name="cancellationToken"> A cancellation token. </param>
    /// <returns> A task that represents this effect graph running. </returns>
    /// <exception cref="InvalidOperationException"> This effect graph is already running, or contains a cyclic dependency. </exception>
    public unsafe Task Run(IFramework framework, CancellationToken cancellationToken = default)
    {
        if (_currentExecution is { Task.IsCompleted: false })
            throw new InvalidOperationException("This EffectGraph is already running");

        var run = new Execution(Plan(), cancellationToken);
        _currentExecution = run;

        // If we aren't in an ImGui frame, or if we have work that cannot be completed synchronously, enable per-frame processing ticks.
        if (!framework.IsInFrameworkUpdateThread || !Im.Context.Pointer->WithinFrameScope || run.Tick())
            ImSharpPerFrame.Update += run.PerFrameTick;

        return run.Task;
    }

    /// <summary> Calculates an execution plan for this effect graph, taking into account dependencies. </summary>
    /// <returns> An array of this graph's effects, in an order of execution that ensures dependencies are met. </returns>
    /// <exception cref="InvalidOperationException"> This effect graph contains a cyclic dependency. </exception>
    public ImmutableArray<IEffect> Plan()
    {
        if (!_effectPlan.IsDefault)
            return _effectPlan;

        var effects       = new IEffect[_effects.Count];
        var index         = 0;
        var cycleDetector = new Stack<IEffect>();
        foreach (var effect in _effects)
            Plan(effects, ref index, effect, cycleDetector);

        _effectPlan = [..effects];
        return _effectPlan;
    }

    private void Plan(IEffect[] effects, ref int index, IEffect effect, Stack<IEffect> cycleDetector)
    {
        if (effects.AsSpan(0, index).Contains(effect))
            return;

        if (cycleDetector.Contains(effect))
            throw new InvalidOperationException("Cyclic dependency in DxEffectGraph");

        cycleDetector.Push(effect);
        try
        {
            foreach (var dependency in effect.GetDependencies())
            {
                if (_effects.Contains(dependency))
                    Plan(effects, ref index, dependency, cycleDetector);
            }

            effects[index++] = effect;
        }
        finally
        {
            cycleDetector.Pop();
        }
    }

    private sealed class Execution(ImmutableArray<IEffect> effects, CancellationToken cancellationToken)
    {
        private readonly TaskCompletionSource _taskCompletionSource = new();

        private int   _nextIndex;
        private Task? _currentTask;

        public Task Task
            => _taskCompletionSource.Task;

        /// <summary> Runs some of the effect processing. </summary>
        /// <returns> Whether this function should be called again on the next frame. </returns>
        public bool Tick()
        {
            var task = _currentTask;
            if (task is not null)
            {
                if (!task.IsCompleted)
                {
                    (effects[_nextIndex - 1] as ITickingEffect)?.Tick();
                    return true;
                }

                _currentTask = null;
                if (!task.IsCompletedSuccessfully)
                {
                    _taskCompletionSource.SetFromTask(task);
                    return false;
                }
            }

            while (_nextIndex < effects.Length)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _taskCompletionSource.SetCanceled(cancellationToken);
                    return false;
                }

                try
                {
                    task = effects[_nextIndex++].Run(cancellationToken);
                }
                catch (Exception e)
                {
                    _taskCompletionSource.SetException(e);
                    return false;
                }

                if (!task.IsCompleted)
                {
                    _currentTask = task;
                    return true;
                }

                if (!task.IsCompletedSuccessfully)
                {
                    _taskCompletionSource.SetFromTask(task);
                    return false;
                }
            }

            _taskCompletionSource.SetResult();
            return false;
        }

        public void PerFrameTick()
        {
            if (!Tick())
                ImSharpPerFrame.Update -= PerFrameTick;
        }
    }
}
