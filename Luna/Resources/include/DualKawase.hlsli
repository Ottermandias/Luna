#ifndef DUALKAWASE_HLSLI_INCLUDED
#define DUALKAWASE_HLSLI_INCLUDED

// Mostly borrowed and adapted from:
// https://github.com/goatcorp/Dalamud/blob/master/Dalamud/Interface/ImGuiBackend/Renderers/kawase-downsample.ps.hlsl
// https://github.com/goatcorp/Dalamud/blob/master/Dalamud/Interface/ImGuiBackend/Renderers/kawase-upsample.ps.hlsl
// Originals licensed under AGPL-3.0-or-later

cbuffer blur : register(b1)
{
    float2 blur_rect_uv_min;
    float2 blur_rect_uv_max;
    float4 blur_rect_rounding;
    float blur_strength; // Kawase spread factor; typical range 0.5 – 4
    float unblurred_opacity;
}

Texture2D source_texture : register(t0);
SamplerState clamp_sampler : register(s0);

Texture2D input_texture : register(t1);

float4 kawase_downsample(float2 rcp_resolution, float2 uv) : SV_Target
{
    // Upsample pass, the input resolution is twice the output resolution.
    // half_pixel = rcp(resolution * 2) * 0.5 = rcp_resolution * 0.5 * 0.5
    float2 half_pixel = rcp_resolution * 0.25f;
    float2 ofs = half_pixel * (1.0f + blur_strength);

    float4 sum = source_texture.SampleLevel(clamp_sampler, uv,                          0) * 4.0f;
    sum += source_texture.SampleLevel(clamp_sampler, uv - ofs,                    0);
    sum += source_texture.SampleLevel(clamp_sampler, uv + ofs,                    0);
    sum += source_texture.SampleLevel(clamp_sampler, uv + float2( ofs.x, -ofs.y), 0);
    sum += source_texture.SampleLevel(clamp_sampler, uv + float2(-ofs.x,  ofs.y), 0);

    return sum * 0.125f;
}

float4 kawase_upsample(float2 rcp_resolution, float2 uv) : SV_Target
{
    // Upsample pass, the input resolution is half the output resolution.
    // hp = rcp(resolution * 0.5) * 0.5 = rcp_resolution * 2.0 * 0.5
    float2 hp = rcp_resolution; // half a source texel
    float2 fp = hp * 2.0f; // one full source texel
    float ofs = 1.0f + blur_strength;

    float4 sum = source_texture.SampleLevel(clamp_sampler, uv + float2(-fp.x, 0.0f) * ofs, 0);
    sum += source_texture.SampleLevel(clamp_sampler, uv + float2(-hp.x, hp.y) * ofs, 0) * 2.0f;
    sum += source_texture.SampleLevel(clamp_sampler, uv + float2( 0.0f, fp.y) * ofs, 0);
    sum += source_texture.SampleLevel(clamp_sampler, uv + float2( hp.x, hp.y) * ofs, 0) * 2.0f;
    sum += source_texture.SampleLevel(clamp_sampler, uv + float2( fp.x, 0.0f) * ofs, 0);
    sum += source_texture.SampleLevel(clamp_sampler, uv + float2( hp.x, -hp.y) * ofs, 0) * 2.0f;
    sum += source_texture.SampleLevel(clamp_sampler, uv + float2( 0.0f, -fp.y) * ofs, 0);
    sum += source_texture.SampleLevel(clamp_sampler, uv + float2(-hp.x, -hp.y) * ofs, 0) * 2.0f;

    return sum * (1.0f / 12.0f);
}

#endif
