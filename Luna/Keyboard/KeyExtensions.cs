using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Utility;

namespace Luna;

/// <summary> Extension methods for the key value enumerations. </summary>
public static class KeyExtensions
{
    /// <summary> Convert a <see cref="VirtualKey"/> to an ImSharp <see cref="Key"/>. </summary>
    /// <param name="key"> The key to convert. </param>
    /// <returns> The converted key. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key ToImGuiKey(this VirtualKey key)
        => (Key)ImGuiHelpers.VirtualKeyToImGuiKey(key);

    /// <summary> Convert an ImSharp <see cref="Key"/> to a <see cref="VirtualKey"/>. </summary>
    /// <param name="key"> The key to convert. </param>
    /// <returns> The converted key. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VirtualKey ToVirtualKey(this Key key)
        => ImGuiHelpers.ImGuiKeyToVirtualKey((ImGuiKey)key);
}
