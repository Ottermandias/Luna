#ifndef SYMBOLFILTER_HLSLI_INCLUDED
#define SYMBOLFILTER_HLSLI_INCLUDED

struct symbol_filter
{
    Texture2D m_tex;
    SamplerState m_samp;

    float2 m_size;
    float2 m_rcp_size;

    void initialize(Texture2D tex, SamplerState samp)
    {
        m_tex  = tex;
        m_samp = samp;
        tex.GetDimensions(m_size.x, m_size.y);
        m_rcp_size = rcp(m_size);
    }

    void parse_texel(in float2 uv, out float2 texel_uv, out float2 weights)
    {
        uv = frac(uv);
        float2 texel = uv * m_size;
        texel_uv = trunc(texel) * m_rcp_size;
        weights  = frac(texel);
    }

    void gather(in float2 texel_uv, out float4 red, out float4 green, out float4 blue, out float4 alpha)
    {
        red   = m_tex.GatherRed(m_samp,   texel_uv, 0);
        green = m_tex.GatherGreen(m_samp, texel_uv, 0);
        blue  = m_tex.GatherBlue(m_samp,  texel_uv, 0);
        alpha = m_tex.GatherAlpha(m_samp, texel_uv, 0);
    }

    float mix_gathered(float4 values, float2 uv)
    {
        float2 v_mix = lerp(values.wz, values.xy, uv.y);
        return lerp(v_mix.x, v_mix.y, uv.x);
    }

    float4 quantize_symbols(float4 raw)
    {
        return (round(raw * 15.0) + 0.5) / 15.0;
    }

    float4 sample(float2 uv)
    {
        float2 texel_uv;
        float2 weights;
        parse_texel(uv, texel_uv, weights);

        float4 red, green, blue, alpha;
        gather(texel_uv, red, green, blue, alpha);
        float4 symbols = quantize_symbols(red);
        if (weights.x >= 0.5) {
            symbols   = symbols.yxwz;
            red       = red    .yxwz;
            green     = green  .yxwz;
            blue      = blue   .yxwz;
            alpha     = alpha  .yxwz;
            weights.x = 1.0 - weights.x;
        }
        if (weights.y >= 0.5) {
            symbols   = symbols.wzyx;
            red       = red    .wzyx;
            green     = green  .wzyx;
            blue      = blue   .wzyx;
            alpha     = alpha  .wzyx;
            weights.y = 1.0 - weights.y;
        }

        float4 equality = float4(symbols.xyzw == symbols.wwww);
        uint selector = uint(dot(equality, float4(4.0, 8.0, 16.0, 0.0)))
            + (symbols.x == symbols.z ? 2u : 0u)
            + (weights.x + weights.y >= 0.5 ? 1u : 0u);
        //            XY Z
        uint lut = 0x00000C07u;

        if (0u != ((lut >> selector) & 1u)) {
            return float4(red.w, green.w, blue.w, alpha.w);
        }

        if (selector == 3u) {
            equality = float4(symbols.xyzw == symbols.zzzz);
        }

        return float4(
            mix_gathered(red   * equality, weights),
            mix_gathered(green * equality, weights),
            mix_gathered(blue  * equality, weights),
            mix_gathered(alpha * equality, weights)) / mix_gathered(equality, weights);
    }
};

#endif
