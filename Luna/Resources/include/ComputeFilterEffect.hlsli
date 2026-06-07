#ifndef COMPUTEFILTEREFFECT_HLSLI_INCLUDED
#define COMPUTEFILTEREFFECT_HLSLI_INCLUDED

cbuffer thread_group_count : register(b0)
{
    uint3 thread_group_count;
};

#endif
