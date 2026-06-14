using Microsoft.Extensions.Logging;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Luna.DirectX;

using static DxUtility;

/// <summary> A manager to handle caches for custom renderable objects. </summary>
public sealed partial class CustomRenderManager : IDisposable
{
    /// <summary> The custom render manager. </summary>
    public static readonly CustomRenderManager Instance = new(null);

    /// <summary> A custom logger to set when the manager should not use the global logger. </summary>
    public ILogger? CustomLogger
    {
        get;
        set
        {
            field = value;
            UpdateLogger(ImSharpConfiguration.Logger);
        }
    }

    /// <summary> The logger the internal functions write to. </summary>
    public ILogger Logger { get; private set; }

    private readonly ConditionalWeakTable<ICustomRenderable, Dictionary<Dimensions, RenderCache>> _caches = [];

    private ComPtr<ID3D11Device> _device;

    /// <summary> The Direct3D 11 device to render on. </summary>
    public unsafe ID3D11Device* Device
        => _device;

    private CustomRenderManager(ILogger? logger)
    {
        CustomLogger                       =  logger;
        Logger                             =  CustomLogger ?? ImSharpConfiguration.Logger;
        ImSharpPerFrame.Update             += CheckCachedRenders;
        ImSharpConfiguration.LoggerChanged += UpdateLogger;
    }

