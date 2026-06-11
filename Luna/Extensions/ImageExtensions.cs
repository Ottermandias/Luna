using Dalamud.Plugin.Services;

namespace Luna;

/// <summary> Extensions for images. </summary>
public static class ImageExtensions
{
    /// <summary> Infers the container GUID to use to save at the given path from its extension. </summary>
    /// <param name="readbackProvider"> Dalamud's texture readback provider. </param>
    /// <param name="path"> The path to save at. </param>
    /// <returns> The container GUID associated with the given path's extension. </returns>
    public static Guid? GetContainerGuid(this ITextureReadbackProvider readbackProvider, string path)
    {
        var extension = Path.GetExtension(path.AsSpan());
        if (extension.IsEmpty)
            return null;

        foreach (var encoder in readbackProvider.GetSupportedImageEncoderInfos())
        {
            foreach (var ext in encoder.Extensions)
            {
                if (extension.Equals(ext, StringComparison.OrdinalIgnoreCase))
                    return encoder.ContainerGuid;
            }
        }

        return null;
    }
}
