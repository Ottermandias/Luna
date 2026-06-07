using Luna.Generators;
using TerraFX.Interop.DirectX;

namespace Luna.DirectX;

/// <summary> A collection of built-in image processing effects, in the form of factory functions. </summary>
public static class LunaEffects
{
    /// <summary> Creates an effect that resizes its input to a fixed size. </summary>
    /// <param name="input"> The image to resize. </param>
    /// <param name="width"> The desired width. </param>
    /// <param name="height"> The desired height. </param>
    /// <param name="method"> The resampling filter/algorithm. </param>
    /// <param name="generateMips"> Whether to generate mipmaps for the resized image. </param>
    /// <param name="format"> The pixel format of the resized image. </param>
    /// <returns> The newly-created resize effect. </returns>
    public static IEffect Resize(TextureStandIn input, int width, int height, ResizeMethod method, bool generateMips = false,
        DXGI_FORMAT format = FullScreenQuad.DefaultOutputFormat)
    {
        var effect = Resize(input, method, format, generateMips);
        effect.DimensionsStrategy = null;
        effect.Width              = width;
        effect.Height             = height;
        return effect;
    }

    /// <summary> Creates an effect that resizes its input proportionally. </summary>
    /// <param name="input"> The image to resize. </param>
    /// <param name="factor"> The scaling factor. </param>
    /// <param name="method"> The resampling filter/algorithm. </param>
    /// <param name="generateMips"> Whether to generate mipmaps for the resized image. </param>
    /// <param name="format"> The pixel format of the resized image. </param>
    /// <returns> The newly-created resize effect. </returns>
    public static IEffect Resize(TextureStandIn input, float factor, ResizeMethod method, bool generateMips = false,
        DXGI_FORMAT format = FullScreenQuad.DefaultOutputFormat)
    {
        var effect = Resize(input, method, format, generateMips);
        effect.DimensionsStrategy = ShaderFilterEffect.ScaleLargestInput(factor);
        return effect;
    }

    /// <summary> Creates an effect that resizes its input according to a custom function. </summary>
    /// <param name="input"> The image to resize. </param>
    /// <param name="dimensionsStrategy"> A function that calculates the output dimensions from the input dimensions. </param>
    /// <param name="method"> The resampling filter/algorithm. </param>
    /// <param name="generateMips"> Whether to generate mipmaps for the resized image. </param>
    /// <param name="format"> The pixel format of the resized image. </param>
    /// <returns> The newly-created resize effect. </returns>
    public static IEffect Resize(TextureStandIn input, Func<int, int, (int Width, int Height)> dimensionsStrategy, ResizeMethod method,
        bool generateMips = false, DXGI_FORMAT format = FullScreenQuad.DefaultOutputFormat)
    {
        var effect = Resize(input, method, format, generateMips);
        effect.DimensionsStrategy = inputDimensions =>
        {
            if (inputDimensions.IsEmpty)
                return null;

            return dimensionsStrategy(inputDimensions[0].Width, inputDimensions[0].Height);
        };
        return effect;
    }

    /// <summary> Creates an effect that transforms the colors of its input linearly. </summary>
    /// <param name="input"> The image to transform. </param>
    /// <param name="uniforms"> A constant buffer that can be used to modify the color transform between runs. </param>
    /// <param name="initialUniforms"> The initial color transform. </param>
    /// <param name="generateMips"> Whether to generate mipmaps for the transformed image. </param>
    /// <param name="format"> The pixel format of the transformed image. </param>
    /// <returns> The newly-created color transform effect. </returns>
    public static IEffect ColorTransform(TextureStandIn input, out ConstantBuffer<LunaShaders.ColorTransformUniforms> uniforms,
        in LunaShaders.ColorTransformUniforms initialUniforms = default, bool generateMips = false,
        DXGI_FORMAT format = FullScreenQuad.DefaultOutputFormat)
    {
        uniforms = new ConstantBuffer<LunaShaders.ColorTransformUniforms>(in initialUniforms);
        var effect = new ShaderFilterEffect(LunaShaders.ColorTransform, uniforms, [format], "Color Transform");
        effect.GenerateMips = generateMips;
        effect.Textures.Add(input);
        effect.Samplers.Add(Sampler.Default);
        return effect;
    }

