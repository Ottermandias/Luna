using Luna.Generators;
using TerraFX.Interop.DirectX;

namespace Luna.DirectX;

/// <summary> A collection of built-in image processing effects, in the form of factory functions. </summary>
public static partial class LunaEffects
{
    /// <summary> Creates an effect that resamples its input to a fixed size. </summary>
    /// <param name="input"> The image to resample. </param>
    /// <param name="dimensions"> The desired dimensions. </param>
    /// <param name="method"> The resampling filter/algorithm. </param>
    /// <param name="generateMips"> Whether to generate mipmaps for the resampled image. </param>
    /// <param name="format"> The pixel format of the resampled image. </param>
    /// <returns> The newly-created resample effect. </returns>
    public static IEffect Resample(TextureStandIn input, Dimensions dimensions, ResampleMethod method, bool generateMips = false,
        DXGI_FORMAT format = FullScreenQuad.DefaultOutputFormat)
    {
        var effect = Resample(input, method, format, generateMips);
        effect.DimensionsStrategy = null;
        effect.Dimensions         = dimensions;
        return effect;
    }

    /// <summary> Creates an effect that resamples its input to a size proportional to the original. </summary>
    /// <param name="input"> The image to resample. </param>
    /// <param name="scale"> The scaling factor. </param>
    /// <param name="method"> The resampling filter/algorithm. </param>
    /// <param name="generateMips"> Whether to generate mipmaps for the resampled image. </param>
    /// <param name="format"> The pixel format of the resampled image. </param>
    /// <returns> The newly-created resample effect. </returns>
    public static IEffect Resample(TextureStandIn input, float scale, ResampleMethod method, bool generateMips = false,
        DXGI_FORMAT format = FullScreenQuad.DefaultOutputFormat)
    {
        var effect = Resample(input, method, format, generateMips);
        effect.DimensionsStrategy = ShaderFilterEffect.ScaleLargestInput(scale);
        return effect;
    }

