using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Luna.DirectX;

/// <summary> A dynamic object that can be rendered on an ImGui canvas in place of an image. </summary>
public interface ICustomRenderable
{
    /// <summary> How many outputs this object generates. This should not change over the lifetime of the object. </summary>
    public int OutputCount { get; }

    /// <summary> For how many frames should renders of this object stay cached. Zero if this object should be re-rendered every frame. </summary>
    public int KeepAliveDuration { get; }

    /// <summary> The version of this object. Change to invalidate all existing renders. </summary>
    public long Version { get; }

    /// <summary> How the depth-stencil, render targets and unordered access views shall be cleared before rendering. </summary>
    public ITargetClearStrategy? ClearStrategy { get; }

    /// <summary> The desired depth-stencil state for rendering. </summary>
    public D3D11_DEPTH_STENCIL_DESC DepthStencilState { get; }

    /// <summary> A default depth-stencil state. </summary>
    public static D3D11_DEPTH_STENCIL_DESC DefaultDepthStencilState
        => new()
        {
            DepthEnable      = BOOL.TRUE,
            DepthWriteMask   = D3D11_DEPTH_WRITE_MASK.D3D11_DEPTH_WRITE_MASK_ALL,
            DepthFunc        = D3D11_COMPARISON_FUNC.D3D11_COMPARISON_LESS,
            StencilEnable    = BOOL.FALSE,
            StencilReadMask  = 0xFF,
            StencilWriteMask = 0xFF,
            FrontFace = new D3D11_DEPTH_STENCILOP_DESC
            {
                StencilFunc        = D3D11_COMPARISON_FUNC.D3D11_COMPARISON_ALWAYS,
                StencilDepthFailOp = D3D11_STENCIL_OP.D3D11_STENCIL_OP_KEEP,
                StencilFailOp      = D3D11_STENCIL_OP.D3D11_STENCIL_OP_KEEP,
                StencilPassOp      = D3D11_STENCIL_OP.D3D11_STENCIL_OP_KEEP,
            },
            BackFace = new D3D11_DEPTH_STENCILOP_DESC
            {
                StencilFunc        = D3D11_COMPARISON_FUNC.D3D11_COMPARISON_ALWAYS,
                StencilDepthFailOp = D3D11_STENCIL_OP.D3D11_STENCIL_OP_KEEP,
                StencilFailOp      = D3D11_STENCIL_OP.D3D11_STENCIL_OP_KEEP,
                StencilPassOp      = D3D11_STENCIL_OP.D3D11_STENCIL_OP_KEEP,
            },
        };

    /// <summary> The desired rasterizer state for rendering. </summary>
    public D3D11_RASTERIZER_DESC RasterizerState { get; }

    /// <summary> A default rasterizer state. </summary>
    public static D3D11_RASTERIZER_DESC DefaultRasterizerState
        => new()
        {
            FillMode              = D3D11_FILL_MODE.D3D11_FILL_SOLID,
            CullMode              = D3D11_CULL_MODE.D3D11_CULL_BACK,
            FrontCounterClockwise = BOOL.FALSE,
            DepthBias             = 0,
            SlopeScaledDepthBias  = 0.0f,
            DepthBiasClamp        = 0.0f,
            DepthClipEnable       = BOOL.TRUE,
            ScissorEnable         = BOOL.FALSE,
            MultisampleEnable     = BOOL.FALSE,
            AntialiasedLineEnable = BOOL.FALSE,
        };

    /// <summary> Retrieves the format of one of this object's outputs. This should not change over the lifetime of the object. </summary>
    /// <param name="outputIndex"> The output index. </param>
    /// <returns> The output format. </returns>
    public DXGI_FORMAT GetOutputFormat(int outputIndex);

    /// <summary> Renders this object. </summary>
    /// <param name="width"> The output width. </param>
    /// <param name="height"> The output height. </param>
    /// <param name="deviceContext"> The device context to run commands on. </param>
    public unsafe void Render(uint width, uint height, ID3D11DeviceContext* deviceContext);
}
