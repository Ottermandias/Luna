using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;
using TerraFX.Interop.Windows;

namespace Luna.DirectX;

/// <summary> An image processing effect that saves its input to a file. </summary>
/// <param name="readbackProvider"> Dalamud's texture readback provider. </param>
/// <param name="containerGuid"> The WIC container GUID of the format to save as. </param>
/// <param name="path"> The path to save at. </param>
/// <param name="props"> Properties to pass to the encoder. </param>
/// <seealso cref="ITextureReadbackProvider.SaveToFileAsync"/>
public class SaveToFileEffect(
    ITextureReadbackProvider readbackProvider,
    Guid containerGuid,
    string path,
    IReadOnlyDictionary<string, object>? props = null) : WrapEffectBase
{
    /// <inheritdoc/>
    public override int Count
        => 0;

    /// <inheritdoc/>
    public override ImTextureId this[int index]
        => throw new NotSupportedException();

    /// <summary> Constructs a <see cref="SaveToFileEffect"/>, inferring the container GUID from the path's extension. </summary>
    /// <param name="readbackProvider"> Dalamud's texture readback provider. </param>
    /// <param name="path"> The path to save at. </param>
    /// <param name="props"> Properties to pass to the encoder. </param>
    public SaveToFileEffect(ITextureReadbackProvider readbackProvider, string path, IReadOnlyDictionary<string, object>? props = null)
        : this(readbackProvider, readbackProvider.GetContainerGuid(path) ?? GUID.GUID_ContainerFormatPng, path, props)
    { }

    /// <inheritdoc/>
    protected override Task Run(IDalamudTextureWrap wrap, CancellationToken cancellationToken)
        => readbackProvider.SaveToFileAsync(wrap, containerGuid, path, props, true, cancellationToken);
}
