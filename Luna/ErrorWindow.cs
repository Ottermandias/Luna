using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Plugin;

namespace Luna;

/// <summary> A window that specifically is used to display failures to load and check for updates. </summary>
/// <remarks> Use Dalamud's Imgui here since ImRaii may not be correctly setup on failures. </remarks>
public class ErrorWindow : Window, IDisposable
{
    private readonly string              _name;
    private readonly WindowSystem        _system;
    private readonly Task<PluginUpdate?> _updateCheck;

    /// <summary> Create a new error window that will also create its own window system and subscribe to drawing. </summary>
    /// <param name="pi"> The plugin interface. </param>
    /// <param name="label"> The label for the window itself. </param>
    /// <param name="name"> The name of the plugin and window system. </param>
    public ErrorWindow(IDalamudPluginInterface pi, string label, string name)
        : base(label)
    {
        _name   = name;
        _system = WindowSystem.Create(pi.UiBuilder, name);
        _system.AddWindow(this);
        _updateCheck = pi.CheckForUpdateAsync();
        IsOpen       = true;
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
        ImGui.TextWrapped($"Error initializing {_name}. Further information can be found in /xllog.");
        ImGui.PopStyleColor();

        ImGui.NewLine();
        ImGui.Separator();
        ImGui.NewLine();
        if (!_updateCheck.IsCompleted)
            ImGui.Text("Checking for Updates..."u8);
        else if (_updateCheck.IsFaulted)
            ImGui.TextColored(ImGuiColors.DalamudRed, "Failed to check for updates."u8);
        else if (_updateCheck.Result is not { } update)
            ImGui.TextColored(ImGuiColors.DalamudOrange, "No new versions found."u8);
        else
            ImGui.TextColored(ImGuiColors.DalamudOrange,
                $"A new version v{update.Version} is available{(update.IsTesting ? " on testing." : ".")}");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _system.Dispose();
        _updateCheck.Dispose();
    }
}
