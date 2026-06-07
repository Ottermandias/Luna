using Dalamud.Interface.Textures.TextureWraps;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Luna.DirectX;

/// <summary> An image filter effect implemented using a compute shader. </summary>
/// <param name="computeShader"> The compute shader that implements this filter effect. </param>
/// <param name="uniforms"> The uniforms constant buffer. </param>
/// <param name="description"> A description of this object, for debugging and logging purposes. </param>
public class ComputeFilterEffect(ComputeShader computeShader, Buffer? uniforms, string? description)
    : IEffect, ITextureWrapProvider, IDisposable
{
    /// <summary> How many thread groups to run. </summary>
    public (int X, int Y, int Z) ThreadGroupCount =
        (D3D11.D3D11_CS_THREAD_GROUP_MIN_X, D3D11.D3D11_CS_THREAD_GROUP_MIN_Y, D3D11.D3D11_CS_THREAD_GROUP_MIN_Z);

    /// <summary> The textures to pass to the compute shader. </summary>
    public readonly List<TextureStandIn> Textures = new(8);

    /// <summary> The samplers to pass to the compute shader. </summary>
    public readonly List<Sampler?> Samplers = new(4);

    /// <summary> The outputs the compute shader shall write to. </summary>
    public readonly List<IUnorderedAccessViewWrap> Outputs = new(4);

    /// <summary> An event that gets triggered just before this computation begins running. </summary>
    public event Action<ComputeFilterEffect>? BeforeRun;

    /// <summary> An event that gets triggered just after this computation has finished running. </summary>
    public event Action<ComputeFilterEffect>? AfterRun;

    /// <summary> How the unordered access views shall be cleared before the computation runs. </summary>
    public ITargetClearStrategy? ClearStrategy;

    private uint _savedTgcX;
    private uint _savedTgcY;
    private uint _savedTgcZ;

    private ConstantBuffer<SystemUniforms>? _systemBuffer;

    /// <summary> The custom compute shader input data. </summary>
    public Buffer? Uniforms
        => uniforms;

    /// <inheritdoc/>
    public int Count
        => Outputs.Count;

    /// <inheritdoc/>
    public ImTextureId this[int index]
        => Outputs[index].Id;

    ~ComputeFilterEffect()
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

        foreach (var output in Outputs)
            output.Dispose();
    }

    /// <inheritdoc/>
    public override string? ToString()
        => description ?? base.ToString();

    /// <inheritdoc/>
    public IEnumerable<IEffect> GetDependencies()
    {
        foreach (var input in Textures)
        {
            if (input.TryGetListAndIndex(out var list, out _) && list is IEffect effect)
                yield return effect;
        }
    }

    IDalamudTextureWrap? ITextureWrapProvider.GetTextureWrap(int index)
        => Outputs[index] as IDalamudTextureWrap;

    private ConstantBuffer<SystemUniforms> GetOrCreateSystemBuffer()
    {
        if (_systemBuffer is not null)
        {
            if (ThreadGroupCount.X != _savedTgcX || ThreadGroupCount.Y != _savedTgcY || ThreadGroupCount.Z != _savedTgcZ)
            {
                _systemBuffer.Contents = CalculateSystemUniforms((uint)ThreadGroupCount.X, (uint)ThreadGroupCount.Y, (uint)ThreadGroupCount.Z);
                _systemBuffer.SetDirty();
                _savedTgcX = (uint)ThreadGroupCount.X;
                _savedTgcY = (uint)ThreadGroupCount.Y;
                _savedTgcZ = (uint)ThreadGroupCount.Z;
            }

            return _systemBuffer;
        }

        _systemBuffer =
            new ConstantBuffer<SystemUniforms>(CalculateSystemUniforms((uint)ThreadGroupCount.X, (uint)ThreadGroupCount.Y,
                (uint)ThreadGroupCount.Z));

        _savedTgcX = (uint)ThreadGroupCount.X;
        _savedTgcY = (uint)ThreadGroupCount.Y;
        _savedTgcZ = (uint)ThreadGroupCount.Z;
        return _systemBuffer;
    }

    private static SystemUniforms CalculateSystemUniforms(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
        => new SystemUniforms
        {
            ThreadGroupCountX = threadGroupCountX,
            ThreadGroupCountY = threadGroupCountY,
            ThreadGroupCountZ = threadGroupCountZ,
        };

    /// <inheritdoc/>
    public unsafe Task Run(CancellationToken cancellationToken)
    {
        BeforeRun?.Invoke(this);

        if (ThreadGroupCount.X is < D3D11.D3D11_CS_THREAD_GROUP_MIN_X or > D3D11.D3D11_CS_THREAD_GROUP_MAX_X
         || ThreadGroupCount.Y is < D3D11.D3D11_CS_THREAD_GROUP_MIN_Y or > D3D11.D3D11_CS_THREAD_GROUP_MAX_Y
         || ThreadGroupCount.Z is < D3D11.D3D11_CS_THREAD_GROUP_MIN_Z or > D3D11.D3D11_CS_THREAD_GROUP_MAX_Z)
            throw new NotSupportedException(
                $"Cannot dispatch a computation with less than {D3D11.D3D11_CS_THREAD_GROUP_MIN_X}x{D3D11.D3D11_CS_THREAD_GROUP_MIN_Y}x{D3D11.D3D11_CS_THREAD_GROUP_MIN_Z} or more than {D3D11.D3D11_CS_THREAD_GROUP_MAX_X}x{D3D11.D3D11_CS_THREAD_GROUP_MAX_Y}x{D3D11.D3D11_CS_THREAD_GROUP_MAX_Z} thread groups.");

        using (var context = new ComPtr<ID3D11DeviceContext>())
        {
            CustomRenderManager.Instance.Device->GetImmediateContext(context.GetAddressOf());
            if (ClearStrategy is not null)
            {
                for (var i = 0; i < Outputs.Count; ++i)
                    ClearStrategy.ClearUnorderedAccessView(context.Get(), i, (ID3D11UnorderedAccessView*)Outputs[i].Handle);
            }

            Dispatch(context.Get());
        }

        AfterRun?.Invoke(this);

        return Task.CompletedTask;
    }

    private unsafe void Dispatch(ID3D11DeviceContext* deviceContext)
    {
        deviceContext->CSSetShader(computeShader.GetOrCreateShader(), null, 0);

        // This is split in four separate functions so the stackallocs don't add up
        // (on top of each function being kinda logically independent).
        BindConstantBuffers(deviceContext);
        BindTextures(deviceContext);
        BindSamplers(deviceContext);
        BindOutputs(deviceContext);

        deviceContext->Dispatch((uint)ThreadGroupCount.X, (uint)ThreadGroupCount.Y, (uint)ThreadGroupCount.Z);

        UnbindOutputs(deviceContext, Outputs.Count);
    }

    [SkipLocalsInit]
    private unsafe void BindConstantBuffers(ID3D11DeviceContext* deviceContext)
    {
        var buffers = stackalloc ID3D11Buffer*[2];
        buffers[0] = GetOrCreateSystemBuffer().GetOrCreateBuffer(deviceContext);
        buffers[1] = uniforms is not null ? uniforms.GetOrCreateBuffer(deviceContext) : null;
        deviceContext->CSSetConstantBuffers(0, 2, buffers);
    }

    [SkipLocalsInit]
    private unsafe void BindTextures(ID3D11DeviceContext* deviceContext)
    {
        var count = Textures.Count;
        if (count <= 0)
            return;

        if (count > D3D11.D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT)
            throw new InvalidOperationException(
                $"ComputeFilterEffect texture count exceeds DirectX resource limit ({D3D11.D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT})");

        var views = stackalloc ID3D11ShaderResourceView*[count];
        for (var i = 0; i < count; ++i)
            views[i] = (ID3D11ShaderResourceView*)Textures[i].Id.Value;
        deviceContext->CSSetShaderResources(0, (uint)count, views);
    }

    [SkipLocalsInit]
    private unsafe void BindSamplers(ID3D11DeviceContext* deviceContext)
    {
        var count = Samplers.Count;
        if (count <= 0)
            return;

        if (count > D3D11.D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT)
            throw new InvalidOperationException(
                $"ComputeFilterEffect sampler count exceeds DirectX resource limit ({D3D11.D3D11_COMMONSHADER_SAMPLER_SLOT_COUNT})");

        var samplers = stackalloc ID3D11SamplerState*[count];
        for (var i = 0; i < count; ++i)
            samplers[i] = Samplers[i] is { } sampler ? sampler.GetOrCreateSampler() : null;
        deviceContext->CSSetSamplers(0, (uint)count, samplers);
    }

    [SkipLocalsInit]
    private unsafe void BindOutputs(ID3D11DeviceContext* deviceContext)
    {
        var count = Outputs.Count;
        if (count <= 0)
            return;

        if (count > D3D11.D3D11_PS_CS_UAV_REGISTER_COUNT)
            throw new InvalidOperationException(
                $"ComputeFilterEffect output count exceeds DirectX resource limit ({D3D11.D3D11_PS_CS_UAV_REGISTER_COUNT})");

        var uavs    = stackalloc ID3D11UnorderedAccessView*[count];
        var offsets = stackalloc uint[count];
        for (var i = 0; i < count; ++i)
        {
            uavs[i]    = (ID3D11UnorderedAccessView*)Outputs[i].Handle;
            offsets[i] = Outputs[i].InitialOffset;
        }

        deviceContext->CSSetUnorderedAccessViews(0, (uint)count, uavs, offsets);
    }

    [SkipLocalsInit]
    private static unsafe void UnbindOutputs(ID3D11DeviceContext* deviceContext, int count)
    {
        if (count <= 0)
            return;

        var uavs   = stackalloc ID3D11UnorderedAccessView*[count];
        for (var i = 0; i < count; ++i)
            uavs[i] = null;
        deviceContext->CSSetUnorderedAccessViews(0, (uint)count, uavs, null);
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    private struct SystemUniforms
    {
        public uint ThreadGroupCountX;
        public uint ThreadGroupCountY;
        public uint ThreadGroupCountZ;
    }
}
