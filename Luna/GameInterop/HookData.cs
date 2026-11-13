using Dalamud.Hooking;

namespace Luna;

public interface IHookData
{
    public void Enable();
    public void Disable();

    public string        Name      { get; }
    public IDalamudHook? Hook      { get; }
    public Exception?    Error     { get; }
    public long          Timestamp { get; }
    public string        Signature { get; }

    public void Deconstruct(out IDalamudHook? hook, out long timestamp, out Exception? error, out string signature);
}

internal sealed record HookData<T>(HookManager Parent, string Name, Hook<T>? Hook, long Timestamp, Exception? Error, string Signature)
    : IHookData
    where T : Delegate
{
    public void Enable()
    {
        Hook?.Enable();
        Parent.InvokeHookStateChanged(Name);
    }

    public void Disable()
    {
        Hook?.Disable();
        Parent.InvokeHookStateChanged(Name);
    }

    IDalamudHook? IHookData.Hook
        => Hook;

    public void Deconstruct(out IDalamudHook? hook, out long timestamp, out Exception? error, out string signature)
    {
        hook      = Hook;
        timestamp = Timestamp;
        error     = Error;
        signature = Signature;
    }
}
