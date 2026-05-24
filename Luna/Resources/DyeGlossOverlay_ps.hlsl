#include "include/FsQuad.hlsli"
#include "include/ImGuiUtil.hlsli"

cbuffer uniforms : register(b1)
{
    float4 rounding;
};

float4 main(fs_quad_vertex vertex) : SV_Target
{
    float value = 1.0f - abs(vertex.uv.x - vertex.uv.y);
    return float4(value, value, value, 0.314f * im_rounding_mask(vertex.uv, rounding));
}
