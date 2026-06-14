#include "include/FsQuad.hlsli"
#include "include/CustomSampling.hlsli"
#include "include/CustomBlending.hlsli"

cbuffer uniforms : register(b1)
{
    float4 foreground_transform;
    float2 foreground_offset;
    uint blend;
    blend_parameters_t blend_parameters;
};

cbuffer linkage : register(b2)
{
    uint foreground_filter;
};

Texture2D background_texture : register(t0);
Texture2D foreground_texture : register(t1);
SamplerState background_sampler : register(s0);
SamplerState foreground_sampler : register(s1);

float4 main(fs_quad_vertex vertex) : SV_Target
{
    float4 bg = background_texture.Sample(background_sampler, vertex.uv);
    float2 fg_uv = float2(dot(foreground_transform.xy, vertex.uv), dot(foreground_transform.zw, vertex.uv))
        + foreground_offset;

    dispatch_filter foreground;
    foreground.initialize(foreground_filter, foreground_texture, foreground_sampler);
    float4 fg = foreground.sample(fg_uv);

    dispatch_blend blend_fn;
    blend_fn.initialize(blend, blend_parameters);
    return blend_fn.blend(bg, fg);
}
