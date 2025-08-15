using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;

namespace Luna;

/// <summary> A helper service for keyboard interaction between ImGui and the FFXIV key state. </summary>
public sealed class KeyboardManager : IService, IDisposable
{
    private readonly IFramework                         _framework;
    private readonly IKeyState                          _keyState;
    private          int                                _debugCount;
    private readonly Dictionary<ModifiableHotkey, bool> _registeredKeys = [];

    /// <summary> Create a new keyboard manager and subscribe to framework update events. </summary>
    public KeyboardManager(IFramework framework, IKeyState keyState)
    {
        _framework        =  framework;
        _keyState         =  keyState;
        _framework.Update += OnUpdate;
    }

    /// <summary> Register a specific key combination for this frame, causing ImGui to intercept key input during this frame if and only if the associated modifiers are held. </summary>
    /// <param name="key"> The optionally modified hotkey to register. </param>
    /// <returns> The registered hotkey itself. </returns>
    public ModifiableHotkey RegisterKey(ModifiableHotkey key)
    {
        var modifiers = key.Modifiers.IsActive();
        if (_registeredKeys.TryAdd(key, modifiers) && modifiers)
            Im.GetIo().CaptureTextInput = true;
        return key;
    }

    /// <inheritdoc cref="RegisterKey(VirtualKey,ModifierHotkey,ModifierHotkey)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ModifiableHotkey RegisterKey(VirtualKey key)
        => RegisterKey(new ModifiableHotkey(key));

    /// <inheritdoc cref="RegisterKey(VirtualKey,ModifierHotkey,ModifierHotkey)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ModifiableHotkey RegisterKey(VirtualKey key, ModifierHotkey modifier1)
        => RegisterKey(new ModifiableHotkey(key, modifier1));

    /// <summary> Register a specific key combination for this frame, causing ImGui to intercept key input during this frame if and only if the associated modifiers are held. </summary>
    /// <param name="key"> The hotkey to register. </param>
    /// <param name="modifier1"> The first optional modifier required to register, see <see cref="ModifiableHotkey"/>. </param>
    /// <param name="modifier2"> The second optional modifier required to register, see <see cref="ModifiableHotkey"/>. </param>
    /// <returns> The registered hotkey itself. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ModifiableHotkey RegisterKey(VirtualKey key, ModifierHotkey modifier1, ModifierHotkey modifier2)
        => RegisterKey(new ModifiableHotkey(key, modifier1, modifier2));

    /// <summary> Draw debug information about the current state of this keyboard manager, the ImGui key state and the game's key state. </summary>
    public void DrawDebug()
    {
        Im.Button("Press Ctrl + V"u8);
        if (Im.Item.Hovered())
        {
            var key = RegisterKey(VirtualKey.V, ModifierHotkey.Control);
            if (key.Modifiers.Modifier1.IsActive() && Im.Keyboard.IsPressed(key.Hotkey.ToImGuiKey()))
                ++_debugCount;
        }

        Im.Line.Same();
        Im.Text($"Pressed {_debugCount} times.");

        Im.Separator();
        var active   = ImGuiColor.Button.Get();
        var inactive = ImGuiColor.FrameBackground.Get();
        ImEx.TextFramed("Control"u8, frameColor: ModifierHotkey.Control.IsActive() ? active : inactive);
        Im.Line.SameInner();
        ImEx.TextFramed("Shift"u8, frameColor: ModifierHotkey.Shift.IsActive() ? active : inactive);
        Im.Line.SameInner();
        ImEx.TextFramed("Alt"u8, frameColor: ModifierHotkey.Alt.IsActive() ? active : inactive);
        Im.Line.SameInner();
        ImEx.TextFramed("Capture Text Input"u8, frameColor: Im.Io.CaptureTextInput ? active : inactive);
        Im.Line.SameInner();
        ImEx.TextFramed("Capture Keyboard"u8, frameColor: Im.Io.CaptureKeyboard ? active : inactive);

        Im.Separator();
        using (var table = Im.Table.Begin("table"u8, 3))
        {
            if (!table)
                return;

            foreach (var (key, modifiers) in _registeredKeys)
            {
                table.DrawColumn($"{key}");
                var pressed = Im.Keyboard.IsDown(key.Hotkey.ToImGuiKey());
                table.DrawColumn(pressed ? "Pressed"u8 : "No Input"u8);
                table.DrawColumn(modifiers ? "Modifiers Active"u8 : "Modifiers Inactive"u8);
            }
        }

        Im.Separator();

        using var tree = Im.Tree.Node("Full Keystate"u8);
        if (!tree)
            return;

        using var table2 = Im.Table.Begin("table2"u8, 4, TableFlags.SizingFixedFit);
        if (!table2)
            return;

        table2.SetupColumn("Fancy Name"u8);
        table2.SetupColumn("ImGuiKey"u8);
        table2.SetupColumn("Game State"u8);
        table2.SetupColumn("ImGui State"u8);

        table2.HeaderRow();
        foreach (var key in _keyState.GetValidVirtualKeys())
        {
            var imguiKey = key.ToImGuiKey();
            table2.DrawColumn(key.GetFancyName());
            table2.DrawColumn($"{imguiKey}");
            table2.DrawColumn(_keyState[key] ? "Active"u8 : "Inactive"u8);
            table2.DrawColumn(Im.Keyboard.IsDown(imguiKey) ? "Active"u8 : "Inactive"u8);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
        => _framework.Update -= OnUpdate;

    /// <summary> Disables the registered keys in the games key state and clears registered keys on the next frame. </summary>
    private void OnUpdate(IFramework framework)
    {
        foreach (var (key, modifiers) in _registeredKeys)
        {
            if (modifiers)
                _keyState[key.Hotkey] = false;
        }

        _registeredKeys.Clear();
    }
}
