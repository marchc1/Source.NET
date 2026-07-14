#ifndef COMMON_GL460_GLSL
#define COMMON_GL460_GLSL

vec3 LinearToGamma(vec3 f3linear)
{
    return pow(f3linear, vec3(1.0 / 2.2));
}

vec3 GammaToLinear(vec3 gamma)
{
    return pow(gamma, vec3(2.2));
}

#endif // COMMON_GL460_GLSL
