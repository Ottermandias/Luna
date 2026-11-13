using Dalamud.Hooking;
using Dalamud.Plugin.Services;

namespace Luna;

/// <summary> A utility to asynchronously create hooks, and dispose of them. </summary>
public sealed class HookManager(IGameInteropProvider provider, ISigScanner sigScanner) : IDisposable, IScopedService
{
    public readonly  IGameInteropProvider                    Provider   = provider;
    public readonly  ISigScanner                             SigScanner = sigScanner;
    private readonly CancellationTokenSource                 _cancel    = new();
    private readonly ConcurrentDictionary<string, IHookData> _hooks     = [];
    private          Task?                                   _currentTask;
    private          bool                                    _disposed;
    public           bool                                    HasExceptions { get; private set; }

    /// <summary> Invoked whenever a new hook is added, removed, replaced, enabled or disabled. </summary>
    public event Action<string>? HookStateChanged;

    /// <summary> Log all exceptions that occured while creating hooks. </summary>
    /// <param name="log"> The logger to log to. </param>
    /// <returns> True if any exceptions occured, false otherwise. </returns>
    public bool LogExceptions(LunaLogger log)
    {
        _currentTask?.Wait();
        if (!HasExceptions)
            return false;

        foreach (var (name, (hook, _, ex, sig)) in _hooks)
        {
            if (hook is not null)
                continue;

            if (ex is null)
                log.Error($"Unknown error creating hook {name}{(sig.Length > 0 ? $" at {sig}" : string.Empty)}.");
            else
                log.Error($"Error creating hook {name}{(sig.Length > 0 ? $" at {sig}" : string.Empty)}:\n{ex}");
        }

        return true;
    }

    /// <summary> Get the data of all currently hooked methods. </summary>
    public IEnumerable<IHookData> Hooks
        => _hooks.Values;

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
                AddHook(name, hook, timer, null, $"0x{address:X}");
                return hook;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                AddHook(name, hook, timer, ex, $"0x{address:X}");
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
                AddHook(name, hook, timer, null, signature);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                AddHook(name, hook, timer, ex, signature);
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

            var enabled = oldHook.Hook?.IsEnabled ?? false;
            oldHook.Hook?.Dispose();
            var newHook = oldHook.Hook is null ? null : Provider.HookFromAddress(oldHook.Hook.Address, detour);
            if (enabled)
                newHook?.Enable();
            _cancel.Token.ThrowIfCancellationRequested();
            AddHook(name, newHook, timer, null, oldHook.Signature);
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

            hook.Hook?.Dispose();
            HookStateChanged?.Invoke(name);
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
                    TaskContinuationOptions.NotOnCanceled, TaskScheduler.Default);
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
                hook.Hook?.Dispose();
            _hooks.Clear();
            _currentTask = null;
        }
    }

    /// <summary> Add the hook and throw on failure. </summary>
    private void AddHook<T>(string name, Hook<T>? hook, Stopwatch timer, Exception? ex, string sig) where T : Delegate
    {
        HasExceptions |= ex is not null;
        if (!_hooks.TryAdd(name, new HookData<T>(this, name, hook, timer.ElapsedMilliseconds, ex, sig)))
            throw new Exception($"A hook with the name of {name} already exists.");

        HookStateChanged?.Invoke(name);
    }

    /// <summary> Invoke hook state changed. </summary>
    internal void InvokeHookStateChanged(string name)
        => HookStateChanged?.Invoke(name);
}
