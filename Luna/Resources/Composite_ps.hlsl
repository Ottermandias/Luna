#include "include/FsQuad.hlsli"
#include "include/CustomSampling.hlsli"
#include "include/CustomBlending.hlsli"

cbuffer uniforms : register(b1)
{
    float4 foreground_transform;
    float2 foreground_offset;
    uint blend;
    float3 color_composite_weights;
    float3 alpha_composite_weights;
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

    float a_dst  = bg.a * (1.0f - fg.a);
    float a_src  = fg.a * (1.0f - bg.a);
    float a_both = fg.a * bg.a;

    dispatch_blend blend_fn;
    blend_fn.initialize(blend, blend_parameters);
    float3 blended = blend_fn.blend(bg, fg).rgb;

    float3 color = a_dst  * color_composite_weights.x * bg.rgb
                 + a_src  * color_composite_weights.y * fg.rgb
                 + a_both * color_composite_weights.z * blended;

    float alpha = dot(float3(a_dst, a_src, a_both), alpha_composite_weights);

    return float4(alpha == 0.0f ? 0.0f : color / alpha, alpha);
}
