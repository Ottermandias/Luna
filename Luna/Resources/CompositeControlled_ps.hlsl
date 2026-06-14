#include "include/FsQuad.hlsli"
#include "include/CustomSampling.hlsli"
#include "include/CustomBlending.hlsli"

cbuffer uniforms : register(b1)
{
    float4 foreground_transform;
    float2 foreground_offset;
    uint blend;
    float3 composite_weights;
    float background_control0;
    float3 control_composite_weights;
    float foreground_control0;
    float4 background_control_weights;
    float4 foreground_control_weights;
    blend_parameters_t blend_parameters;
};

cbuffer linkage : register(b2)
{
    uint foreground_filter;
};

Texture2D background_texture : register(t0);
Texture2D background_control_texture : register(t1);
Texture2D foreground_texture : register(t2);
Texture2D foreground_control_texture : register(t3);
SamplerState background_sampler : register(s0);
SamplerState foreground_sampler : register(s1);

struct output
{
    float4 main    : SV_Target0;
    float  control : SV_Target1;
};

output main(fs_quad_vertex vertex)
{
    float4 bg = background_texture.Sample(background_sampler, vertex.uv);
    float bg_control = dot(background_control_texture.Sample(background_sampler, vertex.uv), background_control_weights)
        + background_control0;
    float2 fg_uv = float2(dot(foreground_transform.xy, vertex.uv), dot(foreground_transform.zw, vertex.uv))
        + foreground_offset;

    dispatch_filter foreground;
    foreground.initialize(foreground_filter, foreground_texture, foreground_sampler);
    float4 fg = foreground.sample(fg_uv);

    dispatch_filter foreground_control;
    foreground_control.initialize(foreground_filter, foreground_control_texture, foreground_sampler);
    float fg_control = dot(foreground_control.sample(fg_uv), foreground_control_weights) + foreground_control0;

    float control_dst  = bg_control * (1.0f - fg_control);
    float control_src  = fg_control * (1.0f - bg_control);
    float control_both = fg_control * bg_control;

    dispatch_blend blend_fn;
    blend_fn.initialize(blend, blend_parameters);
    float4 blended = blend_fn.blend(bg, fg);

    float4 result = control_dst  * composite_weights.x * bg
                  + control_src  * composite_weights.y * fg
                  + control_both * composite_weights.z * blended;

    float control = dot(float3(control_dst, control_src, control_both), control_composite_weights);

    output ret;
    ret.main    = control == 0.0f ? 0.0f : result / control;
    ret.control = control;

    return ret;
}
