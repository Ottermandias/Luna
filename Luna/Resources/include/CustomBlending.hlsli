#ifndef CUSTOMBLENDING_HLSLI_INCLUDED
#define CUSTOMBLENDING_HLSLI_INCLUDED

static const uint blend_function_mask = 0x07FFFFFF;
static const uint blend_hdr           = 0x08000000;
static const uint blend_invert_dest   = 0x10000000;
static const uint blend_invert_src    = 0x20000000;
static const uint blend_invert_ret    = 0x40000000;
static const uint blend_swap_inputs   = 0x80000000;

static const uint blend_source     = 0;
static const uint blend_multiply   = 1;
static const uint blend_overlay    = 2;
static const uint blend_lerp       = 3;
static const uint blend_add        = 4;
static const uint blend_subtract   = 5;
static const uint blend_difference = 6;
static const uint blend_divide     = 7;
static const uint blend_min        = 8;
static const uint blend_max        = 9;

typedef float4 blend_parameters_t[1];

struct dispatch_blend
{
    uint m_blend;

    float4 m_weights;

    void initialize(uint blend, blend_parameters_t parameters)
    {
        m_blend   = blend;
        m_weights = parameters[0];
    }

    float4 blend(float4 dest, float4 src)
    {
        if ((m_blend & blend_swap_inputs) != 0)
        {
            float4 temp = src;
            src = dest;
            dest = temp;
        }

        if ((m_blend & blend_invert_src) != 0)
            src = 1.0f - src;

        if ((m_blend & blend_invert_dest) != 0)
            dest = 1.0f - dest;

        float4 ret;
        switch (m_blend & blend_function_mask)
        {
        case blend_source:
            ret = src;
            break;
        case blend_multiply:
            ret = dest * src;
            break;
        case blend_overlay:
            {
                float4 dark  = 2.0f * dest * src;
                float4 light = 1.0f - 2.0f * (1.0f - dest) * (1.0f - src);
                ret = lerp(dark, light, step(0.5f, dest));
            }
            break;
        case blend_lerp:
            ret = lerp(dest, src, m_weights);
            break;
        case blend_add:
            ret = dest + src;
            break;
        case blend_subtract:
            ret = dest - src;
            break;
        case blend_difference:
            ret = abs(dest - src);
            break;
        case blend_divide:
            ret = dest / max(src, 1e-5);
            break;
        case blend_min:
            ret = min(dest, src);
            break;
        case blend_max:
            ret = max(dest, src);
            break;
        default:
            ret = 0.0f;
            break;
        }

        if ((m_blend & blend_invert_ret) != 0)
            ret = 1.0f - ret;

        if ((m_blend & blend_hdr) == 0)
            ret = saturate(ret);

        return ret;
    }
};

#endif
