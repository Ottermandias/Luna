#include "include/FsQuad.hlsli"

cbuffer uniforms : register(b1)
{
    float4 exponent;
    float4 palette[32];
};

Texture2D<float2> input_texture : register(t0);
SamplerState input_sampler : register(s0);

float4 main(fs_quad_vertex vertex) : SV_Target
{
    float2 sample = input_texture.Sample(input_sampler, vertex.uv);
    uint pair_index = (uint)round(saturate(sample.r) * 15.0f);
    float4 row_a = palette[pair_index << 1];
    float4 row_b = palette[(pair_index << 1) | 1];
    float4 raw = lerp(row_b, row_a, sample.g);
    return sign(raw) * pow(abs(raw), exponent);
}
