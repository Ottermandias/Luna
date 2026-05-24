using System.Collections.Immutable;
using Dalamud.Interface.Textures.TextureWraps;
using TerraFX.Interop.DirectX;

namespace Luna.DirectX;

/// <summary> An image filter effect implemented using a pixel shader. </summary>
/// <param name="pixelShader"> The pixel shader that implements this filter effect. </param>
/// <param name="uniforms"> The uniforms constant buffer. </param>
/// <param name="outputFormats"> The effect's output formats. </param>
/// <param name="description"> A description of this object, for debugging and logging purposes. </param>
/// <remarks> This uses <see cref="FullScreenQuadWithUniformsAndTextures"/>. The pixel shader has to accept the same inputs. </remarks>
public class ShaderFilterEffect(
    PixelShader pixelShader,
    ConstantBufferBase? uniforms,
    ImmutableArray<DXGI_FORMAT> outputFormats,
    string? description)
    : IEffect, ITextureWrapProvider, IDisposable
{
    private const int DefaultWidth  = 32;
    private const int DefaultHeight = 32;

    /// <summary> The output width of this effect. </summary>
    public int Width = DefaultWidth;

    /// <summary> The output height of this effect. </summary>
    public int Height = DefaultHeight;

    /// <summary>
    ///   The size this effect's outputs shall automatically take, as a factor multiplied by its first input texture size and rounded up, if applicable.
    ///   Set to zero, infinity or <see cref="float.NaN"/> to disable.
    /// </summary>
    /// <remarks> The effective output size can can be retrieved from <see cref="Width"/> and <see cref="Height"/> after this effect has run. </remarks>
    public float AutoSizeFactor = 1.0f;

    /// <summary> Whether to generate mipmaps for the outputs of this effect. </summary>
    public bool GenerateMips = false;

    private readonly FullScreenQuadWithUniformsAndTextures _quad    = new(pixelShader, uniforms, outputFormats, string.Empty);
    private readonly RenderOutputs                         _outputs = new(DefaultWidth, DefaultHeight, false, []);

    /// <inheritdoc cref="FullScreenQuadWithUniformsAndTextures.Uniforms"/>
    public ConstantBufferBase? Uniforms
        => _quad.Uniforms;

    /// <inheritdoc cref="FullScreenQuadWithUniformsAndTextures.Textures"/>
    public List<TextureStandIn> Textures
        => _quad.Textures;

    /// <inheritdoc cref="FullScreenQuadWithUniformsAndTextures.Samplers"/>
    public List<Sampler?> Samplers
        => _quad.Samplers;

    /// <inheritdoc/>
    public int Count
        => _outputs.Count;

    /// <inheritdoc/>
    public ImTextureId this[int index]
        => _outputs[index];

    /// <summary> Creates a new <see cref="ShaderFilterEffect"/>. </summary>
    /// <param name="pixelShader"> The pixel shader that implements this filter effect. </param>
    /// <param name="uniforms"> The uniforms constant buffer. </param>
    /// <param name="description"> A description of this object, for debugging and logging purposes. </param>
    public ShaderFilterEffect(PixelShader pixelShader, ConstantBufferBase? uniforms, string? description)
        : this(pixelShader, uniforms, [DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM], description)
    { }

    ~ShaderFilterEffect()
        => Dispose(false);

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary> Releases the resources used by this object. </summary>
    /// <param name="disposing"> True if called explicitly, false if garbage collected. </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        _quad.Dispose();
        _outputs.Dispose();
    }

    /// <inheritdoc/>
    public override string? ToString()
        => description ?? base.ToString();

    /// <inheritdoc/>
    public IEnumerable<IEffect> GetDependencies()
    {
        foreach (var input in _quad.Textures)
        {
            if (input.TryGetListAndIndex(out var list, out _) && list is IEffect effect)
                yield return effect;
        }
    }

    IDalamudTextureWrap ITextureWrapProvider.GetTextureWrap(int index)
        => _outputs.GetOutputAsImage(index);

    /// <inheritdoc/>
    public Task Run(CancellationToken cancellationToken)
    {
        if (float.IsFinite(AutoSizeFactor) && AutoSizeFactor is not 0.0f)
        {
            foreach (var input in _quad.Textures)
            {
                if (input.IsEmpty)
                    continue;

                var dimensions = input.Id.Dimensions;
                Width  = (int)MathF.Ceiling(dimensions.Width * AutoSizeFactor);
                Height = (int)MathF.Ceiling(dimensions.Height * AutoSizeFactor);
                break;
            }
        }

        if (_outputs.Count is 0 || _outputs.Width != Width || _outputs.Height != Height)
            _outputs.SetOutputs(Width, Height, GenerateMips, _quad);

        _outputs.RenderObject(_quad);

        return Task.CompletedTask;
    }

    /// <inheritdoc cref="RenderOutputs.ExportOutputs"/>
    public void ExportOutputs(int index, Span<ImTextureId> outputs)
        => _outputs.ExportOutputs(index, outputs);

    /// <inheritdoc cref="RenderOutputs.GetOutputAsImage"/>
    public Image GetOutputAsImage(int index)
        => _outputs.GetOutputAsImage(index);
}
