using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Luna.DirectX;

/// <summary> Utility and extension methods for COM and DirectX. </summary>
public static unsafe class DxUtility
{
    /// <summary> Extensions for COM smart pointers. </summary>
    /// <param name="ptr"> A pointer to a COM object. </param>
    /// <typeparam name="T"> The type of the COM object. </typeparam>
    extension<T>(ComPtr<T> ptr) where T : unmanaged, IUnknown.Interface
    {
        /// <summary> Whether the current pointer is valid (i.e. not <c>null</c>). </summary>
        public bool Valid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ptr.Get() is not null;
        }
    }

    /// <summary> Extensions for ImGui texture IDs. </summary>
    /// <param name="id"> The ImGui texture ID. </param>
    extension(ImTextureId id)
    {
        /// <summary> Gets the dimensions of a 2D texture. </summary>
        /// <returns> The dimensions of the texture. </returns>
        public (uint Width, uint Height) Dimensions
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetDimensionsFromView((ID3D11ShaderResourceView*)id.Value);
        }

        /// <summary> Gets the specifications of a 2D texture. </summary>
        /// <returns> The specifications of the texture. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public D3D11_TEXTURE2D_DESC GetDescription()
            => GetDescriptionFromView((ID3D11ShaderResourceView*)id.Value);
    }

    /// <summary> Extensions for DXGI formats. </summary>
    /// <param name="format"> The DXGI format. </param>
    extension(DXGI_FORMAT format)
    {
        /// <summary> Gets the component type of this DXGI format. </summary>
        public DxgiFormatComponentType ComponentType
            => format switch
            {
                DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT         => DxgiFormatComponentType.Float,
                DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_UINT          => DxgiFormatComponentType.UInt,
                DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_SINT          => DxgiFormatComponentType.SInt,
                DXGI_FORMAT.DXGI_FORMAT_R32G32B32_FLOAT            => DxgiFormatComponentType.Float,
                DXGI_FORMAT.DXGI_FORMAT_R32G32B32_UINT             => DxgiFormatComponentType.UInt,
                DXGI_FORMAT.DXGI_FORMAT_R32G32B32_SINT             => DxgiFormatComponentType.SInt,
                DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT         => DxgiFormatComponentType.Float,
                DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UNORM         => DxgiFormatComponentType.UNorm,
                DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UINT          => DxgiFormatComponentType.UInt,
                DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_SNORM         => DxgiFormatComponentType.SNorm,
                DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_SINT          => DxgiFormatComponentType.SInt,
                DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT               => DxgiFormatComponentType.Float,
                DXGI_FORMAT.DXGI_FORMAT_R32G32_UINT                => DxgiFormatComponentType.UInt,
                DXGI_FORMAT.DXGI_FORMAT_R32G32_SINT                => DxgiFormatComponentType.SInt,
                DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UNORM          => DxgiFormatComponentType.UNorm,
                DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UINT           => DxgiFormatComponentType.UInt,
                DXGI_FORMAT.DXGI_FORMAT_R11G11B10_FLOAT            => DxgiFormatComponentType.Float,
                DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM             => DxgiFormatComponentType.UNorm,
                DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM_SRGB        => DxgiFormatComponentType.UNorm,
                DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UINT              => DxgiFormatComponentType.UInt,
                DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_SNORM             => DxgiFormatComponentType.SNorm,
                DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_SINT              => DxgiFormatComponentType.SInt,
                DXGI_FORMAT.DXGI_FORMAT_R16G16_FLOAT               => DxgiFormatComponentType.Float,
                DXGI_FORMAT.DXGI_FORMAT_R16G16_UNORM               => DxgiFormatComponentType.UNorm,
                DXGI_FORMAT.DXGI_FORMAT_R16G16_UINT                => DxgiFormatComponentType.UInt,
                DXGI_FORMAT.DXGI_FORMAT_R16G16_SNORM               => DxgiFormatComponentType.SNorm,
                DXGI_FORMAT.DXGI_FORMAT_R16G16_SINT                => DxgiFormatComponentType.SInt,
                DXGI_FORMAT.DXGI_FORMAT_D32_FLOAT                  => DxgiFormatComponentType.Float,
                DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT                  => DxgiFormatComponentType.Float,
                DXGI_FORMAT.DXGI_FORMAT_R32_UINT                   => DxgiFormatComponentType.UInt,
                DXGI_FORMAT.DXGI_FORMAT_R32_SINT                   => DxgiFormatComponentType.SInt,
                DXGI_FORMAT.DXGI_FORMAT_R8G8_UNORM                 => DxgiFormatComponentType.UNorm,
                DXGI_FORMAT.DXGI_FORMAT_R8G8_UINT                  => DxgiFormatComponentType.UInt,
                DXGI_FORMAT.DXGI_FORMAT_R8G8_SNORM                 => DxgiFormatComponentType.SNorm,
                DXGI_FORMAT.DXGI_FORMAT_R8G8_SINT                  => DxgiFormatComponentType.SInt,
                DXGI_FORMAT.DXGI_FORMAT_R16_FLOAT                  => DxgiFormatComponentType.Float,
                DXGI_FORMAT.DXGI_FORMAT_D16_UNORM                  => DxgiFormatComponentType.UNorm,
                DXGI_FORMAT.DXGI_FORMAT_R16_UNORM                  => DxgiFormatComponentType.UNorm,
                DXGI_FORMAT.DXGI_FORMAT_R16_UINT                   => DxgiFormatComponentType.UInt,
                DXGI_FORMAT.DXGI_FORMAT_R16_SNORM                  => DxgiFormatComponentType.SNorm,
                DXGI_FORMAT.DXGI_FORMAT_R16_SINT                   => DxgiFormatComponentType.SInt,
                DXGI_FORMAT.DXGI_FORMAT_R8_UNORM                   => DxgiFormatComponentType.UNorm,
                DXGI_FORMAT.DXGI_FORMAT_R8_UINT                    => DxgiFormatComponentType.UInt,
                DXGI_FORMAT.DXGI_FORMAT_R8_SNORM                   => DxgiFormatComponentType.SNorm,
                DXGI_FORMAT.DXGI_FORMAT_R8_SINT                    => DxgiFormatComponentType.SInt,
                DXGI_FORMAT.DXGI_FORMAT_A8_UNORM                   => DxgiFormatComponentType.UNorm,
                DXGI_FORMAT.DXGI_FORMAT_R1_UNORM                   => DxgiFormatComponentType.UNorm,
                DXGI_FORMAT.DXGI_FORMAT_R9G9B9E5_SHAREDEXP         => DxgiFormatComponentType.Float,
                DXGI_FORMAT.DXGI_FORMAT_R8G8_B8G8_UNORM            => DxgiFormatComponentType.UNorm,
                DXGI_FORMAT.DXGI_FORMAT_G8R8_G8B8_UNORM            => DxgiFormatComponentType.UNorm,
                DXGI_FORMAT.DXGI_FORMAT_B5G6R5_UNORM               => DxgiFormatComponentType.UNorm,
                DXGI_FORMAT.DXGI_FORMAT_B5G5R5A1_UNORM             => DxgiFormatComponentType.UNorm,
                DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM             => DxgiFormatComponentType.UNorm,
                DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM             => DxgiFormatComponentType.UNorm,
                DXGI_FORMAT.DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM => DxgiFormatComponentType.UNorm,
                DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM_SRGB        => DxgiFormatComponentType.UNorm,
                DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM_SRGB        => DxgiFormatComponentType.UNorm,
                DXGI_FORMAT.DXGI_FORMAT_B4G4R4A4_UNORM             => DxgiFormatComponentType.UNorm,
                DXGI_FORMAT.DXGI_FORMAT_A4B4G4R4_UNORM             => DxgiFormatComponentType.UNorm,
                _                                                  => DxgiFormatComponentType.Unknown,
            };
    }

    /// <summary> Wraps a COM object pointer in a <see cref="ComPtr{T}"/> without incrementing its reference count. </summary>
    /// <param name="ptr"> The raw pointer. </param>
    /// <typeparam name="T"> The object's type. </typeparam>
    /// <returns> The wrapped pointer. </returns>
    /// <seealso cref="ComPtr{T}.Attach"/>
    public static ComPtr<T> NonOwningComPtr<T>(T* ptr) where T : unmanaged, IUnknown.Interface
    {
        var comPtr = new ComPtr<T>();
        comPtr.Attach(ptr);
        return comPtr;
    }

    /// <summary> Gets the specifications of a buffer. </summary>
    /// <param name="buffer"> The buffer. </param>
    /// <returns> The specifications. </returns>
    [MethodImpl(ImSharpConfiguration.Inl)]
    [SkipLocalsInit]
    public static D3D11_BUFFER_DESC GetDescription(ID3D11Buffer* buffer)
    {
        D3D11_BUFFER_DESC ret;
        buffer->GetDesc(&ret);
        return ret;
    }

    /// <summary> Gets the specifications of a 2D texture. </summary>
    /// <param name="texture"> The texture. </param>
    /// <returns> The specifications. </returns>
    [MethodImpl(ImSharpConfiguration.Inl)]
    [SkipLocalsInit]
    public static D3D11_TEXTURE2D_DESC GetDescription(ID3D11Texture2D* texture)
    {
        D3D11_TEXTURE2D_DESC ret;
        texture->GetDesc(&ret);
        return ret;
    }

    /// <summary> Gets the specifications of a 2D texture from a view (shader resource, depth-stencil, render target, etc.) of it. </summary>
    /// <typeparam name="T"> The type of view (shader resource, depth-stencil, render target, etc.). </typeparam>
    /// <param name="view"> The view. </param>
    /// <returns> The specifications of the texture. </returns>
    public static D3D11_TEXTURE2D_DESC GetDescriptionFromView<T>(T* view)
        where T : unmanaged, ID3D11View.Interface
    {
        using var resource = new ComPtr<ID3D11Resource>();
        view->GetResource(resource.GetAddressOf());
        using var texture2D = new ComPtr<ID3D11Texture2D>();
        Marshal.ThrowExceptionForHR(resource.As(&texture2D));
        return GetDescription(texture2D);
    }

    /// <summary> Gets the dimensions of a 2D texture from a view (shader resource, depth-stencil, render target, etc.) of it. </summary>
    /// <param name="view"> The view. </param>
    /// <typeparam name="T"> The type of view (shader resource, depth-stencil, render target, etc.). </typeparam>
    /// <returns> The dimensions of the texture. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (uint Width, uint Height) GetDimensionsFromView<T>(T* view) where T : unmanaged, ID3D11View.Interface
    {
        var desc = GetDescriptionFromView(view);
        return (desc.Width, desc.Height);
    }

    /// <summary> Constructs a viewport with most fields set to reasonable defaults, and binds it to a device context. </summary>
    /// <param name="deviceContext"> The device context. </param>
    /// <param name="width"> The width of the viewport. </param>
    /// <param name="height"> The height of the viewport. </param>
    internal static void SetSimpleViewport(ID3D11DeviceContext* deviceContext, float width, float height)
    {
        var viewport = new D3D11_VIEWPORT
        {
            TopLeftX = 0.0f,
            TopLeftY = 0.0f,
            Width    = width,
            Height   = height,
            MinDepth = 0.0f,
            MaxDepth = 1.0f,
        };
        deviceContext->RSSetViewports(1, &viewport);
    }

    /// <summary> Constructs a rasterizer state from the given specifications, and binds it to a device context. </summary>
    /// <param name="deviceContext"> The device context. </param>
    /// <param name="desc"> The rasterizer state specifications. </param>
    internal static void SetRasterizerState(ID3D11DeviceContext* deviceContext, in D3D11_RASTERIZER_DESC desc)
    {
        using var device = new ComPtr<ID3D11Device>();
        deviceContext->GetDevice(device.GetAddressOf());
        using var rsState = new ComPtr<ID3D11RasterizerState>();
        fixed (D3D11_RASTERIZER_DESC* pDesc = &desc)
        {
            Marshal.ThrowExceptionForHR(device.Get()->CreateRasterizerState(pDesc, rsState.GetAddressOf()));
        }

        deviceContext->RSSetState(rsState);
    }

    /// <summary> Constructs a depth-stencil state from the given specifications, and binds it to a device context. </summary>
    /// <param name="deviceContext"> The device context. </param>
    /// <param name="desc"> The depth-stencil state specifications. </param>
    /// <param name="stencilRef"> The stencil reference value. </param>
    internal static void SetDepthStencilState(ID3D11DeviceContext* deviceContext, in D3D11_DEPTH_STENCIL_DESC desc, uint stencilRef)
    {
        using var device = new ComPtr<ID3D11Device>();
        deviceContext->GetDevice(device.GetAddressOf());
        using var dsState = new ComPtr<ID3D11DepthStencilState>();
        fixed (D3D11_DEPTH_STENCIL_DESC* pDesc = &desc)
        {
            Marshal.ThrowExceptionForHR(device.Get()->CreateDepthStencilState(pDesc, dsState.GetAddressOf()));
        }

        deviceContext->OMSetDepthStencilState(dsState, stencilRef);
    }
}
