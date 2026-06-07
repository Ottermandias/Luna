Texture2D<uint> input : register(t0);
RWTexture2D<uint> output : register(u0);

[numthreads(1, 1, 1)]
void main(uint3 thread_id : SV_DispatchThreadID)
{
    InterlockedMax(output[uint2(0, 0)], input[thread_id.xy]);
}
