using TerraFX.Interop.DirectX;

namespace Luna.DirectX;

/// <summary> A target clear strategy that clears all the buffers with constant values. </summary>
/// <param name="depth"> The depth value. </param>
/// <param name="stencil"> The stencil value. </param>
/// <param name="color"> The color value. </param>
/// <param name="intValue"> The integer value. </param>
public sealed class SimpleTargetClearStrategy(float depth, byte stencil, Vector4 color, (uint X, uint Y, uint Z, uint W) intValue)
    : ITargetClearStrategy
{
    private readonly Vector4 _color = color;

    private readonly UintClearValue _intValue = new()
    {
        X = intValue.X,
        Y = intValue.Y,
        Z = intValue.Z,
        W = intValue.W,
    };

    /// <inheritdoc/>
    public unsafe void ClearDepthStencil(ID3D11DeviceContext* deviceContext, ID3D11DepthStencilView* depthStencilView)
        => deviceContext->ClearDepthStencilView(depthStencilView,
            (uint)(D3D11_CLEAR_FLAG.D3D11_CLEAR_DEPTH | D3D11_CLEAR_FLAG.D3D11_CLEAR_STENCIL), depth, stencil);

    /// <inheritdoc/>
    public unsafe void ClearRenderTarget(ID3D11DeviceContext* deviceContext, int outputIndex, ID3D11RenderTargetView* renderTargetView)
    {
        fixed (Vector4* pColor = &_color)
        {
            deviceContext->ClearRenderTargetView(renderTargetView, (float*)pColor);
        }
    }

    /// <inheritdoc/>
    public unsafe void ClearUnorderedAccessView(ID3D11DeviceContext* deviceContext, int outputIndex,
        ID3D11UnorderedAccessView* unorderedAccessView)
    {
        D3D11_UNORDERED_ACCESS_VIEW_DESC desc;
        unorderedAccessView->GetDesc(&desc);
        switch (desc.Format.ComponentType)
        {
            case DxgiFormatComponentType.Float or DxgiFormatComponentType.UNorm or DxgiFormatComponentType.SNorm:
                fixed (Vector4* pColor = &_color)
                {
                    deviceContext->ClearUnorderedAccessViewFloat(unorderedAccessView, (float*)pColor);
                }

                break;
            case DxgiFormatComponentType.UInt or DxgiFormatComponentType.SInt:
                fixed (UintClearValue* pIntValue = &_intValue)
                {
                    deviceContext->ClearUnorderedAccessViewUint(unorderedAccessView, (uint*)pIntValue);
                }

                break;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    private struct UintClearValue
    {
        public uint X;
        public uint Y;
        public uint Z;
        public uint W;
    }
}
