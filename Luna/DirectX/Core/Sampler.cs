using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Luna.DirectX;

/// <summary> A Direct3D sampler. </summary>
/// <param name="description"> The specifications of the sampler. </param>
public class Sampler(in D3D11_SAMPLER_DESC description) : IDisposable
{
    /// <summary> A sampler with UV clamping and bilinear filtering. Can be used as a default. </summary>
    public static readonly Sampler ClampBilinear = new(D3D11_SAMPLER_DESC.DEFAULT);

    /// <summary> A sampler with UV clamping and nearest-neighbor filtering. </summary>
    public static readonly Sampler ClampNearestNeighbor = new(D3D11_SAMPLER_DESC.DEFAULT with
    {
        Filter = D3D11_FILTER.D3D11_FILTER_MIN_MAG_MIP_POINT,
    });

    /// <summary> A sampler with UV wrapping and bilinear filtering. </summary>
    public static readonly Sampler WrapBilinear = new(D3D11_SAMPLER_DESC.DEFAULT with
    {
        AddressU = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_WRAP,
        AddressV = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_WRAP,
        AddressW = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_WRAP,
    });

    /// <summary> A sampler with UV wrapping and nearest-neighbor filtering. </summary>
    public static readonly Sampler WrapNearestNeighbor = new(D3D11_SAMPLER_DESC.DEFAULT with
    {
        Filter = D3D11_FILTER.D3D11_FILTER_MIN_MAG_MIP_POINT,
        AddressU = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_WRAP,
        AddressV = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_WRAP,
        AddressW = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_WRAP,
    });

    /// <summary> A sampler with UV mirroring and bilinear filtering. </summary>
    public static readonly Sampler MirrorBilinear = new(D3D11_SAMPLER_DESC.DEFAULT with
    {
        AddressU = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_MIRROR,
        AddressV = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_MIRROR,
        AddressW = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_MIRROR,
    });

    /// <summary> A sampler with UV mirroring and nearest-neighbor filtering. </summary>
    public static readonly Sampler MirrorNearestNeighbor = new(D3D11_SAMPLER_DESC.DEFAULT with
    {
        Filter = D3D11_FILTER.D3D11_FILTER_MIN_MAG_MIP_POINT,
        AddressU = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_MIRROR,
        AddressV = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_MIRROR,
        AddressW = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_MIRROR,
    });

    /// <summary> A sampler that pads around textures with solid white, and with bilinear filtering. </summary>
    public static readonly Sampler BorderWhiteBilinear = new(D3D11_SAMPLER_DESC.DEFAULT with
    {
        AddressU = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_BORDER,
        AddressV = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_BORDER,
        AddressW = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_BORDER,
    });

    /// <summary> A sampler that pads around textures with solid white, and with nearest-neighbor filtering. </summary>
    public static readonly Sampler BorderWhiteNearestNeighbor = new(D3D11_SAMPLER_DESC.DEFAULT with
    {
        Filter = D3D11_FILTER.D3D11_FILTER_MIN_MAG_MIP_POINT,
        AddressU = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_BORDER,
        AddressV = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_BORDER,
        AddressW = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_BORDER,
    });

    /// <summary> A sampler that pads around textures with solid black, and with bilinear filtering. </summary>
    public static readonly Sampler BorderBlackBilinear = new(D3D11_SAMPLER_DESC.DEFAULT with
    {
        AddressU = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_BORDER,
        AddressV = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_BORDER,
        AddressW = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_BORDER,
        BorderColor = BorderColor(0.0f, 0.0f, 0.0f, 1.0f),
    });

    /// <summary> A sampler that pads around textures with solid black, and with nearest-neighbor filtering. </summary>
    public static readonly Sampler BorderBlackNearestNeighbor = new(D3D11_SAMPLER_DESC.DEFAULT with
    {
        Filter = D3D11_FILTER.D3D11_FILTER_MIN_MAG_MIP_POINT,
        AddressU = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_BORDER,
        AddressV = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_BORDER,
        AddressW = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_BORDER,
        BorderColor = BorderColor(0.0f, 0.0f, 0.0f, 1.0f),
    });

    /// <summary> A sampler that pads around textures with transparent, and with bilinear filtering. </summary>
    public static readonly Sampler BorderTransparentBilinear = new(D3D11_SAMPLER_DESC.DEFAULT with
    {
        AddressU = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_BORDER,
        AddressV = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_BORDER,
        AddressW = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_BORDER,
        BorderColor = BorderColor(0.0f, 0.0f, 0.0f, 0.0f),
    });

    /// <summary> A sampler that pads around textures with transparent, and with nearest-neighbor filtering. </summary>
    public static readonly Sampler BorderTransparentNearestNeighbor = new(D3D11_SAMPLER_DESC.DEFAULT with
    {
        Filter = D3D11_FILTER.D3D11_FILTER_MIN_MAG_MIP_POINT,
        AddressU = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_BORDER,
        AddressV = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_BORDER,
        AddressW = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_BORDER,
        BorderColor = BorderColor(0.0f, 0.0f, 0.0f, 0.0f),
    });

    /// <summary> The specifications of this sampler. </summary>
    public readonly D3D11_SAMPLER_DESC Description = description;

    private ComPtr<ID3D11SamplerState> _sampler;

    /// <summary> Creates a <see cref="Sampler"/> from an existing Direct3D object. </summary>
    /// <param name="sampler"> The existing object. </param>
    /// <param name="addRef"> Whether to increment the reference count of <paramref name="sampler"/>. </param>
    public unsafe Sampler(ID3D11SamplerState* sampler, bool addRef = true)
        : this(GetDescription(sampler))
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

    [SkipLocalsInit]
    private static unsafe D3D11_SAMPLER_DESC GetDescription(ID3D11SamplerState* sampler)
    {
        D3D11_SAMPLER_DESC desc;
        sampler->GetDesc(&desc);
        return desc;
    }

    private static D3D11_SAMPLER_DESC._BorderColor_e__FixedBuffer BorderColor(float red, float green, float blue, float alpha)
    {
        var color = new D3D11_SAMPLER_DESC._BorderColor_e__FixedBuffer();
        color[0] = red;
        color[1] = green;
        color[2] = blue;
        color[3] = alpha;
        return color;
    }
}
