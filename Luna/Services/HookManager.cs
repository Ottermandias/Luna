using Dalamud.Hooking;
using Dalamud.Plugin.Services;

namespace Luna;

/// <summary> A utility to asynchronously create hooks, and dispose of them. </summary>
public sealed class HookManager(IGameInteropProvider provider) : IDisposable, IService
{
    public readonly  IGameInteropProvider                                               Provider = provider;
    private readonly CancellationTokenSource                                            _cancel  = new();
    private readonly ConcurrentDictionary<string, (IDalamudHook?, long, Exception? ex)> _hooks   = [];
    private          Task?                                                              _currentTask;
    private          bool                                                               _disposed;
    public           bool                                                               HasExceptions { get; private set; }

    /// <summary> Get the data of all currently hooked methods. </summary>
    public IEnumerable<(string Name, nint Address, long Time, Type Delegate)> Diagnostics
        => _disposed
            ? []
            : _hooks.Select(kvp => (kvp.Key, kvp.Value.Item1?.Address ?? nint.Zero, kvp.Value.Item2,
                kvp.Value.Item1?.GetType().GenericTypeArguments[0] ?? typeof(void)));

    /// <summary> Create a hook for a given address. </summary>
    public Task<Hook<T>?> CreateHook<T>(string name, nint address, T detour, bool enable = false) where T : Delegate
    {
        CheckDisposed();
        if (address <= 0)
            throw new Exception($"Creating Hook {name} failed: address 0x{address:X} is invalid.");

        return AppendTask(Func);

        Hook<T>? Func()
        {
            _cancel.Token.ThrowIfCancellationRequested();
            var      timer = Stopwatch.StartNew();
            Hook<T>? hook  = null;
            try
            {
                hook = Provider.HookFromAddress(address, detour);
                if (enable)
                    hook.Enable();

                _cancel.Token.ThrowIfCancellationRequested();
                AddHook(name, hook, timer, null);
                return hook;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                AddHook(name, hook, timer, ex);
                return null;
            }
        }
    }

    /// <summary> Create a hook from a given signature. </summary>
    public Task<Hook<T>?> CreateHook<T>(string name, string signature, T detour, bool enable = false) where T : Delegate
    {
        CheckDisposed();
        return AppendTask(Func);

        Hook<T>? Func()
        {
            _cancel.Token.ThrowIfCancellationRequested();
            var      timer = Stopwatch.StartNew();
            Hook<T>? hook  = null;
            try
            {
                hook = Provider.HookFromSignature(signature, detour);
                if (enable)
                    hook.Enable();
                _cancel.Token.ThrowIfCancellationRequested();
                AddHook(name, hook, timer, null);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                AddHook(name, hook, timer, ex);
            }

            return hook;
        }
    }

    /// <summary> Try to replace an existing hook with a different detour. </summary>
    public Task<Hook<T>?> TryReplaceHook<T>(string name, T detour) where T : Delegate
    {
        CheckDisposed();
        return AppendTask(Func);

        Hook<T>? Func()
        {
            _cancel.Token.ThrowIfCancellationRequested();
            var timer = Stopwatch.StartNew();
            if (!_hooks.TryRemove(name, out var oldHook))
                return null;

            var enabled = oldHook.Item1?.IsEnabled ?? false;
            oldHook.Item1?.Dispose();
            var newHook = oldHook.Item1 is null ? null : Provider.HookFromAddress(oldHook.Item1.Address, detour);
            if (enabled)
                newHook?.Enable();
            _cancel.Token.ThrowIfCancellationRequested();
            AddHook(name, newHook, timer, null);
            return newHook;
        }
    }

    /// <summary> Dispose an existing hook. </summary>
    public Task<bool> DisposeHook(string name)
    {
        CheckDisposed();
        return AppendTask(Func);

        bool Func()
        {
            _cancel.Token.ThrowIfCancellationRequested();
            if (!_hooks.TryRemove(name, out var hook))
                return false;

            hook.Item1?.Dispose();
            return true;
        }
    }

    /// <summary> Append a new hooking task to the current task. </summary>
    private Task<T> AppendTask<T>(Func<T> func)
    {
        Task<T> task;
        lock (_hooks)
        {
            task = _currentTask is null || _currentTask.IsCompleted
                ? Task.Run(func, _cancel.Token)
                : _currentTask.ContinueWith(_ => func(), _cancel.Token,
                    TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted, TaskScheduler.Default);
            _currentTask = task;
        }

        return task;
    }

    /// <summary> Check whether the hook manager was disposed already. </summary>
    private void CheckDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(HookManager));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;


        lock (_hooks)
        {
            _cancel.Cancel();
            _currentTask?.Wait();
            _disposed = true;
            foreach (var (_, hook) in _hooks)
                hook.Item1?.Dispose();
            _hooks.Clear();
            _currentTask = null;
        }
    }

    /// <summary> Add the hook and throw on failure. </summary>
    private void AddHook(string name, IDalamudHook? hook, Stopwatch timer, Exception? ex)
    {
        HasExceptions |= ex is not null;
        if (!_hooks.TryAdd(name, (hook, timer.ElapsedMilliseconds, ex)))
            throw new Exception($"A hook with the name of {name} already exists.");
    }
}
