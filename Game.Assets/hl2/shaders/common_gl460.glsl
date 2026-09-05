#ifndef COMMON_GL460_GLSL
#define COMMON_GL460_GLSL

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

#endif // COMMON_GL460_GLSL
