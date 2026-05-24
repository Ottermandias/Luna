#ifndef LANCZOS3_HLSLI_INCLUDED
#define LANCZOS3_HLSLI_INCLUDED

// Ported from https://gitlab.com/higan/xml-shaders/blob/master/shaders/OpenGL/v1.0/Lanczos%20(6tap).shader

struct lanczos3
{
    Texture2D m_tex;
    SamplerState m_samp;

    float2 m_step;

    void initialize(Texture2D tex, SamplerState samp)
    {
        m_tex  = tex;
        m_samp = samp;
        float2 size;
        tex.GetDimensions(size.x, size.y);
        m_step = rcp(size);
    }

    float3 weights3(float x)
    {
        static const float pi = 3.1415926535897932384626433832795f;
        static const float radius = 3.0f;
        float3 texels = max(2.0f * pi * float3(x - 1.5f, x - 0.5f, x + 0.5f), 1e-5f);

        // Lanczos3. Note: we normalize outside this function, so no point in multiplying by radius.
        return /*radius **/ sin(texels) * sin(texels / radius) / (texels * texels);
    }

    float4 pixel_tap(float x, float y)
    {
        return m_tex.Sample(m_samp, float2(x, y));
    }

    float4 line_tap(float y, float3 x1, float3 x2, float3 weights1, float3 weights2)
    {
        return
            pixel_tap(x1.r, y) * weights1.r +
            pixel_tap(x1.g, y) * weights2.r +
            pixel_tap(x1.b, y) * weights1.g +
            pixel_tap(x2.r, y) * weights2.g +
            pixel_tap(x2.g, y) * weights1.b +
            pixel_tap(x2.b, y) * weights2.b;
    }

    float4 sample(float2 uv)
    {
        float2 pos = uv + m_step * 0.5f;
        float2 f = frac(pos / m_step);

        float3 line_weights1   = weights3(0.5f - f.x * 0.5f);
        float3 line_weights2   = weights3(1.0f - f.x * 0.5f);
        float3 column_weights1 = weights3(0.5f - f.y * 0.5f);
        float3 column_weights2 = weights3(1.0f - f.y * 0.5f);

        // Make sure all taps added together is exactly 1.0, otherwise some
        // (very small) distortion can occur.
        float sum_line   = dot(line_weights1, 1.0f) + dot(line_weights2, 1.0f);
        float sum_column = dot(column_weights1, 1.0f) + dot(column_weights2, 1.0f);
        line_weights1   /= sum_line;
        line_weights2   /= sum_line;
        column_weights1 /= sum_column;
        column_weights2 /= sum_column;

        float2 start_xy = (-2.5f - f) * m_step + pos;
        float3 x1 = float3(start_xy.x, start_xy.x + m_step.x, start_xy.x + m_step.x * 2.0f);
        float3 x2 = float3(start_xy.x + m_step.x * 3.0f, start_xy.x + m_step.x * 4.0f, start_xy.x + m_step.x * 5.0f);

        return float4(
            line_tap(start_xy.y                , x1, x2, line_weights1, line_weights2) * column_weights1.r +
            line_tap(start_xy.y + m_step.y       , x1, x2, line_weights1, line_weights2) * column_weights2.r +
            line_tap(start_xy.y + m_step.y * 2.0f, x1, x2, line_weights1, line_weights2) * column_weights1.g +
            line_tap(start_xy.y + m_step.y * 3.0f, x1, x2, line_weights1, line_weights2) * column_weights2.g +
            line_tap(start_xy.y + m_step.y * 4.0f, x1, x2, line_weights1, line_weights2) * column_weights1.b +
            line_tap(start_xy.y + m_step.y * 5.0f, x1, x2, line_weights1, line_weights2) * column_weights2.b);
    }
};

#endif
