cbuffer uniforms : register(b1)
{
    float ior;
    float depth;
};

Texture2D<float2> normal_map : register(t0);
RWTexture2D<uint> hits : register(u0);

// This is very naive and should only serve as a test/demo of Luna.DirectX.

[numthreads(8, 8, 1)]
void main(uint3 thread_id : SV_DispatchThreadID)
{
    uint2 size;
    normal_map.GetDimensions(size.x, size.y);

    if (any(thread_id.xy > size))
        return;

    float2 normal_xy = normal_map[thread_id.xy] * 2.0f - 1.0f;
    float normal_z = sqrt(max(0.0f, 1.0f - dot(normal_xy, normal_xy)));
    float3 normal = float3(normal_xy, normal_z);
    float3 direction = refract(float3(0.0f, 0.0f, -1.0f), normal, ior);
    if (direction.z > -1e-5f)
        return;

    float2 uv = ((float2)thread_id.xy + 0.5f) / (float2)size;
    float2 displacement = direction.xy * depth / -direction.z;

    InterlockedAdd(hits[(uint2)trunc(frac(uv + displacement) * (float2)size)], 1);
}
