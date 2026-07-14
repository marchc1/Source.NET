#ifndef COMMON_GL460_VS
#define COMMON_GL460_VS

#include "common_gl460.glsl"

vec3 AmbientLight(vec3 worldNormal)
{
    vec3 nSquared = worldNormal * worldNormal;
    ivec3 isNegative = ivec3(lessThan(worldNormal, vec3(0.0)));
    vec3 color;
    color  = nSquared.x * vs_const[VERTEX_SHADER_AMBIENT_LIGHT + isNegative.x].rgb;
    color += nSquared.y * vs_const[VERTEX_SHADER_AMBIENT_LIGHT + 2 + isNegative.y].rgb;
    color += nSquared.z * vs_const[VERTEX_SHADER_AMBIENT_LIGHT + 4 + isNegative.z].rgb;
    return color;
}

#endif // COMMON_GL460_VS
