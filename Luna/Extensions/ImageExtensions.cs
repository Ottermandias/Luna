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
        path = Path.GetExtension(path);
        if (string.IsNullOrEmpty(path))
            return null;

        foreach (var encoder in readbackProvider.GetSupportedImageEncoderInfos())
        {
            if (encoder.Extensions.Any(ext => string.Equals(path, ext, StringComparison.OrdinalIgnoreCase)))
                return encoder.ContainerGuid;
        }

        return null;
    }
}
