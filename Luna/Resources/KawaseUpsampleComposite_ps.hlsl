#include "include/FsQuad.hlsli"
#include "include/DualKawase.hlsli"
#include "include/Utility.hlsli"

// Dual Kawase blur, pass 6 (upsample + composite)
// Mostly borrowed and adapted from:
// https://github.com/goatcorp/Dalamud/blob/master/Dalamud/Interface/ImGuiBackend/Renderers/kawase-upsample-composite.ps.hlsl
// Original licensed under AGPL-3.0-or-later

float4 main(fs_quad_vertex vertex) : SV_Target
{
    float4 input = input_texture.Sample(clamp_sampler, vertex.uv);
    input.a *= unblurred_opacity;

    float2 rect_min = blur_rect_uv_min * resolution;
    float2 rect_max = blur_rect_uv_max * resolution;
    float mask = im_rounding_mask(vertex.uv * resolution - rect_min, rect_max - rect_min, blur_rect_rounding);
    if (mask <= 0.0f)
        return input;

    float4 result = kawase_upsample(rcp_resolution, vertex.uv);

    return lerp(input, result, mask);
}
