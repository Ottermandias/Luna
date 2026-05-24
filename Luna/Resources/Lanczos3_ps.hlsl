#include "include/FsQuad.hlsli"
#include "include/Lanczos3.hlsli"

Texture2D input_texture : register(t0);
SamplerState input_sampler : register(s0);

float4 main(fs_quad_vertex vertex) : SV_Target
{
    lanczos3 lanczos;
    lanczos.initialize(input_texture, input_sampler);
    return lanczos.sample(vertex.uv);
}
