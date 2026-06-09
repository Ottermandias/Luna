namespace Luna.DirectX;

/// <summary>
///   An object wrapping a Direct3D 11 resource, with a shader resource view (read-only) and an unordered access view (read-write),
///   i.e. a texture, buffer or other resource that can be arbitrarily read from or written to by a pixel or compute shader.
/// </summary>
public interface IUnorderedAccessViewWrap : IDisposable
{
    /// <summary> The Direct3D 11 shader resource view (read-only) handle. </summary>
    /// <remarks> This property is named after its conceptual proximity to <see cref="TextureExtensions.get_Id"/>. </remarks>
    public ImTextureId Id { get; }

    /// <summary> The Direct3D 11 unordered access view (read-write) handle. </summary>
    public nint Handle { get; }

    /// <summary> The starting offset, for appendable and consumable UAVs. Ignored for other types of UAVs. </summary>
    /// <remarks> Return <see cref="uint.MaxValue"/> to leave undefined. </remarks>
    public uint InitialOffset { get; }
}