    /// <summary> Creates an effect that applies a color palette to its input index map. </summary>
    /// <param name="input"> The index map. </param>
    /// <param name="uniforms"> A constant buffer that can be used to modify the color palette between runs. </param>
    /// <param name="initialUniforms"> The initial color palette. </param>
    /// <param name="generateMips"> Whether to generate mipmaps for the transformed image. </param>
    /// <param name="format"> The pixel format of the transformed image. </param>
    /// <returns> The newly-created color palette application effect. </returns>
    public static IEffect ApplyIndex(TextureStandIn input, out ConstantBuffer<LunaShaders.ApplyIndexUniforms> uniforms,
        in LunaShaders.ApplyIndexUniforms initialUniforms = default, bool generateMips = false,
        DXGI_FORMAT format = FullScreenQuad.DefaultOutputFormat)
    {
        uniforms = new ConstantBuffer<LunaShaders.ApplyIndexUniforms>(in initialUniforms);
        var effect = new ShaderFilterEffect(LunaShaders.ApplyIndex, uniforms, [format], "Color Transform");
        effect.GenerateMips = generateMips;
        effect.Textures.Add(input);
        effect.Samplers.Add(Sampler.Default);
        return effect;
    }

    /// <summary> Creates an effect that applies a Dual Kawase blur to its input. </summary>
    /// <param name="input"> The image to blur. </param>
    /// <param name="uniforms"> A constant buffer that can be used to modify the blur parameters between runs. </param>
    /// <param name="initialUniforms"> The initial blur parameters. </param>
    /// <param name="generateMips"> Whether to generate mipmaps for the blurred image. </param>
    /// <param name="format"> The pixel format of the blurred image. </param>
    /// <returns> The newly-created Dual Kawase blur effect. </returns>
    public static IEffect KawaseBlur(TextureStandIn input, out ConstantBuffer<LunaShaders.KawaseUniforms> uniforms,
        in LunaShaders.KawaseUniforms initialUniforms = default, bool generateMips = false,
        DXGI_FORMAT format = FullScreenQuad.DefaultOutputFormat)
    {
        uniforms = new ConstantBuffer<LunaShaders.KawaseUniforms>(in initialUniforms);
        var downPass1 = KawaseDownsample(1, input,                            uniforms, format);
        var downPass2 = KawaseDownsample(2, new TextureStandIn(downPass1, 0), uniforms, format);
        var downPass3 = KawaseDownsample(3, new TextureStandIn(downPass2, 0), uniforms, format);
        var upPass1   = KawaseUpsample(1, new TextureStandIn(downPass3,   0), uniforms, format);
        var upPass2   = KawaseUpsample(2, new TextureStandIn(upPass1,     0), uniforms, format);
        var compositePass = new ShaderFilterEffect(LunaShaders.KawaseUpsampleComposite, uniforms, [format],
            $"Dual Kawase Blur - Upsample + Composite Pass");
        compositePass.DimensionsStrategy = null;
        compositePass.GenerateMips       = generateMips;
        compositePass.Textures.Add(new TextureStandIn(upPass2, 0));
        compositePass.Textures.Add(input);
        compositePass.Samplers.Add(Sampler.Default);
        var graph = new EffectGraph([downPass1, downPass2, downPass3, upPass1, upPass2, compositePass]);
        graph.BeforeRun += _ =>
        {
            // The upsample pass output dimensions cannot be calculated reliably from their own input dimensions.
            // It would only work if the original input dimensions are multiples of 8.
            var inputSize = input.Id.Dimensions;
            upPass1.Width        = (int)Math.Max(1, inputSize.Width >> 2);
            upPass1.Height       = (int)Math.Max(1, inputSize.Height >> 2);
            upPass2.Width        = (int)Math.Max(1, inputSize.Width >> 1);
            upPass2.Height       = (int)Math.Max(1, inputSize.Height >> 1);
            compositePass.Width  = (int)inputSize.Width;
            compositePass.Height = (int)inputSize.Height;
        };

        var effect = new SubGraphEffect(graph);
        effect.Outputs.Add(new TextureStandIn(compositePass, 0));
        return effect;
    }

