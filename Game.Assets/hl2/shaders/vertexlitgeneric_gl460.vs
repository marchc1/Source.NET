#version 460

layout(location = 0) in vec3 v_Position;
layout(location = 1) in vec3 v_Normal;
layout(location = 2) in vec4 v_Color;
layout(location = 7) in vec4 v_BoneIndex;
layout(location = 8) in vec2 v_BoneWeights;
layout(location = 9) in vec4 v_UserData;
layout(location = 10) in vec2 v_TexCoord;

layout(std140, binding = 0) uniform source_matrices {
    mat4 viewMatrix;
    mat4 projectionMatrix;
    mat4 modelMatrix;
};

layout(std140, binding = 4) uniform bone_matrices {
    mat4 bones[53];
};

out vec2 vs_TexCoord;
out vec4 vs_Color;

void main()
{
	mat4 mvp = projectionMatrix * viewMatrix * modelMatrix;

    gl_Position = mvp * vec4(v_Position, 1.0);
    vs_TexCoord = v_TexCoord;
    vs_Color    = v_Color;
}