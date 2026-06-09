#include "include/FsQuad.hlsli"
#include "include/CustomSampling.hlsli"

cbuffer linkage : register(b2)
{
    uint filter;
};

Texture2D input_texture : register(t0);
SamplerState input_sampler : register(s0);

float4 main(fs_quad_vertex vertex) : SV_Target
{
    dispatch_filter input;
    input.initialize(filter, input_texture, input_sampler);
    return input.sample(vertex.uv);
}
