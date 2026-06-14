#include "include/FsQuad.hlsli"
#include "include/DualKawase.hlsli"

// Dual Kawase blur, pass 4,5 (only upsample)

float4 main(fs_quad_vertex vertex) : SV_Target
{
    return kawase_upsample(rcp_resolution, vertex.uv);
}
