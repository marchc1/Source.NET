using Source.Common.ShaderAPI;
using Source.Common.ShaderLib;

using System.Numerics;

namespace Source.Common.MaterialSystem;


public struct ShaderParamInfo
{
	public string Name;
	public string Help;
	public ShaderParamType Type;
	public string? DefaultValue;
	public ShaderParamFlags Flags;
}


public interface IShader
{
	string? GetName();
	int GetFlags();
	int GetNumParams();
	ReadOnlySpan<char> GetParamName(int paramIndex);
	ReadOnlySpan<char> GetParamHelp(int paramIndex);
	ShaderParamType GetParamType(int paramIndex);
	ReadOnlySpan<char> GetParamDefault(int paramIndex);
	string? GetFallbackShader(IMaterialVar[] vars);
	void InitShaderParams(IMaterialVar[] vars, IShaderAPI shaderAPI, ReadOnlySpan<char> materialName);
	void InitShaderInstance(IMaterialVar[] shaderParams, IShaderAPI shaderAPI, IShaderInit shaderManager, ReadOnlySpan<char> materialName, ReadOnlySpan<char> textureGroupName);
	void DrawElements(IMaterialVar[] shaderParams, IShaderShadow? shadow, IShaderDynamicAPI? shaderAPI, VertexCompressionType none);
	bool IsTranslucent(IMaterialVar[]? shaderParams);
	bool NeedsPowerOfTwoFrameBufferTexture(IMaterialVar[]? shaderParams, bool checkSpecificToThisFrame);
	bool NeedsFullFrameBufferTexture(IMaterialVar[]? shaderParams, bool checkSpecificToThisFrame);
}

public interface IShaderInit
{
	public void LoadTexture(IMaterialVar textureVar, ReadOnlySpan<char> textureGroupName, int additionalCreationFlags = 0);
	public void LoadCubeMap(IMaterialVar[] parms, IMaterialVar textureVar, int additionalCreationFlags = 0);
	VertexShaderHandle LoadVertexShader(ReadOnlySpan<char> name, ReadOnlySpan<char> defines = default);
	PixelShaderHandle LoadPixelShader(ReadOnlySpan<char> name, ReadOnlySpan<char> defines = default);
}


public enum VertexCompressionType : uint
{
	Invalid = 0xFFFFFFFF,
	None = 0,
	On = 1
}

public struct ShaderViewport
{
	public int TopLeftX;
	public int TopLeftY;
	public int Width;
	public int Height;
	public float MinZ;
	public float MaxZ;

	public ShaderViewport() {

	}

	public ShaderViewport(int x, int y, int width, int height, float minZ = 0.0f, float maxZ = 1.0f) {
		TopLeftX = x;
		TopLeftY = y;
		Width = width;
		Height = height;
		MinZ = minZ;
		MaxZ = maxZ;
	}
}

public ref struct DynamicShaderIndex(IShaderDynamicAPI shaderAPI, ShaderType type)
{
	readonly IShaderDynamicAPI shaderAPI = shaderAPI;
	readonly ShaderType type = type;
	int index = 0;

	public void Set(ReadOnlySpan<char> name, int value) => index += value * shaderAPI.GetDynamicComboScale(type, name);
	public void Set(ReadOnlySpan<char> name, bool value) => Set(name, value ? 1 : 0);
	public readonly int GetIndex() => index;
}

public ref struct StaticShaderIndex(IShaderShadow shaderShadow, ShaderType type, ReadOnlySpan<char> fileName)
{
	readonly IShaderShadow shaderShadow = shaderShadow;
	readonly ShaderType type = type;
	readonly ReadOnlySpan<char> fileName = fileName;
	int index = 0;

	public void Set(ReadOnlySpan<char> name, int value) => index += value * shaderShadow.GetStaticComboScale(type, fileName, name);
	public void Set(ReadOnlySpan<char> name, bool value) => Set(name, value ? 1 : 0);
	public readonly int GetIndex() => index;
}

public interface IShaderDynamicAPI
{
	MaterialFogMode GetSceneFogMode();
	bool InFlashlightMode();
	void PushMatrix();
	void PopMatrix();
	IMesh GetDynamicMesh(IMaterial material, int nCurrentBoneCount, bool buffered, IMesh? vertexOverride, IMesh? indexOverride);
	bool InEditorMode();


	void BindVertexShader(in VertexShaderHandle vertexShader);
	void BindPixelShader(in PixelShaderHandle pixelShader);
	void SetVertexShaderIndex(int index);
	void SetPixelShaderIndex(int index);
	int GetDynamicComboScale(ShaderType type, ReadOnlySpan<char> name);

	int LocateShaderUniform(ReadOnlySpan<char> name);

	void SetShaderUniform(int uniform, int integer);
	void SetShaderUniform(int uniform, float fl);
	void SetShaderUniform(int uniform, ReadOnlySpan<float> flConsts);

	void MatrixMode(MaterialMatrixMode i);
	void LoadMatrix(in Matrix4x4 transposeTop);
	void LoadIdentity();
	int GetCurrentNumBones();
	GraphicsDriver GetDriver();
	nint GetCurrentProgram();

	void SetShaderUniform(IMaterialVar variable);
	void BindStandardTexture(Sampler sampler, StandardTextureId id);
	void SetVertexShaderConstant(int var, Span<float> vec);
	void SetPixelShaderConstant(int var, Span<float> vec);
	void SetVertexShaderStateAmbientLightCube();
	void GetMatrix(MaterialMatrixMode matrixMode, out Matrix4x4 dst);
	void CommitVertexShaderLighting();
	void GetLightState(out LightState state);
}

public struct LightState
{
	public int NumLights;
	public bool AmbientLight;
	public bool StaticLightVertex;
	public bool StaticLightTexel;
	public readonly int HasDynamicLight() => (AmbientLight || (NumLights > 0)) ? 1 : 0;
}
