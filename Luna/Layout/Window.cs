using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace Luna;

/// <summary> Wrapper to avoid Dalamud.Bindings.Imgui as much as possible while still using <see cref="WindowSystem"/>. </summary>
public abstract class Window : Dalamud.Interface.Windowing.Window
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
