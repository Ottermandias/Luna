using TerraFX.Interop.DirectX;

namespace Luna.DirectX;

/// <summary>
///   This class supports Luna's infrastructure and should not be used directly.
///   Please use its subclass <see cref="RenderOutputs"/> instead.
/// </summary>
public abstract class RenderOutputsBase : IDisposable, IReadOnlyList<ImTextureId>, IRenderTargetProvider
{
    /// <summary> This output collection's depth-stencil buffer. </summary>
    internal DepthStencil DepthStencil;

    /// <summary> This output collection's render targets. </summary>
    internal RenderTarget[] Outputs = [];

    /// <inheritdoc/>
    public int Count
        => Outputs.Length;

    /// <inheritdoc/>
    public ImTextureId this[int index]
    {
        get
        {
            ImTextureId ret = default;
            ExportOutputs(index, new Span<ImTextureId>(ref ret));
            return ret;
        }
    }

    ~RenderOutputsBase()
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
        => DestroyAll();

    /// <summary> Destroys all the currently existing render targets and the depth-stencil buffer of this output collection. </summary>
    protected void DestroyAll()
    {
        if (Outputs.Length is not 0)
        {
            foreach (var output in Outputs)
                output.Dispose();
            Outputs = [];
        }

        DepthStencil.Dispose();
    }

    /// <inheritdoc/>
    public IEnumerator<ImTextureId> GetEnumerator()
    {
        for (var i = 0; i < Outputs.Length; ++i)
            yield return Outputs[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <summary> Gets this collection's outputs as <see cref="ImTextureId"/>, for use in ImGui or as input to another render. </summary>
    /// <param name="index"> The index of the first output to get. Use <c>-1</c> to get the depth-stencil buffer. </param>
    /// <param name="outputs"> The span to fill with the outputs. </param>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="index"/> is less than <c>-1</c> or greater than or equal to <see cref="Count"/>. </exception>
    /// <exception cref="ArgumentException"> Some of the requested outputs are past <see cref="Count"/>. </exception>
    public void ExportOutputs(int index, Span<ImTextureId> outputs)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, -1);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Outputs.Length);
        if (index + outputs.Length > Outputs.Length)
            throw new ArgumentException("Some of the requested outputs are past the render output count.");

        if (index is -1)
            outputs[0] = DepthStencil;
        for (var i = Math.Max(0, -index); i < outputs.Length; ++i)
            outputs[i] = Outputs[index + i];
    }

    /// <summary> Gets one of this collection's outputs as an <see cref="Image"/>. </summary>
    /// <param name="index"> The index of the output to get. Use <c>-1</c> to get the depth-stencil buffer. </param>
    /// <returns> The requested output. </returns>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="index"/> is less than <c>-1</c> or greater than or equal to <see cref="Count"/>. </exception>
    /// <remarks>
    ///   If you want to use this output somewhere that expects a <see cref="TextureStandIn"/>, prefer <see cref="TextureStandIn(IReadOnlyList{ImTextureId},int)"/>.
    ///   This object is a <see cref="IReadOnlyList{ImTextureId}"/>.
    /// </remarks>
    public Image GetOutputAsImage(int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, -1);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Outputs.Length);

        if (index is -1)
            return new Image(in DepthStencil.Texture);

        return new Image(in Outputs[index].Texture);
    }

    /// <inheritdoc/>
    public virtual unsafe void Clear(ITargetClearStrategy strategy, ID3D11DeviceContext* deviceContext)
    {
        strategy.ClearDepthStencil(deviceContext, DepthStencil.DepthStencilView);
        for (var i = 0; i < Outputs.Length; ++i)
            strategy.ClearRenderTarget(deviceContext, i, Outputs[i].RenderTargetView);
    }

    /// <inheritdoc/>
    public virtual unsafe void Bind(ID3D11DeviceContext* deviceContext)
    {
        var outputRtViews = stackalloc ID3D11RenderTargetView*[Outputs.Length];
        for (var i = 0; i < Outputs.Length; ++i)
            outputRtViews[i] = Outputs[i].RenderTargetView;
        deviceContext->OMSetRenderTargets((uint)Outputs.Length, outputRtViews, DepthStencil.DepthStencilView);
    }

    /// <inheritdoc/>
    public virtual unsafe void PostProcess(ID3D11DeviceContext* deviceContext)
    {
    }
}
