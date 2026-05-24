#include "include/FsQuad.hlsli"

Texture2D input_texture : register(t0);
SamplerState input_sampler : register(s0);

float4 main(fs_quad_vertex vertex) : SV_Target
{
    return input_texture.Sample(input_sampler, vertex.uv);
}
