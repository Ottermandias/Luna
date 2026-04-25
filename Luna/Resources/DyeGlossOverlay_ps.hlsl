#include "../../ImSharp/ImSharp/Resources/include/FsQuad.hlsli"

cbuffer uniforms : register(cb1)
{
    float rounding;
};

float rounding_alpha(float2 uv)
{
    float effective_rounding = floor(min(rounding, min(resolution.x, resolution.y) * 0.5 - 1.0));
    float2 position = uv * resolution;
    float2 distance_to_edge = min(position, resolution - position);
    float2 rounding_position = max(0.0, effective_rounding - distance_to_edge);
    return 1.0 - smoothstep(max(0.0, effective_rounding - 0.5), effective_rounding + 0.5, length(rounding_position));
}

float4 main(fs_quad_vertex vertex) : SV_Target
{
    float value = 1.0 - abs(vertex.uv.x - vertex.uv.y);
    return float4(value, value, value, 0.314 * rounding_alpha(vertex.uv));
}
