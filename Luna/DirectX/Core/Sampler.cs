using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Luna.DirectX;

/// <summary> A Direct3D sampler. </summary>
/// <param name="description"> The specifications of the sampler. </param>
public class Sampler(in D3D11_SAMPLER_DESC description) : IDisposable
{
    /// <summary> A default sampler. </summary>
    public static readonly Sampler Default = new(D3D11_SAMPLER_DESC.DEFAULT);

    /// <summary> Similar to <see cref="Default"/>, but with nearest-neighbor filtering. </summary>
    public static readonly Sampler DefaultNearestNeighbor = new Sampler(D3D11_SAMPLER_DESC.DEFAULT with
    {
        Filter = D3D11_FILTER.D3D11_FILTER_MIN_MAG_MIP_POINT,
    });

    /// <summary> The specifications of this sampler. </summary>
    public readonly D3D11_SAMPLER_DESC Description = description;

    private ComPtr<ID3D11SamplerState> _sampler;

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
}
