using Dalamud.Bindings.ImGui;

namespace Luna;

/// <summary> Extensions concerning handling of Dalamud objects. </summary>
public static class DalamudExtensions
{
    /// <summary> Convert between ImSharp WindowFlags and Dalamud ImGuiWindowFlags. </summary>
    /// <param name="flags"> The flags to convert. </param>
    /// <returns> The converted flags. </returns>
    public static ImGuiWindowFlags ToDalamudWindowFlags(this WindowFlags flags)
        => (ImGuiWindowFlags)flags;
}
