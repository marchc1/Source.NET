#version 460

in vec2 vs_TexCoord;
in vec4 vs_Color;

layout(std140, binding = 3) uniform source_pixel_sharedUBO {
    bool isAlphaTesting;
    int alphaTestFunc;
    float alphaTestRef;
};

const int VertexColor = 16;
const int VertexAlpha = 32;

uniform int flags;
uniform sampler2D basetexture;

out vec4 fragColor;

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

    fragColor.rgb = texelColor.rgb * vs_Color.rgb;
    fragColor.a = texelColor.a;

    // Gradient for testing
    //fragColor = vec4(vs_TexCoord.x, vs_TexCoord.y, 1.0, 1.0);
}