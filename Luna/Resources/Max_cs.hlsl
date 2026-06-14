Texture2D<uint> input : register(t0);
RWTexture2D<uint> output : register(u0);

[numthreads(8, 8, 1)]
void main(uint3 thread_id : SV_DispatchThreadID)
{
    uint2 size;
    input.GetDimensions(size.x, size.y);

    if (any(thread_id.xy > size))
        return;

    InterlockedMax(output[uint2(0, 0)], input[thread_id.xy]);
}
