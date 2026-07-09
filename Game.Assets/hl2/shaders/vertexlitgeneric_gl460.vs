#version 460

layout(location = 0) in vec3 v_Position;
layout(location = 1) in vec3 v_Normal;
layout(location = 2) in vec4 v_Color;
layout(location = 7) in ivec2 v_BoneIndex;
layout(location = 8) in vec2 v_BoneWeights;
layout(location = 9) in vec4 v_UserData;
layout(location = 10) in vec2 v_TexCoord;

layout(std140, binding = 0) uniform source_matrices {
    mat4 viewMatrix;
    mat4 projectionMatrix;
    mat4 modelMatrix;
};

layout(std140, binding = 2) uniform source_base_vertex {
    int numBones;
};

layout(std140, binding = 4) uniform source_bone_matrices {
    mat4 bones[256];
};

layout(std140, binding = 5) uniform source_vs_constants {
    vec4 vs_const[256];
};

const int VERTEX_SHADER_AMBIENT_LIGHT = 21;

out vec2 vs_TexCoord;
out vec4 vs_Color;

vec3 AmbientLight(vec3 worldNormal)
{
    vec3 nSquared = worldNormal * worldNormal;
    ivec3 isNegative = ivec3(lessThan(worldNormal, vec3(0.0)));
    vec3 color;
    color  = nSquared.x * vs_const[VERTEX_SHADER_AMBIENT_LIGHT + isNegative.x].rgb;
    color += nSquared.y * vs_const[VERTEX_SHADER_AMBIENT_LIGHT + 2 + isNegative.y].rgb;
    color += nSquared.z * vs_const[VERTEX_SHADER_AMBIENT_LIGHT + 4 + isNegative.z].rgb;
    return pow(color, vec3(1.0 / 2.2));
}

void main()
{
    vec4 localPos = vec4(0.0);
    vec3 worldNormal = vec3(0.0);
	mat4 mvp;
	
    if (numBones == 0) {
		mvp = projectionMatrix * viewMatrix * modelMatrix;
		gl_Position = mvp * vec4(v_Position, 1.0);
		worldNormal = mat3(modelMatrix) * v_Normal;
	}
	else{
		if (numBones >= 1) {
			localPos += (bones[v_BoneIndex.x] * vec4(v_Position, 1.0)) * v_BoneWeights.x;
			worldNormal += (mat3(bones[v_BoneIndex.x]) * v_Normal) * v_BoneWeights.x;
		}

		if (numBones >= 2) {
			localPos += (bones[v_BoneIndex.y] * vec4(v_Position, 1.0)) * v_BoneWeights.y;
			worldNormal += (mat3(bones[v_BoneIndex.y]) * v_Normal) * v_BoneWeights.y;
		}
		mvp = projectionMatrix * viewMatrix;
		gl_Position = mvp * localPos;
	}   

    vs_TexCoord = v_TexCoord;
    vs_Color    = vec4(AmbientLight(normalize(worldNormal)), 1.0);
}