    ~CustomRenderManager()
        => Dispose(false);

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool _)
    {
        foreach (var (_, caches) in _caches)
            Clear(caches);
        _caches.Clear();
        ImSharpPerFrame.Update             -= CheckCachedRenders;
        ImSharpConfiguration.LoggerChanged -= UpdateLogger;
        _device.Dispose();
    }

    /// <summary> Update the logger if it changes in the global configuration. </summary>
    private void UpdateLogger(ILogger obj)
        => Logger = CustomLogger ?? obj;

    private static void Clear(Dictionary<Dimensions, RenderCache> caches)
    {
        foreach (var (_, cache) in caches)
            cache.Dispose();
        caches.Clear();
    }

    /// <summary> Sets the Direct3D 11 device to render on. </summary>
    /// <param name="device"> The device. </param>
    public unsafe void SetDevice(nint device)
    {
        if (device is 0)
        {
            _device.Dispose();
            return;
        }

        Marshal.ThrowExceptionForHR(NonOwningComPtr((IUnknown*)device).As(ref _device));
    }

    /// <summary> Renders an object, and returns the output in a form suitable for use as an ImGui image. </summary>
    /// <param name="renderable"> The object to render. </param>
    /// <param name="dimensions"> The dimensions at which to render the object. </param>
    /// <param name="outputIndex"> If this object has multiple render outputs, the index, otherwise 0. Pass -1 to get the depth-stencil buffer. </param>
    /// <returns> An ImGui texture ID representing the rendered object. </returns>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public ImTextureId RenderObject(ICustomRenderable renderable, Dimensions dimensions, int outputIndex = 0)
    {
        ImTextureId output = default;
        RenderObject(renderable, dimensions, outputIndex, new Span<ImTextureId>(ref output));
        return output;
    }

    /// <summary> Renders an object, and returns the outputs in a form suitable to use as ImGui images. </summary>
    /// <param name="renderable"> The object to render. </param>
    /// <param name="dimensions"> The dimensions at which to render the object. </param>
    /// <param name="outputIndex"> The first render output index. Pass -1 to get the depth-stencil buffer. </param>
    /// <param name="outputs"> On return, ImGui texture IDs representing the rendered object. </param>
    public unsafe void RenderObject(ICustomRenderable renderable, Dimensions dimensions, int outputIndex, Span<ImTextureId> outputs)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(outputIndex, -1);
        if (outputs.Length > D3D11.D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT)
            throw new ArgumentException("Output count exceeds D3D11's maximum simultaneous render target count");

        var version = renderable.Version;
        var caches  = _caches.GetOrCreateValue(renderable);
        if (!caches.TryGetValue(dimensions, out var cache))
        {
            LogCreatingCache(Logger, renderable, dimensions.Width, dimensions.Height);
            // Cause a version mismatch on purpose to simplify the paths below.
            cache              = new RenderCache(unchecked(version - 1));
            cache.DepthStencil = new DepthStencil(_device, dimensions);
            caches.Add(dimensions, cache);
        }

        if (cache.Version == version)
        {
            cache.ExpiresAtFrame = Im.Context.FrameCount + renderable.KeepAliveDuration;
            cache.ExportOutputs(outputIndex, outputs);
            return;
        }

        LogVersionUpdate(Logger, renderable, cache.Version, version, dimensions.Width, dimensions.Height);
        cache.SetOutputCount(renderable.OutputCount, _device, dimensions, renderable.GetOutputFormat);

        RenderObject(renderable, dimensions, in cache);

        cache.ExpiresAtFrame = Im.Context.FrameCount + renderable.KeepAliveDuration;
        cache.Version        = version;
        cache.ExportOutputs(outputIndex, outputs);
    }

    /// <summary> Renders an object onto caller-supplied outputs. </summary>
    /// <param name="renderable"> The object to render. </param>
    /// <param name="dsView"> The depth-stencil buffer to render onto. </param>
    /// <param name="rtViews"> The render targets to render onto. </param>
    public unsafe void RenderObject(ICustomRenderable renderable, ID3D11DepthStencilView* dsView,
        params ReadOnlySpan<ImSharp.Pointer<ID3D11RenderTargetView>> rtViews)
    {
        if (rtViews.Length > D3D11.D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT)
            throw new ArgumentException("The render target count exceeds Direct3D 11's maximum");

        if (rtViews.Length != renderable.OutputCount)
            throw new ArgumentException("The render target count does not match the renderable's output count");

        var valid      = false;
        var dimensions = Dimensions.Invalid;
        foreach (var rtView in rtViews)
        {
            if (rtView.Value is not null)
            {
                valid      = true;
                dimensions = GetDimensionsFromView(rtView.Value);
                break;
            }
        }

        if (!valid)
        {
            if (dsView is null)
                throw new ArgumentException("All the passed render targets and the depth-stencil view are null");

            dimensions = GetDimensionsFromView(dsView);
        }

        RenderObject(renderable, dimensions, new CustomRenderOutputs(dsView, rtViews));
    }

    /// <summary> Renders an object onto caller-supplied outputs. </summary>
    /// <param name="renderable"> The object to render. </param>
    /// <param name="dimensions"> The dimensions at which to render the object. </param>
    /// <param name="outputs"> The outputs to render the object onto. </param>
    public unsafe void RenderObject<T>(ICustomRenderable renderable, Dimensions dimensions, in T outputs)
        where T : IRenderTargetProvider, allows ref struct
    {
        using var deviceContext = new ComPtr<ID3D11DeviceContext>();
        _device.Get()->GetImmediateContext(deviceContext.GetAddressOf());

        // Save some state to restore it later, so we play nice with ImGui, the game itself, and other DirectX consumers in the process.
        // Despite changing the RS (Rasterizer) Viewports, saving and restoring them seems unnecessary.
        using var savedRsState = new SavedRasterizerState(deviceContext);
        using var savedRtViews = new SavedRenderTargetViews(deviceContext);
        using var savedDsState = new SavedDepthStencilState(deviceContext);

        // First clear the depth-stencil and render targets, if and how the renderable wants it.
        if (renderable.ClearStrategy is { } clearStrategy)
            outputs.Clear(clearStrategy, deviceContext);

        // Install our own output configuration (RS Viewports + our version of the stuff we saved earlier).
        SetSimpleViewport(deviceContext, dimensions.Width, dimensions.Height);
        SetRasterizerState(deviceContext, renderable.RasterizerState);
        SetDepthStencilState(deviceContext, renderable.DepthStencilState, 0);
        outputs.Bind(deviceContext);

        // Our output configuration and render targets are installed, now the renderable may run its own draw calls.
        renderable.Render(dimensions, deviceContext);

        outputs.PostProcess(deviceContext);
    }

    /// <summary> Check all cached renders for disposal. </summary>
    /// <remarks> Any render that has not been retrieved for at least its <seealso cref="ICustomRenderable.KeepAliveDuration"/> frames will be disposed and removed. </remarks>
    private void CheckCachedRenders()
    {
        var frame              = Im.Context.FrameCount;
        var discardRenderables = new HashSet<ICustomRenderable>(32);
        var discardSizes       = new HashSet<Dimensions>(16);
        foreach (var (renderable, caches) in _caches)
        {
            discardSizes.Clear();
            foreach (var (size, cache) in caches)
            {
                // We are called at the beginning of a frame, therefore stuff that "expires at this frame" is given one more frame of grace.
                if (cache.ExpiresAtFrame < frame)
                {
                    LogDiscardedCache(Logger, renderable, size.Width, size.Height);
                    cache.Dispose();
                    discardSizes.Add(size);
                }
            }

            foreach (var size in discardSizes)
                caches.Remove(size);
            if (caches.Count is 0)
                discardRenderables.Add(renderable);
        }

        foreach (var renderable in discardRenderables)
            _caches.Remove(renderable);
    }

    private readonly unsafe ref struct CustomRenderOutputs(
        ID3D11DepthStencilView* dsView,
        ReadOnlySpan<ImSharp.Pointer<ID3D11RenderTargetView>> rtViews) : IRenderTargetProvider
    {
        private readonly ReadOnlySpan<ImSharp.Pointer<ID3D11RenderTargetView>> _rtViews = rtViews;

        void IRenderTargetProvider.Clear(ITargetClearStrategy strategy, ID3D11DeviceContext* deviceContext)
        {
            strategy.ClearDepthStencil(deviceContext, dsView);
            for (var i = 0; i < _rtViews.Length; ++i)
                strategy.ClearRenderTarget(deviceContext, i, _rtViews[i]);
        }

        void IRenderTargetProvider.Bind(ID3D11DeviceContext* deviceContext)
        {
            fixed (ImSharp.Pointer<ID3D11RenderTargetView>* pRtViews = &_rtViews[0])
            {
                deviceContext->OMSetRenderTargets((uint)_rtViews.Length, (ID3D11RenderTargetView**)pRtViews, dsView);
            }
        }

        void IRenderTargetProvider.PostProcess(ID3D11DeviceContext* deviceContext)
        { }
    }

    private sealed class RenderCache(long version) : RenderOutputsBase
    {
        public long Version = version;
        public int  ExpiresAtFrame;

        ~RenderCache()
            => Dispose();

        public unsafe void SetOutputCount(int count, ID3D11Device* device, Dimensions dimensions, Func<int, DXGI_FORMAT> format)
        {
            var previousCount = Outputs.Length;
            if (count == previousCount)
                return;

            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, D3D11.D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT);

            for (var i = count; i < previousCount; ++i)
                Outputs[i].Dispose();

            Array.Resize(ref Outputs, count);
            for (var i = previousCount; i < count; ++i)
                Outputs[i] = new RenderTarget(device, dimensions, format(i));
        }
    }

    // ReSharper disable InconsistentNaming
    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Debug,
        "[CustomRenderManager] Creating new cache for {Renderable:l} at size {Width}x{Height}.")]
    static partial void LogCreatingCache(ILogger logger, ICustomRenderable Renderable, uint Width, uint Height);

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Debug,
        "[CustomRenderManager] Rendering {Renderable:l} (version {OldVersion} -> {NewVersion}) at size {Width}x{Height}.")]
    static partial void LogVersionUpdate(ILogger logger, ICustomRenderable Renderable, long OldVersion, long NewVersion, uint Width,
        uint Height);

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Debug,
        "[CustomRenderManager] Discarding cache for {Renderable:l} at size {Width}x{Height}.")]
    static partial void LogDiscardedCache(ILogger logger, ICustomRenderable Renderable, uint Width, uint Height);
    // ReSharper restore InconsistentNaming
}
