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
        /// <param name="desc"> The specifications of the texture. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetDescription(out D3D11_TEXTURE2D_DESC desc)
            => GetDescriptionFromView((ID3D11ShaderResourceView*)id.Value, out desc);
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
    /// <param name="desc"> The specifications. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetDescription(ID3D11Buffer* buffer, out D3D11_BUFFER_DESC desc)
    {
        fixed (D3D11_BUFFER_DESC* pDesc = &desc)
        {
            buffer->GetDesc(pDesc);
        }
    }

    /// <summary> Gets the specifications of a 2D texture. </summary>
    /// <param name="texture"> The texture. </param>
    /// <param name="desc"> The specifications. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetDescription(ID3D11Texture2D* texture, out D3D11_TEXTURE2D_DESC desc)
    {
        fixed (D3D11_TEXTURE2D_DESC* pDesc = &desc)
        {
            texture->GetDesc(pDesc);
        }
    }

    /// <summary> Gets the specifications of a 2D texture from a view (shader resource, depth-stencil, render target, etc.) of it. </summary>
    /// <param name="view"> The view. </param>
    /// <param name="desc"> The specifications of the texture. </param>
    /// <typeparam name="T"> The type of view (shader resource, depth-stencil, render target, etc.). </typeparam>
    public static void GetDescriptionFromView<T>(T* view, out D3D11_TEXTURE2D_DESC desc) where T : unmanaged, ID3D11View.Interface
    {
        using var resource = new ComPtr<ID3D11Resource>();
        view->GetResource(resource.GetAddressOf());
        using var texture2D = new ComPtr<ID3D11Texture2D>();
        Marshal.ThrowExceptionForHR(resource.As(&texture2D));
        GetDescription(texture2D, out desc);
    }

    /// <summary> Gets the dimensions of a 2D texture from a view (shader resource, depth-stencil, render target, etc.) of it. </summary>
    /// <param name="view"> The view. </param>
    /// <typeparam name="T"> The type of view (shader resource, depth-stencil, render target, etc.). </typeparam>
    /// <returns> The dimensions of the texture. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (uint Width, uint Height) GetDimensionsFromView<T>(T* view) where T : unmanaged, ID3D11View.Interface
    {
        GetDescriptionFromView(view, out var desc);
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
