
using Source.Common.ShaderAPI;

namespace Source.Common.MaterialSystem;

public interface IShaderSystem
{
	public static bool IsTranslucent(IShaderShadow renderState) {
		return false; // todo
	}

	void BindTexture(Sampler sampler, ITexture texture, int frame = 0);
	void Draw(bool makeActualDrawCall = true);
	void Init();

	void LoadAllShaderDLLs();
	bool LoadShaderDLL<T>(T instance) where T : IShaderDLL;
	IShader? FindShader(ReadOnlySpan<char> shaderName);

	void InitShaderParameters(IShader shader, IMaterialVar[] vars, ReadOnlySpan<char> materialName, ReadOnlySpan<char> textureGroupName);
	void InitShaderInstance(IShader shader, IMaterialVar[]? shaderParams, ReadOnlySpan<char> materialName, ReadOnlySpan<char> textureGroupName);
	bool InitRenderState(IShader shader, IMaterialVar[] shaderParams, ref IShaderShadow shaderRenderState, ReadOnlySpan<char> materialName);
	void ResetShaderState();
	// void CleanupRenderState(ref ShaderRenderState renderState);
	void DrawElements(IShader shader, IMaterialVar[] parms, IShaderShadow renderState, VertexCompressionType vertexCompression, uint materialVarTimeStamp);
	IEnumerable<IShader> GetShaders();
	ReadOnlySpan<char> ShaderStateString(int i);
}
public interface IShaderDLL
{
	public IEnumerable<IShader> GetShaders();
}
