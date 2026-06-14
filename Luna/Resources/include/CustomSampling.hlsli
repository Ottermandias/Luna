#ifndef CUSTOMSAMPLING_HLSLI_INCLUDED
#define CUSTOMSAMPLING_HLSLI_INCLUDED

#include "Lanczos3.hlsli"
#include "SymbolFilter.hlsli"

static const uint filter_simple   = 0;
static const uint filter_lanczos3 = 1;
static const uint filter_symbol   = 2;

struct dispatch_filter
{
    uint m_filter;

    Texture2D m_tex;
    SamplerState m_samp;

    float2 m_size;
    float2 m_rcp_size;

    void initialize(uint filter, Texture2D tex, SamplerState samp)
    {
        m_filter = filter;
        m_tex    = tex;
        m_samp   = samp;
        tex.GetDimensions(m_size.x, m_size.y);
        m_rcp_size = rcp(m_size);
    }

    float4 sample(float2 uv)
    {
        switch (m_filter)
        {
        case filter_simple:
            return m_tex.Sample(m_samp, uv);
        case filter_lanczos3:
            {
                lanczos3 impl;
                impl.m_tex = m_tex;
                impl.m_samp = m_samp;
                impl.m_step = m_rcp_size;
                return impl.sample(uv);
            }
        case filter_symbol:
            {
                symbol_filter impl;
                impl.m_tex = m_tex;
                impl.m_samp = m_samp;
                impl.m_size = m_size;
                impl.m_rcp_size = m_rcp_size;
                return impl.sample(uv);
            }
        default:
            return 0.0f;
        }
    }
};

#endif
