#ifndef COMMON_GL460_GLSL
#define COMMON_GL460_GLSL

#define OO_SQRT_3 0.57735025882720947
const vec3 bumpBasis[3] = vec3[3](
    vec3( 0.81649661064147949, 0.0, OO_SQRT_3 ),
    vec3( -0.40824833512306213, 0.70710676908493042, OO_SQRT_3 ),
    vec3( -0.40824821591377258, -0.7071068286895752, OO_SQRT_3 )
);
const vec3 bumpBasisTranspose[3] = vec3[3](
    vec3( 0.81649661064147949, -0.40824833512306213, -0.40824833512306213 ),
    vec3( 0.0, 0.70710676908493042, -0.7071068286895752 ),
    vec3( OO_SQRT_3, OO_SQRT_3, OO_SQRT_3 )
);

vec3 CalcReflectionVectorUnnormalized(vec3 normal, vec3 eyeVector)
{
    // FIXME: might be better of normalizing with a normalizing cube map and
    // get rid of the dot( normal, normal )
    // compute reflection vector r = 2 * ((n dot v)/(n dot n)) n - v
    //  multiply all values through by N.N.  uniformly scaling reflection vector won't affect result
    //  since it is used in a cubemap lookup
    return (2.0 * (dot(normal, eyeVector)) * normal) - (dot(normal, normal) * eyeVector);
}

vec3 Vec3TangentToWorld(vec3 iTangentVector, vec3 iWorldNormal, vec3 iWorldTangent, vec3 iWorldBinormal)
{
    vec3 vWorldVector;
    vWorldVector.xyz = iTangentVector.x * iWorldTangent.xyz;
    vWorldVector.xyz += iTangentVector.y * iWorldBinormal.xyz;
    vWorldVector.xyz += iTangentVector.z * iWorldNormal.xyz;
    return vWorldVector.xyz; // Return without normalizing
}

vec3 Vec3TangentToWorldNormalized(vec3 iTangentVector, vec3 iWorldNormal, vec3 iWorldTangent, vec3 iWorldBinormal)
{
    return normalize(Vec3TangentToWorld(iTangentVector, iWorldNormal, iWorldTangent, iWorldBinormal));
}

vec3 LinearToGamma(vec3 f3linear)
{
    return pow(f3linear, vec3(1.0 / 2.2));
}

vec3 GammaToLinear(vec3 gamma)
{
    return pow(gamma, vec3(2.2));
}

#ifndef AA_CLAMP
#define AA_CLAMP 0
#endif

#if (AA_CLAMP==1)
vec2 ComputeLightmapCoordinates(vec4 Lightmap1and2Coord, vec2 Lightmap3Coord)
{
    vec2 result = clamp(Lightmap1and2Coord.xy, 0.0, 1.0) * Lightmap1and2Coord.wz * 0.99;
    result += Lightmap3Coord;
    return result;
}

void ComputeBumpedLightmapCoordinates(vec4 Lightmap1and2Coord, vec2 Lightmap3Coord,
                                      out vec2 bumpCoord1,
                                      out vec2 bumpCoord2,
                                      out vec2 bumpCoord3)
{
    vec2 result = clamp(Lightmap1and2Coord.xy, 0.0, 1.0) * Lightmap1and2Coord.wz * 0.99;
    result += Lightmap3Coord;
    bumpCoord1 = result + vec2(Lightmap1and2Coord.z, 0);
    bumpCoord2 = result + 2.0 * vec2(Lightmap1and2Coord.z, 0);
    bumpCoord3 = result + 3.0 * vec2(Lightmap1and2Coord.z, 0);
}
#else
vec2 ComputeLightmapCoordinates(vec4 Lightmap1and2Coord, vec2 Lightmap3Coord)
{
    return Lightmap1and2Coord.xy;
}

void ComputeBumpedLightmapCoordinates(vec4 Lightmap1and2Coord, vec2 Lightmap3Coord,
                                      out vec2 bumpCoord1,
                                      out vec2 bumpCoord2,
                                      out vec2 bumpCoord3)
{
    bumpCoord1 = Lightmap1and2Coord.xy;
    bumpCoord2 = Lightmap1and2Coord.wz; // reversed order!!!
    bumpCoord3 = Lightmap3Coord.xy;
}
#endif

#endif // COMMON_GL460_GLSL
