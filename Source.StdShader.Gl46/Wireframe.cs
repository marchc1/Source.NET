using Source.Common.MaterialSystem;
using Source.Common.ShaderLib;

namespace Source.StdShader.Gl46;

public class Wireframe : BaseVSShader
{
	public static string HelpString = "Help for Wireframe";
	public static int Flags = 0;
	protected override void OnInitShaderParams(IMaterialVar[] vars, ReadOnlySpan<char> materialName) {
		InitParamsUnlitGeneric(-1, -1, -1, -1, -1, -1, -1);
		SetFlags(vars, MaterialVarFlags.NoDebugOverride);
		SetFlags(vars, MaterialVarFlags.NoFog);
		SetFlags(vars, MaterialVarFlags.Wireframe);
	}
	public override string? GetFallbackShader(IMaterialVar[] vars)
		=> null;
	protected override void OnInitShaderInstance(IMaterialVar[] vars, ReadOnlySpan<char> materialName)
		=> InitUnlitGeneric(-1, -1, -1, -1);
	protected override void OnDrawElements(IMaterialVar[] vars, IShaderDynamicAPI shaderAPI, VertexCompressionType vertexCompression)
		=> VertexShaderUnlitGenericPass(-1, -1, -1, -1, -1, true, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, "unlitgeneric");
}