#ifndef IMGUIUTIL_HLSLI_INCLUDED
#define IMGUIUTIL_HLSLI_INCLUDED

float im_rounding_mask(float2 position, float2 size, float4 rounding)
{
    float4 effective_rounding2 = floor(min(rounding, min(size.x, size.y) * 0.5f - 1.0f));
    float2 effective_rounding1 = position.y >= size.y * 0.5f ? effective_rounding2.wz : effective_rounding2.xy;
    float effective_rounding = position.x >= size.x * 0.5f ? effective_rounding1.y : effective_rounding1.x;
    float2 distance_to_edge = min(position, size - position);
    float2 rounding_position = max(0.0f, effective_rounding - distance_to_edge);
    return 1.0f - smoothstep(max(0.0f, effective_rounding - 0.5f), effective_rounding + 0.5f, length(rounding_position));
}

#ifdef FSQUAD_HLSLI_INCLUDED
float im_rounding_mask(float2 uv, float4 rounding)
{
    return im_rounding_mask(uv * resolution, resolution, rounding);
}
#endif

#endif
