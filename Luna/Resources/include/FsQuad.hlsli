#ifndef FSQUAD_HLSLI_INCLUDED
#define FSQUAD_HLSLI_INCLUDED

struct fs_quad_vertex
{
    float4 position : SV_Position;
    float2 uv       : TEXCOORD0;
};

cbuffer resolution : register(b0)
{
    float2 resolution;
    float2 rcp_resolution;
};

#endif
