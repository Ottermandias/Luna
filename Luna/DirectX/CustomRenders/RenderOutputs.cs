using System.Collections.Immutable;
using TerraFX.Interop.DirectX;

namespace Luna.DirectX;

/// <summary> A collection of render targets and depth-stencil buffer, to use as persistent outputs for custom renders. </summary>
public unsafe class RenderOutputs : RenderOutputsBase
{
    private int                         _width;
    private int                         _height;
    private bool                        _generateMips;
    private ImmutableArray<DXGI_FORMAT> _formats = [];

    /// <summary> The width of these render outputs. </summary>
    public int Width
        => _width;

    /// <summary> The height of these render outputs. </summary>
    public int Height
        => _height;

    /// <summary> Whether to generate mipmaps after rendering onto these outputs. </summary>
    public bool GenerateMips
        => _generateMips;

    /// <summary> The dimensions of these render outputs. </summary>
    public (int Width, int Height) Dimensions
        => (_width, _height);

    /// <summary> The formats of these render outputs. </summary>
    public ImmutableArray<DXGI_FORMAT> Formats
        => _formats;

    /// <summary> Constructs a collection of render outputs. </summary>
    /// <param name="width"> The width of the render outputs. </param>
    /// <param name="height"> The height of the render outputs. </param>
    /// <param name="generateMips"> Whether to generate mipmaps after rendering onto the outputs. </param>
    /// <param name="formats"> The formats of the render outputs. </param>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="width"/> or <paramref name="height"/> are negative or zero. </exception>
    /// <exception cref="ArgumentException"> <paramref name="formats"/> is <c>default</c> or specifies too many outputs. </exception>
    public RenderOutputs(int width, int height, bool generateMips, ImmutableArray<DXGI_FORMAT> formats)
        => SetOutputs(width, height, generateMips, formats);

    /// <summary> Constructs a collection of render outputs. </summary>
    /// <param name="width"> The width of the render outputs. </param>
    /// <param name="height"> The height of the render outputs. </param>
    /// <param name="generateMips"> Whether to generate mipmaps after rendering onto the outputs. </param>
    /// <param name="renderable"> A custom renderable, to set output formats after. </param>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="width"/> or <paramref name="height"/> are negative or zero. </exception>
    /// <exception cref="ArgumentException"> <paramref name="renderable"/> specifies too many outputs. </exception>
    public RenderOutputs(int width, int height, bool generateMips, ICustomRenderable renderable)
        => SetOutputs(width, height, generateMips, renderable);

    /// <summary> Changes the specifications of these render outputs. </summary>
    /// <param name="width"> The new width. </param>
    /// <param name="height"> The new height. </param>
    /// <param name="generateMips"> Whether to generate mipmaps after rendering. </param>
    /// <param name="formats"> The new formats. </param>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="width"/> or <paramref name="height"/> are negative or zero. </exception>
    /// <exception cref="ArgumentException"> <paramref name="formats"/> is <c>default</c> or specifies too many outputs. </exception>
    public void SetOutputs(int width, int height, bool generateMips, ImmutableArray<DXGI_FORMAT> formats)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(width,  0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(height, 0);
        if (formats.IsDefault || formats.Length > D3D11.D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT)
            throw new ArgumentException(
                $"The output formats array must not be default and must contain 0 to {D3D11.D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT} (inclusive) elements.",
                nameof(formats));

        var recreateAll           = width != _width || height != _height || generateMips != _generateMips;
        var previousOutputFormats = _formats;

        _width        = width;
        _height       = height;
        _generateMips = generateMips;
        _formats      = formats;
        if (recreateAll)
            RecreateAllOutputs();
        else
            RecreateOutputs(previousOutputFormats);
    }

    /// <summary> Changes the specifications of these render outputs. </summary>
    /// <param name="width"> The new width. </param>
    /// <param name="height"> The new height. </param>
    /// <param name="generateMips"> Whether to generate mipmaps after rendering. </param>
    /// <param name="renderable"> A custom renderable, to set output formats after. </param>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="width"/> or <paramref name="height"/> are negative or zero. </exception>
    /// <exception cref="ArgumentException"> <paramref name="renderable"/> specifies too many outputs. </exception>
    public void SetOutputs(int width, int height, bool generateMips, ICustomRenderable renderable)
    {
        var outputFormats = new DXGI_FORMAT[renderable.OutputCount];
        for (var i = 0; i < outputFormats.Length; ++i)
            outputFormats[i] = renderable.GetOutputFormat(i);
        SetOutputs(width, height, generateMips, [..outputFormats]);
    }

    private void RecreateAllOutputs()
    {
        DestroyAll();
        Outputs = new RenderTarget[_formats.Length];

        DepthStencil = new DepthStencil(CustomRenderManager.Instance.Device, (uint)_width, (uint)_height);
        for (var i = 0; i < _formats.Length; ++i)
            Outputs[i] = CreateOutput(i);
    }

    private void RecreateOutputs(ImmutableArray<DXGI_FORMAT> previousOutputFormats)
    {
        for (var i = _formats.Length; i < previousOutputFormats.Length; ++i)
            Outputs[i].Dispose();

        Array.Resize(ref Outputs, _formats.Length);

        for (var i = 0; i < _formats.Length && i < previousOutputFormats.Length; ++i)
        {
            if (_formats[i] == previousOutputFormats[i])
                continue;

            Outputs[i].Dispose();
            Outputs[i] = CreateOutput(i);
        }

        for (var i = previousOutputFormats.Length; i < _formats.Length; ++i)
            Outputs[i] = CreateOutput(i);
    }

    private RenderTarget CreateOutput(int index)
        => new(CustomRenderManager.Instance.Device, (uint)_width, (uint)_height, _formats[index], _generateMips);

    /// <summary> Renders an object onto these outputs. </summary>
    /// <param name="renderable"> The object to render. </param>
    /// <exception cref="InvalidOperationException"> This object has no outputs. </exception>
    public void RenderObject(ICustomRenderable renderable)
    {
        if (Outputs.Length is 0)
            throw new InvalidOperationException("This DxRenderOutputs object has no outputs to render onto.");

        CustomRenderManager.Instance.RenderObject(renderable, (uint)_width, (uint)_height, this);
    }

    /// <inheritdoc/>
    public override void PostProcess(ID3D11DeviceContext* deviceContext)
    {
        if (!_generateMips)
            return;

        for (var i = 0; i < Outputs.Length; ++i)
            deviceContext->GenerateMips(Outputs[i].Texture.ShaderResourceView);
    }
}
