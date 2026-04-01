using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Textures.TextureWraps;

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

    /// <summary> Scale the size of an image down to fit a maximum size in both dimensions. </summary>
    /// <param name="wrap"> The image provided as a Dalamud wrap. </param>
    /// <param name="maxSize"> The maximum size. </param>
    /// <returns> The size of the image scaled down to the maximum size if necessary, with preserved ratio. </returns>
    public static Vector2 ScaledDownSize(this IDalamudTextureWrap wrap, float maxSize)
    {
        var size = wrap.Size;
        if (wrap.Size.X >= maxSize)
        {
            if (wrap.Size.Y < wrap.Size.X)
            {
                size.X = maxSize;
                size.Y = maxSize / wrap.Size.X * wrap.Size.Y;
            }
            else if (wrap.Size.Y == wrap.Size.X)
            {
                size.X = maxSize;
                size.Y = maxSize;
            }
            else
            {
                size.Y = maxSize;
                size.X = maxSize / wrap.Size.Y * wrap.Size.X;
            }
        }
        else if (wrap.Size.Y >= maxSize)
        {
            size.Y = maxSize;
            size.X = maxSize / wrap.Size.Y * wrap.Size.X;
        }

        return size;
    }
}
