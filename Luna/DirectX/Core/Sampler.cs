using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Luna.DirectX;

/// <summary> A Direct3D sampler. </summary>
/// <param name="description"> The specifications of the sampler. </param>
public class Sampler(in D3D11_SAMPLER_DESC description) : IDisposable
{
    /// <summary> A default sampler, with UV clamping and bilinear filtering. </summary>
    public static readonly Sampler Default = new(D3D11_SAMPLER_DESC.DEFAULT);

    /// <summary> Similar to <see cref="Default"/>, but with nearest-neighbor filtering. </summary>
    public static readonly Sampler DefaultNearestNeighbor = new(D3D11_SAMPLER_DESC.DEFAULT with
    {
        Filter = D3D11_FILTER.D3D11_FILTER_MIN_MAG_MIP_POINT,
    });

    /// <summary> The specifications of this sampler. </summary>
    public readonly D3D11_SAMPLER_DESC Description = description;

    private ComPtr<ID3D11SamplerState> _sampler;

    /// <summary> Creates a <see cref="Sampler"/> from an existing Direct3D object. </summary>
    /// <param name="sampler"> The existing object. </param>
    /// <param name="addRef"> Whether to increment the reference count of <paramref name="sampler"/>. </param>
    public unsafe Sampler(ID3D11SamplerState* sampler, bool addRef = true) : this(GetDescription(sampler))
    {
        if (addRef)
            sampler->AddRef();

        _sampler.Attach(sampler);
    }

    ~Sampler()
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
        => _sampler.Dispose();

    /// <summary> Gets the Direct3D sampler object, creating it if necessary. </summary>
    /// <returns> The sampler object. </returns>
    public unsafe ID3D11SamplerState* GetOrCreateSampler()
    {
        if (!_sampler.Valid)
            _sampler.Attach(CreateSampler());

        return _sampler;
    }

    private unsafe ID3D11SamplerState* CreateSampler()
    {
        ID3D11SamplerState* sampler;
        fixed (D3D11_SAMPLER_DESC* pDesc = &Description)
        {
            Marshal.ThrowExceptionForHR(
                CustomRenderManager.Instance.Device->CreateSamplerState(pDesc, &sampler));
        }

        return sampler;
    }

    private static unsafe D3D11_SAMPLER_DESC GetDescription(ID3D11SamplerState* sampler)
    {
        D3D11_SAMPLER_DESC desc;
        sampler->GetDesc(&desc);
        return desc;
    }
}
