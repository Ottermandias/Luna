using Dalamud.Hooking;

namespace Luna;

/// <summary> A base class for fast setup of basic hooks. </summary>
/// <typeparam name="T"> The delegate type for the hook. </typeparam>
public abstract class FastHook<T> : IHookService where T : Delegate
{
    /// <summary> The task to launch to obtain the hook, that will also ultimately contain the hook. </summary>
    protected Task<Hook<T>> Task { get; init; } = null!;

    /// <summary> A non-generic awaiter task to wait for completion of the hook. </summary>
    public Task Awaiter
        => Task;

    /// <summary> Get whether the hook is available. </summary>
    public bool Finished
        => Task.IsCompletedSuccessfully;

    /// <summary> Get the address queried for the hook. </summary>
    public nint Address
        => Task.Result.Address;

    /// <summary> Enable the hook. </summary>
    public void Enable()
        => Task.Result.Enable();

    /// <summary> Disable the hook. </summary>
    public void Disable()
        => Task.Result.Disable();

    /// <summary> Set the hook's state. </summary>
    /// <param name="value"> True toggles on, false toggles off. </param>
    public void Set(bool value)
    {
        if (value)
            Enable();
        else
            Disable();
    }
}
