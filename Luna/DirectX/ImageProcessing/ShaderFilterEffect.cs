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
    Buffer? uniforms,
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
    ///   A function that calculates the output dimensions of this effect, from the input dimensions.
    ///   It will be called just before this effect runs, and shall return <c>null</c> to keep the currently set <see cref="Width"/> and <see cref="Height"/>.
    /// </summary>
    /// <remarks> The effective output size can can be retrieved from <see cref="Width"/> and <see cref="Height"/> after this effect has run. </remarks>
    public Func<ReadOnlySpan<(int Width, int Height)>, (int Width, int Height)?>? DimensionsStrategy = ScaleLargestInput(1.0f);

    /// <summary> Whether to generate mipmaps for the outputs of this effect. </summary>
    public bool GenerateMips = false;

    /// <summary> An event that gets triggered just before this effect begins rendering. </summary>
    public event Action<ShaderFilterEffect>? BeforeRun;

    /// <summary> An event that gets triggered just after this effect has finished rendering. </summary>
    public event Action<ShaderFilterEffect>? AfterRun;

    private readonly FullScreenQuadWithUniformsAndTextures _quad    = new(pixelShader, uniforms, outputFormats, string.Empty);
    private readonly RenderOutputs                         _outputs = new(DefaultWidth, DefaultHeight, false, []);

    /// <inheritdoc cref="FullScreenQuadWithUniformsAndTextures.Uniforms"/>
    public Buffer? Uniforms
        => _quad.Uniforms;

    /// <inheritdoc cref="FullScreenQuadWithUniformsAndTextures.Textures"/>
    public List<TextureStandIn> Textures
        => _quad.Textures;

    /// <inheritdoc cref="FullScreenQuadWithUniformsAndTextures.Samplers"/>
    public List<Sampler?> Samplers
        => _quad.Samplers;

    /// <inheritdoc cref="RenderOutputs.UavOutputs"/>
    public List<IUnorderedAccessViewWrap> UavOutputs
        => _outputs.UavOutputs;

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
    public ShaderFilterEffect(PixelShader pixelShader, Buffer? uniforms, string? description)
        : this(pixelShader, uniforms, [FullScreenQuad.DefaultOutputFormat], description)
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
        BeforeRun?.Invoke(this);

        if (DimensionsStrategy is not null)
        {
            var inputDimensions = new List<(int Width, int Height)>(_quad.Textures.Count);
            foreach (var input in _quad.Textures)
            {
                if (input.IsEmpty)
                    continue;

                var (width, height) = input.Id.Dimensions;
                inputDimensions.Add(((int)width, (int)height));
            }

            if (DimensionsStrategy(CollectionsMarshal.AsSpan(inputDimensions)) is { } dimensions)
            {
                Width  = dimensions.Width;
                Height = dimensions.Height;
            }
        }

        if (_outputs.Count is 0 || _outputs.Width != Width || _outputs.Height != Height)
            _outputs.SetOutputs(Width, Height, GenerateMips, _quad);

        _outputs.RenderObject(_quad);

        AfterRun?.Invoke(this);

        return Task.CompletedTask;
    }

    /// <inheritdoc cref="RenderOutputs.ExportOutputs"/>
    public void ExportOutputs(int index, Span<ImTextureId> outputs)
        => _outputs.ExportOutputs(index, outputs);

    /// <inheritdoc cref="RenderOutputs.GetOutputAsImage"/>
    public Image GetOutputAsImage(int index)
        => _outputs.GetOutputAsImage(index);

    /// <summary> Returns a function suitable for <see cref="DimensionsStrategy"/> that applies a scaling factor to the effect's largest input. </summary>
    /// <param name="factor"> The scaling factor. </param>
    /// <returns> The function that calculates dimensions. </returns>
    public static Func<ReadOnlySpan<(int Width, int Height)>, (int Width, int Height)?>? ScaleLargestInput(float factor)
        => (inputDimensions) =>
        {
            if (inputDimensions.Length is 0)
                return null;

            var largest     = inputDimensions[0];
            var largestArea = (long)largest.Width * largest.Height;
            for (var i = 1; i < inputDimensions.Length; ++i)
            {
                var area = (long)inputDimensions[i].Width * inputDimensions[i].Height;
                if (area > largestArea)
                {
                    largest     = inputDimensions[i];
                    largestArea = area;
                }
            }

            return ((int)MathF.Ceiling(largest.Width * factor), (int)MathF.Ceiling(largest.Height * factor));
        };
}
