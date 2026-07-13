#version 460
//	STATIC: "CUBEMAP"					"0..1"
//	STATIC: "ENVMAPMASK"				"0..1"
//	STATIC: "BASEALPHAENVMAPMASK"		"0..1"
//	STATIC: "NORMALMAPALPHAENVMAPMASK"	"0..1"
//	STATIC: "SELFILLUM"					"0..1"
//	STATIC: "VERTEXCOLOR"				"0..1"

in vec2 vs_TexCoord;
in vec4 vs_Color;
#if VERTEXCOLOR
in vec4 vs_VertexColor;
#endif
#if CUBEMAP
in vec3 vs_WorldNormal;
in vec3 vs_WorldVertToEye;
#endif

layout(std140, binding = 3) uniform source_pixel_sharedUBO {
    bool isAlphaTesting;
    int alphaTestFunc;
    float alphaTestRef;
};

layout(std140, binding = 6) uniform source_ps_constants {
    vec4 ps_const[256];
};

const int VertexColor = 16;
const int VertexAlpha = 32;
const int PIXEL_SHADER_SELFILLUM_TINT = 1;
const int PIXEL_SHADER_ENVMAP_TINT = 2;
const int PIXEL_SHADER_MODULATION = 3;
const int PIXEL_SHADER_ENVMAP_CONTRAST = 4;
const int PIXEL_SHADER_ENVMAP_SATURATION = 5;

uniform int flags;
uniform sampler2D basetexture;
#if CUBEMAP
uniform samplerCube envmap;
#if ENVMAPMASK
uniform sampler2D envmapmask;
#endif
#if NORMALMAPALPHAENVMAPMASK
uniform sampler2D bumpmap;
#endif
#endif

out vec4 fragColor;

vec3 GammaToLinear(vec3 gamma)
{
    return pow(gamma, vec3(2.2));
}

vec3 LinearToGamma(vec3 linear)
{
    return pow(linear, vec3(1.0 / 2.2));
}

void main()
{
    vec4 texelColor = texture(basetexture, vs_TexCoord);
    if(isAlphaTesting){
        switch(alphaTestFunc){
            case 1: if(texelColor.a >=  alphaTestRef){ discard; } break;
            case 2: if(texelColor.a != alphaTestRef){ discard; } break;
            case 3: if(texelColor.a > alphaTestRef){ discard; } break;
            case 4: if(texelColor.a <=  alphaTestRef){ discard; } break;
            case 5: if(texelColor.a == alphaTestRef){ discard; } break;
            case 6: if(texelColor.a < alphaTestRef){ discard; } break;
            case 7: discard; break;
        }
    }

    vec3 albedo = GammaToLinear(texelColor.rgb) * ps_const[PIXEL_SHADER_MODULATION].rgb;

#if VERTEXCOLOR
    albedo *= GammaToLinear(vs_VertexColor.rgb);
#endif

    vec3 linearColor = albedo * vs_Color.rgb;

#if SELFILLUM
    vec3 selfIllumComponent = albedo * ps_const[PIXEL_SHADER_SELFILLUM_TINT].rgb;
    linearColor = mix(linearColor, selfIllumComponent, texelColor.a);
#endif

#if CUBEMAP
    vec3 specularFactor = vec3(1.0);
#if ENVMAPMASK
    specularFactor *= texture(envmapmask, vs_TexCoord).rgb;
#endif
#if BASEALPHAENVMAPMASK
    specularFactor *= 1.0 - texelColor.a;
#endif
#if NORMALMAPALPHAENVMAPMASK
    specularFactor *= texture(bumpmap, vs_TexCoord).a;
#endif

    vec3 reflectVect = 2.0 * vs_WorldNormal * dot(vs_WorldNormal, vs_WorldVertToEye) - vs_WorldVertToEye * dot(vs_WorldNormal, vs_WorldNormal);
    vec3 specularLighting = GammaToLinear(texture(envmap, reflectVect).rgb);
    specularLighting *= specularFactor;
    specularLighting *= ps_const[PIXEL_SHADER_ENVMAP_TINT].rgb;
    vec3 specularLightingSquared = specularLighting * specularLighting;
    specularLighting = mix(specularLighting, specularLightingSquared, ps_const[PIXEL_SHADER_ENVMAP_CONTRAST].rgb);
    vec3 greyScale = vec3(dot(specularLighting, vec3(0.299, 0.587, 0.114)));
    specularLighting = mix(greyScale, specularLighting, ps_const[PIXEL_SHADER_ENVMAP_SATURATION].rgb);
    linearColor += specularLighting;
#endif

    fragColor.rgb = LinearToGamma(linearColor);
    fragColor.a = texelColor.a * ps_const[PIXEL_SHADER_MODULATION].a;

    // Gradient for testing
    //fragColor = vec4(vs_TexCoord.x, vs_TexCoord.y, 1.0, 1.0);
}