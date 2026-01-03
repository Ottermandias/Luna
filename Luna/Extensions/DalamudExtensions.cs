using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiFileDialog;

namespace Luna;

/// <summary> Extensions concerning handling of Dalamud objects. </summary>
public static class DalamudExtensions
{
    /// <summary> Convert between ImSharp WindowFlags and Dalamud ImGuiWindowFlags. </summary>
    /// <param name="flags"> The flags to convert. </param>
    /// <returns> The converted flags. </returns>
    public static ImGuiWindowFlags ToDalamudWindowFlags(this WindowFlags flags)
        => (ImGuiWindowFlags)flags;

    /// <summary> Create a FileDialogManager with specific Window Flags. </summary>
    /// <param name="flags"> The flags to pass. </param>
    /// <returns> A file dialog manager. </returns>
    public static FileDialogManager CreateFileDialog(WindowFlags flags = WindowFlags.None)
        => new() { AddedWindowFlags = flags.ToDalamudWindowFlags() };
}
