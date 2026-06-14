#include "include/FsQuad.hlsli"
#include "include/DualKawase.hlsli"

// Dual Kawase blur pass 1,2,3 (downsample)

float4 main(fs_quad_vertex vertex) : SV_Target
{
    return kawase_downsample(rcp_resolution, vertex.uv);
}
