#include "include/FsQuad.hlsli"

cbuffer uniforms : register(b1)
{
    float4 basis_red;
    float4 basis_green;
    float4 basis_blue;
    float4 basis_alpha;
    float4 origin;
};

Texture2D input_texture : register(t0);
SamplerState input_sampler : register(s0);

float4 main(fs_quad_vertex vertex) : SV_Target
{
    float4 sample = input_texture.Sample(input_sampler, vertex.uv);
    return
        sample.r * basis_red +
        sample.g * basis_green +
        sample.b * basis_blue +
        sample.a * basis_alpha +
        origin;
}
