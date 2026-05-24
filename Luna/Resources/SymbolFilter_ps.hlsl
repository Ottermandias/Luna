#include "include/FsQuad.hlsli"
#include "include/SymbolFilter.hlsli"

Texture2D input_texture : register(t0);
SamplerState input_sampler : register(s0);

float4 main(fs_quad_vertex vertex) : SV_Target
{
    symbol_filter sym_flt;
    sym_flt.initialize(input_texture, input_sampler);
    return sym_flt.sample(vertex.uv);
}
