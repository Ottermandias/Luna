using TerraFX.Interop.DirectX;

namespace Luna.DirectX;

partial class LunaEffects
{
    private static ShaderFilterEffect Resample(TextureStandIn input, ResampleMethod method, DXGI_FORMAT format, bool generateMips)
    {
        var shader = LunaShaders.Resample;
        var effect = new ShaderFilterEffect(shader, null, [format], $"Resample ({method.ToName()})");
        effect.GenerateMips = generateMips;
        effect.ExtraBuffers.Add(FilterLinkage(method.ToFilter()));
        effect.Textures.Add(input);
        effect.Samplers.Add(GetClampSampler(method));
        return effect;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ConstantBuffer<LunaShaders.FilterLinkage> FilterLinkage(LunaShaders.Filter filter)
        => new(new LunaShaders.FilterLinkage
        {
            Filter = filter,
        });

    private static ShaderFilterEffect KawaseDownsample(int index, TextureStandIn input, Buffer uniforms, DXGI_FORMAT format)
    {
        var effect = new ShaderFilterEffect(LunaShaders.KawaseDownsample, uniforms, [format], $"Dual Kawase Blur - Downsample Pass {index}");
        effect.DimensionsStrategy = inputDimensions =>
        {
            if (inputDimensions.Length is 0)
                return null;

            return (Math.Max(1, inputDimensions[0].Width >> 1), Math.Max(1, inputDimensions[0].Height >> 1));
        };
        effect.Textures.Add(input);
        effect.Samplers.Add(Sampler.ClampBilinear);
        return effect;
    }

    private static ShaderFilterEffect KawaseUpsample(int index, TextureStandIn input, Buffer uniforms, DXGI_FORMAT format)
    {
        var effect = new ShaderFilterEffect(LunaShaders.KawaseUpsample, uniforms, [format], $"Dual Kawase Blur - Upsample Pass {index}");
        effect.DimensionsStrategy = null;
        effect.Textures.Add(input);
        effect.Samplers.Add(Sampler.ClampBilinear);
        return effect;
    }

    private static ComputeFilterEffect Max(TextureStandIn input, string parentDescription)
    {
        var maxOut = new RwImage(1, 1, DXGI_FORMAT.DXGI_FORMAT_R32_UINT);
        var max    = new ComputeFilterEffect(LunaShaders.Max, null, $"{parentDescription} - Max");
        max.Textures.Add(input);
        max.Outputs.Add(maxOut);
        max.BeforeRun += _ =>
        {
            var dimensions = input.Id.Dimensions;
            // That shader has [numthreads(8, 8, 1)], therefore the XY group count is 1/8th of the dimensions, rounded up.
            max.ThreadGroupCount = (((int)dimensions.Width + 7) >> 3, ((int)dimensions.Height + 7) >> 3, 1);
        };
        max.ClearStrategy = ITargetClearStrategy.Simple;
        return max;
    }

    private static ShaderFilterEffect DivideByMax(ComputeFilterEffect max, DXGI_FORMAT format, bool generateMips, string parentDescription)
    {
        var divideByMax = new ShaderFilterEffect(LunaShaders.DivideByMax, null, [format], $"{parentDescription} - Divide by Max");
        divideByMax.GenerateMips = generateMips;
        divideByMax.Textures.Add(new TextureStandIn(max.Textures, 0));
        divideByMax.Textures.Add(new TextureStandIn(max, 0));
        return divideByMax;
    }

    private static Sampler GetClampSampler(ResampleMethod method)
        => method switch
        {
            ResampleMethod.NearestNeighbor or ResampleMethod.SymbolFilter => Sampler.ClampNearestNeighbor,
            _                                                             => Sampler.ClampBilinear,
        };

    private static Sampler GetWrapSampler(ResampleMethod method)
        => method switch
        {
            ResampleMethod.NearestNeighbor or ResampleMethod.SymbolFilter => Sampler.WrapNearestNeighbor,
            _                                                             => Sampler.WrapBilinear,
        };

    private static Sampler GetBorderTransparentSampler(ResampleMethod method)
        => method switch
        {
            ResampleMethod.NearestNeighbor or ResampleMethod.SymbolFilter => Sampler.BorderTransparentNearestNeighbor,
            _                                                             => Sampler.BorderTransparentBilinear,
        };
}
