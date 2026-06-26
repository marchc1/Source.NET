using Source.Common.MaterialSystem;
using Source.Common.ShaderLib;

namespace Source;

public enum ShaderRenderTarget : ShaderAPITextureHandle_t
{
	Backbuffer = -1,
	Depthbuffer = -1,
	None = -2
}

public static class ShaderDefines
{
	public const ShaderAPITextureHandle_t INVALID_SHADERAPI_TEXTURE_HANDLE = 0;


	public static bool IsFlagSet(Span<IMaterialVar> shaderParams, int flag) => (shaderParams[(int)ShaderMaterialVars.Flags].GetIntValue() & flag) != 0;
	public static bool IsFlagSet(Span<IMaterialVar> shaderParams, MaterialVarFlags flag) => IsFlagSet(shaderParams, (int)flag);
	public static bool IsFlag2Set(Span<IMaterialVar> shaderParams, int flag) => (shaderParams[(int)ShaderMaterialVars.Flags2].GetIntValue() & flag) != 0;
	public static bool IsFlag2Set(Span<IMaterialVar> shaderParams, MaterialVarFlags2 flag) => IsFlag2Set(shaderParams, (int)flag);

	public static void SetFlags(Span<IMaterialVar> shaderParams, int flag)
		=> shaderParams[(int)ShaderMaterialVars.Flags].SetIntValue(shaderParams[(int)ShaderMaterialVars.Flags].GetIntValue() | flag);
	public static void SetFlags(Span<IMaterialVar> shaderParams, MaterialVarFlags flag) => SetFlags(shaderParams, (int)flag);
	public static void SetFlags2(Span<IMaterialVar> shaderParams, int flag)
		=> shaderParams[(int)ShaderMaterialVars.Flags2].SetIntValue(shaderParams[(int)ShaderMaterialVars.Flags2].GetIntValue() | flag);
	public static void SetFlags2(Span<IMaterialVar> shaderParams, MaterialVarFlags2 flag) => SetFlags2(shaderParams, (int)flag);

	public static void ClearFlags(Span<IMaterialVar> shaderParams, int flag)
		=> shaderParams[(int)ShaderMaterialVars.Flags].SetIntValue(shaderParams[(int)ShaderMaterialVars.Flags].GetIntValue() & ~flag);
	public static void ClearFlags(Span<IMaterialVar> shaderParams, MaterialVarFlags flag) => ClearFlags(shaderParams, (int)flag);
	public static void ClearFlags2(Span<IMaterialVar> shaderParams, int flag)
		=> shaderParams[(int)ShaderMaterialVars.Flags2].SetIntValue(shaderParams[(int)ShaderMaterialVars.Flags2].GetIntValue() & ~flag);
	public static void ClearFlags2(Span<IMaterialVar> shaderParams, MaterialVarFlags2 flag) => ClearFlags2(shaderParams, (int)flag);
}
