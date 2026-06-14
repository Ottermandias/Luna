#include "include/FsQuad.hlsli"

Texture2D<uint> input_texture : register(t0);
Texture2D<uint> max_texture : register(t1);

float main(fs_quad_vertex vertex) : SV_Target
{
    return (float)input_texture[(uint2)trunc(vertex.uv * resolution)] / (float)max(1u, max_texture[uint2(0, 0)]);
}
