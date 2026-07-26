#ifndef COMMON_GL460_FS
#define COMMON_GL460_FS

#include "common_gl460.glsl"

// texture combining modes for combining base and detail/basetexture2
#define TCOMBINE_RGB_EQUALS_BASE_x_DETAILx2 0				// original mode
#define TCOMBINE_RGB_ADDITIVE 1								// base.rgb+detail.rgb*fblend
#define TCOMBINE_DETAIL_OVER_BASE 2
#define TCOMBINE_FADE 3										// straight fade between base and detail.
#define TCOMBINE_BASE_OVER_DETAIL 4                         // use base alpha for blend over detail
#define TCOMBINE_RGB_ADDITIVE_SELFILLUM 5                   // add detail color post lighting
#define TCOMBINE_RGB_ADDITIVE_SELFILLUM_THRESHOLD_FADE 6
#define TCOMBINE_MOD2X_SELECT_TWO_PATTERNS 7				// use alpha channel of base to select between mod2x channels in r+a of detail
#define TCOMBINE_MULTIPLY 8
#define TCOMBINE_MASK_BASE_BY_DETAIL_ALPHA 9                // use alpha channel of detail to mask base
#define TCOMBINE_SSBUMP_BUMP 10								// use detail to modulate lighting as an ssbump
#define TCOMBINE_SSBUMP_NOBUMP 11					// detail is an ssbump but use it as an albedo. shader does the magic here - no user needs to specify mode 11

vec4 TextureCombine(vec4 baseColor, vec4 detailColor, int combine_mode, float fBlendFactor)
{
    if (combine_mode == TCOMBINE_MOD2X_SELECT_TWO_PATTERNS)
    {
        vec3 dc = vec3(mix(detailColor.r, detailColor.a, baseColor.a));
        baseColor.rgb *= mix(vec3(1, 1, 1), 2.0 * dc, fBlendFactor);
    }
    if (combine_mode == TCOMBINE_RGB_EQUALS_BASE_x_DETAILx2)
        baseColor.rgb *= mix(vec3(1, 1, 1), 2.0 * detailColor.rgb, fBlendFactor);
    if (combine_mode == TCOMBINE_RGB_ADDITIVE)
        baseColor.rgb += fBlendFactor * detailColor.rgb;
    if (combine_mode == TCOMBINE_DETAIL_OVER_BASE)
    {
        float fblend = fBlendFactor * detailColor.a;
        baseColor.rgb = mix(baseColor.rgb, detailColor.rgb, fblend);
    }
    if (combine_mode == TCOMBINE_FADE)
    {
        baseColor = mix(baseColor, detailColor, fBlendFactor);
    }
    if (combine_mode == TCOMBINE_BASE_OVER_DETAIL)
    {
        float fblend = fBlendFactor * (1.0 - baseColor.a);
        baseColor.rgb = mix(baseColor.rgb, detailColor.rgb, fblend);
        baseColor.a = detailColor.a;
    }
    if (combine_mode == TCOMBINE_MULTIPLY)
    {
        baseColor = mix(baseColor, baseColor * detailColor, fBlendFactor);
    }
    if (combine_mode == TCOMBINE_MASK_BASE_BY_DETAIL_ALPHA)
    {
        baseColor.a = mix(baseColor.a, baseColor.a * detailColor.a, fBlendFactor);
    }
    if (combine_mode == TCOMBINE_SSBUMP_NOBUMP)
    {
        baseColor.rgb = baseColor.rgb * dot(detailColor.rgb, vec3(2.0 / 3.0));
    }
    return baseColor;
}

vec3 lerp5(vec3 f1, vec3 f2, float i1, float i2, float x)
{
    return f1 + (f2 - f1) * (x - i1) / (i2 - i1);
}

vec3 TextureCombinePostLighting(vec3 lit_baseColor, vec4 detailColor, int combine_mode, float fBlendFactor)
{
    if (combine_mode == TCOMBINE_RGB_ADDITIVE_SELFILLUM)
        lit_baseColor += fBlendFactor * detailColor.rgb;
    if (combine_mode == TCOMBINE_RGB_ADDITIVE_SELFILLUM_THRESHOLD_FADE)
    {
        // fade in an unusual way - instead of fading out color, remap an increasing band of it from
        // 0..1
        float f = fBlendFactor - 0.5;
        float fMult = (f >= 0.0) ? 1.0 / fBlendFactor : 4.0 * fBlendFactor;
        float fAdd = (f >= 0.0) ? 1.0 - fMult : -0.5 * fMult;
        lit_baseColor += clamp(fMult * detailColor.rgb + fAdd, 0.0, 1.0);
    }
    return lit_baseColor;
}

#endif // COMMON_GL460_FS