    /// <summary> Creates an effect that casts rays refracted according to a normal map and counts the hit frequency. </summary>or other object
    /// <param name="input"> The normal map. </param>
    /// <param name="uniforms"> A constant buffer that can be used to modify the casting parameters between runs. </param>
    /// <param name="initialUniforms"> The initial casting parameters. </param>
    /// <param name="generateMips"> Whether to generate mipmaps for the casting. </param>
    /// <param name="format"> The pixel format of the casting. </param>
    /// <returns> The newly-created ray casting effect. </returns>
    /// <remarks> This is implemented very naively and should only serve as a test/demo of Luna.DirectX. </remarks>
    public static unsafe IEffect RefractionRaycast(TextureStandIn input, out ConstantBuffer<LunaShaders.RefractionRaycastUniforms> uniforms,
        in LunaShaders.RefractionRaycastUniforms initialUniforms = default, bool generateMips = false,
        DXGI_FORMAT format = DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT)
    {
        uniforms = new ConstantBuffer<LunaShaders.RefractionRaycastUniforms>(in initialUniforms);

        var castOut    = RwImage.UnsafeCreateUninitialized();
        var lastWidth  = uint.MaxValue;
        var lastHeight = uint.MaxValue;

        var cast = new ComputeFilterEffect(LunaShaders.RefractionRaycast, uniforms, "Refraction Raycast");
        cast.Textures.Add(input);
        cast.Outputs.Add(castOut);
        cast.BeforeRun += _ =>
        {
            var dimensions = input.Id.Dimensions;
            if (dimensions.Width == lastWidth && dimensions.Height == lastHeight)
                return;

            castOut.Recreate(dimensions.Width, dimensions.Height, DXGI_FORMAT.DXGI_FORMAT_R32_UINT);
            cast.ThreadGroupCount = ((int)dimensions.Width, (int)dimensions.Height, 1);
            lastWidth             = dimensions.Width;
            lastHeight            = dimensions.Height;
        };
        cast.ClearStrategy = ITargetClearStrategy.Simple;
        var max = Max(new TextureStandIn(cast, 0), "Refraction Raycast");
        var divideByMax = DivideByMax(new TextureStandIn(cast, 0), new TextureStandIn(max, 0), format, generateMips,
            "Refraction Raycast");

        var effect = new SubGraphEffect([cast, max, divideByMax]);
        effect.Outputs.Add(new TextureStandIn(divideByMax, 0));
        effect.Outputs.Add(new TextureStandIn(max,         0));
        return effect;
    }

    private static ShaderFilterEffect Resize(TextureStandIn input, ResizeMethod method, DXGI_FORMAT format, bool generateMips)
    {
        var effect = new ShaderFilterEffect(method switch
            {
                ResizeMethod.Lanczos3     => LunaShaders.Lanczos3,
                ResizeMethod.SymbolFilter => LunaShaders.SymbolFilter,
                _                         => LunaShaders.Identity,
            }, null, [format], $"Resize ({method.ToName()})");
        effect.GenerateMips = generateMips;
        effect.Textures.Add(input);
        effect.Samplers.Add(method switch
        {
            ResizeMethod.NearestNeighbor or ResizeMethod.SymbolFilter => Sampler.DefaultNearestNeighbor,
            _                                                         => Sampler.Default,
        });
        return effect;
    }

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
        effect.Samplers.Add(Sampler.Default);
        return effect;
    }

    private static ShaderFilterEffect KawaseUpsample(int index, TextureStandIn input, Buffer uniforms, DXGI_FORMAT format)
    {
        var effect = new ShaderFilterEffect(LunaShaders.KawaseUpsample, uniforms, [format], $"Dual Kawase Blur - Upsample Pass {index}");
        effect.DimensionsStrategy = null;
        effect.Textures.Add(input);
        effect.Samplers.Add(Sampler.Default);
        return effect;
    }

    private static unsafe ComputeFilterEffect Max(TextureStandIn input, string parentDescription)
    {
        var maxOut = new RwImage(1, 1, DXGI_FORMAT.DXGI_FORMAT_R32_UINT);
        var max    = new ComputeFilterEffect(LunaShaders.Max, null, $"{parentDescription} - Max");
        max.Textures.Add(input);
        max.Outputs.Add(maxOut);
        max.BeforeRun += _ =>
        {
            var dimensions = input.Id.Dimensions;
            max.ThreadGroupCount = ((int)dimensions.Width, (int)dimensions.Height, 1);
        };
        max.ClearStrategy = ITargetClearStrategy.Simple;
        return max;
    }

    private static ShaderFilterEffect DivideByMax(TextureStandIn input, TextureStandIn max, DXGI_FORMAT format, bool generateMips, string parentDescription)
    {
        var divideByMax = new ShaderFilterEffect(LunaShaders.DivideByMax, null, [format], $"{parentDescription} - Divide by Max");
        divideByMax.GenerateMips = generateMips;
        divideByMax.Textures.Add(input);
        divideByMax.Textures.Add(max);
        return divideByMax;
    }

    /// <summary> A resampling filter/algorithm. </summary>
    [NamedEnum]
    public enum ResizeMethod
    {
        /// <summary> Bilinear filtering. </summary>
        [Name("Bilinear")]
        Bilinear,

        /// <summary> Nearest-neighbor filtering. </summary>
        [Name("Nearest-neighbor")]
        NearestNeighbor,

        /// <summary> Lanczos filtering. </summary>
        [Name("Lanczos")]
        Lanczos3,

        /// <summary>
        ///   A bespoke filter that treats the red channel values as symbols, and conditionally applies bilinear, nearest-neighbor,
        ///   marching squares, or a blend thereof, depending on these symbols' equality. Designed around index map semantics.
        /// </summary>
        [Name("Symbol Filter")]
        SymbolFilter,
    }
}
