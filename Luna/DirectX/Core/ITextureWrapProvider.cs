using Dalamud.Interface.Textures.TextureWraps;

namespace Luna.DirectX;

/// <summary> A list of textures that also provides its textures as <see cref="IDalamudTextureWrap"/>. </summary>
public interface ITextureWrapProvider : IReadOnlyList<ImTextureId>
{
    /// <summary> Retrieves an element of this list, as a wrap. </summary>
    /// <param name="index"> The index of the element to retrieve. </param>
    /// <returns> The element, as a wrap. </returns>
    public IDalamudTextureWrap? GetTextureWrap(int index);
}
