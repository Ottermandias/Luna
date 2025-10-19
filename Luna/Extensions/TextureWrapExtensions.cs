using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;

namespace Luna;

/// <summary> Extension methods concerning ImSharp and Dalamud textures. </summary>
public static class TextureExtensions
{
    /// <summary> Get a ImSharp <see cref="ImTextureId"/> from a dalamud texture. </summary>
    /// <param name="wrap"> The dalamud texture wrap. wrap. </param>
    /// <returns> The ID. </returns>
    public static ImTextureId Id(this IDalamudTextureWrap wrap)
        => new((nint)wrap.Handle.Handle);

    /// <summary> Convert a ImSharp <see cref="ImTextureId"/> to a Dalamud <see cref="ImTextureID"/>. </summary>
    /// <param name="id"> The ID to convert. </param>
    /// <returns> The converted ID. </returns>
    public static ImTextureID ToDalamudId(this ImTextureId id)
        => new(id.Value);
}
