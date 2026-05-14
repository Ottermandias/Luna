using Dalamud.Bindings.ImGui;

namespace Luna;

/// <summary> A helper class that can be used without referencing Dalamud.Bindings.ImGui for its Click handler. </summary>
public sealed class TitleBarButton : Dalamud.Interface.Windowing.TitleBarButton
{
    /// <summary> Subscribe to Dalamud's Click handler. </summary>
    public TitleBarButton()
        => base.Click += OnClick;

    /// <inheritdoc cref="Dalamud.Interface.Windowing.TitleBarButton.Click"/>
    public new event Action<MouseButton>? Click;

    /// <summary> Click-redirection due to incompatible imgui types.</summary>
    private void OnClick(ImGuiMouseButton button)
        => Click?.Invoke((MouseButton)button);
}

/// <summary> Wrapper to avoid Dalamud.Bindings.Imgui as much as possible while still using <see cref="WindowSystem"/>. </summary>
public abstract class Window : Dalamud.Interface.Windowing.Window, IUiService
{
    /// <inheritdoc/>
    protected Window(string name, WindowFlags flags = WindowFlags.None, bool forceMainWindow = false)
        : base(name, (ImGuiWindowFlags)flags, forceMainWindow)
    { }

    /// <inheritdoc/>
    protected Window(string name)
        : base(name)
    { }

    /// <inheritdoc cref="Dalamud.Interface.Windowing.Window.PositionCondition"/>
    public new Condition PositionCondition
    {
        get => (Condition)base.PositionCondition;
        set => base.PositionCondition = (ImGuiCond)value;
    }

    /// <inheritdoc cref="Dalamud.Interface.Windowing.Window.SizeCondition"/>
    public new Condition SizeCondition
    {
        get => (Condition)base.SizeCondition;
        set => base.SizeCondition = (ImGuiCond)value;
    }

    /// <inheritdoc cref="Dalamud.Interface.Windowing.Window.CollapsedCondition"/>
    public new Condition CollapsedCondition
    {
        get => (Condition)base.CollapsedCondition;
        set => base.CollapsedCondition = (ImGuiCond)value;
    }

    /// <inheritdoc cref="Dalamud.Interface.Windowing.Window.Flags"/>
    public new WindowFlags Flags
    {
        get => (WindowFlags)base.Flags;
        set => base.Flags = (ImGuiWindowFlags)value;
    }
}

/// <summary> A base class for any type of window which should not support Dalamud's click-through, pinning or cause sounds. </summary>
public abstract class OverlayWindow : Window
{
    /// <inheritdoc/>
    protected OverlayWindow(string name, WindowFlags flags = WindowFlags.None, bool forceMainWindow = false)
        : base(name, flags, forceMainWindow)
    {
        DisableWindowSounds = true;
        AllowClickthrough   = false;
        AllowPinning        = false;
        AllowBackgroundBlur = false;
    }

    /// <inheritdoc/>
    protected OverlayWindow(string name)
        : base(name)
    {
        DisableWindowSounds = true;
        AllowClickthrough   = false;
        AllowPinning        = false;
        AllowBackgroundBlur = false;
    }
}