    /// <summary> Creates an effect that resamples its input according to a custom function. </summary>
    /// <param name="input"> The image to resample. </param>
    /// <param name="dimensionsStrategy"> A function that calculates the output dimensions from the input dimensions. </param>
    /// <param name="method"> The resampling filter/algorithm. </param>
    /// <param name="generateMips"> Whether to generate mipmaps for the resampled image. </param>
    /// <param name="format"> The pixel format of the resampled image. </param>
    /// <returns> The newly-created resample effect. </returns>
    public static IEffect Resample(TextureStandIn input, Func<Dimensions, Dimensions> dimensionsStrategy, ResampleMethod method,
        bool generateMips = false, DXGI_FORMAT format = FullScreenQuad.DefaultOutputFormat)
    {
        var effect = Resample(input, method, format, generateMips);
        effect.DimensionsStrategy = inputDimensions =>
        {
            if (inputDimensions.IsEmpty)
                return null;

            return dimensionsStrategy(inputDimensions[0]);
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
        effect.Samplers.Add(Sampler.ClampBilinear);
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
        effect.Samplers.Add(Sampler.ClampBilinear);
        return effect;
    }

    /// <summary>
    ///   Creates an effect that blends a foreground texture onto a background texture.
    ///   Alpha is not treated as opacity, and just gets blended along red, green and blue without special treatment.
    /// </summary>
    /// <param name="backgroundInput"> The background texture to blend onto. </param>
    /// <param name="foregroundInput"> The foreground texture to blend. </param>
    /// <param name="uniforms"> A constant buffer that can be used to modify the blend parameters between runs. </param>
    /// <param name="initialUniforms"> The initial blend parameters. </param>
    /// <param name="foregroundResampling"> The resampling function to use for the foreground texture. </param>
    /// <param name="generateMips"> Whether to generate mipmaps for the blended texture. </param>
    /// <param name="format"> The pixel format of the blended texture. </param>
    /// <returns> The newly-created blending effect. </returns>
    public static IEffect Blend4(TextureStandIn backgroundInput, TextureStandIn foregroundInput,
        out ConstantBuffer<LunaShaders.Blend4Uniforms> uniforms, in LunaShaders.Blend4Uniforms initialUniforms = default,
        ResampleMethod foregroundResampling = ResampleMethod.Bilinear, bool generateMips = false,
        DXGI_FORMAT format = FullScreenQuad.DefaultOutputFormat)
    {
        uniforms = new ConstantBuffer<LunaShaders.Blend4Uniforms>(in initialUniforms);
        var shader = LunaShaders.Blend4;
        var effect = new ShaderFilterEffect(shader, uniforms, [format], "RGBA Blend");
        effect.DimensionsStrategy = ShaderFilterEffect.ScaleInput(0, 1.0f);
        effect.GenerateMips       = generateMips;
        effect.ExtraBuffers.Add(FilterLinkage(foregroundResampling.ToFilter()));
        effect.Textures.Add(backgroundInput);
        effect.Textures.Add(foregroundInput);
        effect.Samplers.Add(Sampler.ClampBilinear);
        effect.Samplers.Add(GetWrapSampler(foregroundResampling));
        return effect;
    }

    /// <summary> Creates an effect that composites a foreground image onto a background image. Alpha is treated as opacity. </summary>
    /// <param name="backgroundInput"> The background image to composite onto. </param>
    /// <param name="foregroundInput"> The foreground image to composite. </param>
    /// <param name="uniforms"> A constant buffer that can be used to modify the compositing parameters between runs. </param>
    /// <param name="initialUniforms"> The initial compositing parameters. </param>
    /// <param name="foregroundResampling"> The resampling function to use for the foreground image. </param>
    /// <param name="generateMips"> Whether to generate mipmaps for the composited image. </param>
    /// <param name="format"> The pixel format of the composited image. </param>
    /// <returns> The newly-created compositing effect. </returns>
    public static IEffect Composite(TextureStandIn backgroundInput, TextureStandIn foregroundInput,
        out ConstantBuffer<LunaShaders.CompositeUniforms> uniforms, in LunaShaders.CompositeUniforms initialUniforms = default,
        ResampleMethod foregroundResampling = ResampleMethod.Bilinear, bool generateMips = false,
        DXGI_FORMAT format = FullScreenQuad.DefaultOutputFormat)
    {
        uniforms = new ConstantBuffer<LunaShaders.CompositeUniforms>(in initialUniforms);
        var shader = LunaShaders.Composite;
        var effect = new ShaderFilterEffect(shader, uniforms, [format], "RGB Composite");
        effect.DimensionsStrategy = ShaderFilterEffect.ScaleInput(0, 1.0f);
        effect.GenerateMips       = generateMips;
        effect.ExtraBuffers.Add(FilterLinkage(foregroundResampling.ToFilter()));
        effect.Textures.Add(backgroundInput);
        effect.Textures.Add(foregroundInput);
        effect.Samplers.Add(Sampler.ClampBilinear);
        effect.Samplers.Add(GetBorderTransparentSampler(foregroundResampling));
        return effect;
    }

    /// <summary>
    ///   Creates an effect that composites a foreground texture onto a background texture.
    ///   Alpha is not treated as opacity. Instead, auxiliary control masks have to be provided.
    ///   This effect has two outputs: the composited texture, and the composited control mask.
    /// </summary>
    /// <param name="backgroundInput"> The background texture to composite onto. </param>
    /// <param name="backgroundControlInput"> The background control mask. </param>
    /// <param name="foregroundInput"> The foreground texture to composite. </param>
    /// <param name="foregroundControlInput"> The foreground control mask. </param>
    /// <param name="uniforms"> A constant buffer that can be used to modify the compositing parameters between runs. </param>
    /// <param name="initialUniforms"> The initial compositing parameters. </param>
    /// <param name="foregroundResampling"> The resampling function to use for the foreground texture. </param>
    /// <param name="generateMips"> Whether to generate mipmaps for the composited texture. </param>
    /// <param name="format"> The pixel format of the composited texture. </param>
    /// <param name="controlFormat"> The pixel format of the composited control mask. </param>
    /// <returns> The newly-created compositing effect. </returns>
    public static IEffect CompositeControlled(TextureStandIn backgroundInput, TextureStandIn backgroundControlInput,
        TextureStandIn foregroundInput, TextureStandIn foregroundControlInput,
        out ConstantBuffer<LunaShaders.CompositeControlledUniforms> uniforms,
        in LunaShaders.CompositeControlledUniforms initialUniforms = default, ResampleMethod foregroundResampling = ResampleMethod.Bilinear,
        bool generateMips = false, DXGI_FORMAT format = FullScreenQuad.DefaultOutputFormat,
        DXGI_FORMAT controlFormat = DXGI_FORMAT.DXGI_FORMAT_R8_UNORM)
    {
        uniforms = new ConstantBuffer<LunaShaders.CompositeControlledUniforms>(in initialUniforms);
        var shader = LunaShaders.CompositeControlled;
        var effect = new ShaderFilterEffect(shader, uniforms, [format, controlFormat], "RGBA Composite with Control Mask");
        effect.DimensionsStrategy = ShaderFilterEffect.ScaleInput(0, 1.0f);
        effect.GenerateMips       = generateMips;
        effect.ExtraBuffers.Add(FilterLinkage(foregroundResampling.ToFilter()));
        effect.Textures.Add(backgroundInput);
        effect.Textures.Add(backgroundControlInput);
        effect.Textures.Add(foregroundInput);
        effect.Textures.Add(foregroundControlInput);
        effect.Samplers.Add(Sampler.ClampBilinear);
        effect.Samplers.Add(GetBorderTransparentSampler(foregroundResampling));
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
        compositePass.Samplers.Add(Sampler.ClampBilinear);
        var graph = new EffectGraph([downPass1, downPass2, downPass3, upPass1, upPass2, compositePass]);
        graph.BeforeRun += _ =>
        {
            // The upsample pass output dimensions cannot be calculated reliably from their own input dimensions.
            // It would only work if the original input dimensions are multiples of 8.
            var inputSize = input.Id.Dimensions;
            upPass1.Dimensions        = inputSize.Quarter();
            upPass2.Dimensions        = inputSize.Half();
            compositePass.Dimensions  = inputSize;
        };

        var effect = new SubGraphEffect(graph);
        effect.InputBindings.Add((downPass1, 0));
        effect.OutputBindings.Add(new TextureStandIn(compositePass, 0));
        return effect;
    }

    /// <summary>
    ///   Creates an effect that casts rays refracted according to a normal map and counts the hit frequency.
    ///   This effect has two outputs: the hit frequency map, and a 1x1 UInt texture that contains the highest hit count before normalization.
    /// </summary>
    /// <param name="input"> The normal map. </param>
    /// <param name="uniforms"> A constant buffer that can be used to modify the casting parameters between runs. </param>
    /// <param name="initialUniforms"> The initial casting parameters. </param>
    /// <param name="generateMips"> Whether to generate mipmaps for the hit frequency map. </param>
    /// <param name="format"> The pixel format of the hit frequency map. </param>
    /// <returns> The newly-created ray casting effect. </returns>
    /// <remarks> This is implemented very naively and should only serve as a test/demo of Luna.DirectX. </remarks>
    public static IEffect RefractionRaycast(TextureStandIn input, out ConstantBuffer<LunaShaders.RefractionRaycastUniforms> uniforms,
        in LunaShaders.RefractionRaycastUniforms initialUniforms = default, bool generateMips = false,
        DXGI_FORMAT format = DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT)
    {
        uniforms = new ConstantBuffer<LunaShaders.RefractionRaycastUniforms>(in initialUniforms);

        var castOut        = RwImage.UnsafeCreateUninitialized();
        var lastDimensions = Dimensions.Invalid;

        var cast = new ComputeFilterEffect(LunaShaders.RefractionRaycast, uniforms, "Refraction Raycast");
        cast.Textures.Add(input);
        cast.Outputs.Add(castOut);
        cast.BeforeRun += _ =>
        {
            var dimensions = input.Id.Dimensions;
            if (dimensions == lastDimensions)
                return;

            castOut.Recreate(dimensions, DXGI_FORMAT.DXGI_FORMAT_R32_UINT);
            // That shader has [numthreads(8, 8, 1)], therefore the XY group count is 1/8th of the dimensions, rounded up.
            cast.ThreadGroupCount = (((int)dimensions.Width + 7) >> 3, ((int)dimensions.Height + 7) >> 3, 1);
            lastDimensions        = dimensions;
        };
        cast.ClearStrategy = ITargetClearStrategy.Simple;
        var max         = Max(new TextureStandIn(cast, 0), "Refraction Raycast");
        var divideByMax = DivideByMax(max, format, generateMips, "Refraction Raycast");

        var effect = new SubGraphEffect([cast, max, divideByMax]);
        effect.InputBindings.Add((cast, 0));
        effect.OutputBindings.Add(new TextureStandIn(divideByMax, 0));
        effect.OutputBindings.Add(new TextureStandIn(max,         0));
        return effect;
    }

    /// <summary> A resampling filter/algorithm. </summary>
    [NamedEnum]
    [AssociatedEnum<LunaShaders.Filter>(ForwardDefaultValue: LunaShaders.Filter.Simple)]
    public enum ResampleMethod
    {
        /// <summary> Bilinear filtering. </summary>
        [Name("Bilinear")]
        [Associate<LunaShaders.Filter>(LunaShaders.Filter.Simple)]
        Bilinear,

        /// <summary> Nearest-neighbor filtering. </summary>
        [Name("Nearest-neighbor")]
        [Associate<LunaShaders.Filter>(LunaShaders.Filter.Simple)]
        NearestNeighbor,

        /// <summary> Lanczos filtering. </summary>
        [Name("Lanczos")]
        [Associate<LunaShaders.Filter>(LunaShaders.Filter.Lanczos3)]
        Lanczos3,

        /// <summary>
        ///   A bespoke filter that treats the red channel values as symbols, and conditionally applies bilinear, nearest-neighbor,
        ///   marching squares, or a blend thereof, depending on these symbols' equality. Designed around index map semantics.
        /// </summary>
        [Name("Symbol Filter")]
        [Associate<LunaShaders.Filter>(LunaShaders.Filter.SymbolFilter)]
        SymbolFilter,
    }
}
