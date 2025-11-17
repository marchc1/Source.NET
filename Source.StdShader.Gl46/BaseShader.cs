using Source.Common.MaterialSystem;
using Source.Common.ShaderAPI;
using Source.Common.ShaderLib;

using System.Diagnostics.CodeAnalysis;

namespace Source.StdShader.Gl46;

public abstract class BaseShader : IShader
{
	readonly public IMaterialSystemHardwareConfig HardwareConfig = Singleton<IMaterialSystemHardwareConfig>();
	readonly public IShaderSystem ShaderSystem = Singleton<IShaderSystem>();

	internal static IMaterialVar[]? Params;
	internal static IShaderInit? ShaderInit;
	internal IShaderShadow? ShaderShadow;
	internal static IShaderDynamicAPI? ShaderAPI;
	internal static string? TextureGroupName;

	internal static ShaderParamInfo[] StandardParams = [
		new(){ Name = "$flags",                    Help = "flags",            Type = ShaderParamType.Integer,DefaultValue =  "0", Flags = ShaderParamFlags.NotEditable },
		new(){ Name = "$flags_defined",            Help = "flags_defined",    Type = ShaderParamType.Integer, DefaultValue = "0", Flags =ShaderParamFlags.NotEditable },
		new(){ Name = "$flags2",                   Help = "flags2",           Type = ShaderParamType.Integer, DefaultValue = "0", Flags =ShaderParamFlags.NotEditable },
		new(){ Name = "$flags_defined2",           Help = "flags2_defined",   Type = ShaderParamType.Integer, DefaultValue = "0", Flags =ShaderParamFlags.NotEditable },
		new(){ Name = "$color",                    Help = "color",            Type = ShaderParamType.Color,   DefaultValue = "[1 1 1]", Flags =0 },
		new(){ Name = "$alpha",                    Help = "alpha",            Type = ShaderParamType.Float,  DefaultValue =  "1.0", Flags =0 },
		new(){ Name = "$basetexture",              Help = "Base Texture with lighting built in", Type = ShaderParamType.Texture, DefaultValue = "shadertest/BaseTexture", Flags = ShaderParamFlags.DoNotUpload },
		new(){ Name = "$frame",                    Help = "Animation Frame", Type =  ShaderParamType.Integer,  DefaultValue ="0", Flags =0 },
		new(){ Name = "$basetexturetransform",     Help = "Base Texture Texcoord Transform",Type = ShaderParamType.Matrix, DefaultValue = "center .5 .5 scale 1 1 rotate 0 translate 0 0", Flags =0 },
		new(){ Name = "$flashlighttexture",        Help = "flashlight spotlight shape texture", Type = ShaderParamType.Texture, DefaultValue ="effects/flashlight001", Flags =ShaderParamFlags.NotEditable | ShaderParamFlags.DoNotUpload },
		new(){ Name = "$flashlighttextureframe",   Help = "Animation Frame for $flashlight",  Type = ShaderParamType.Integer,DefaultValue = "0", Flags =ShaderParamFlags.NotEditable },
		new(){ Name = "$color2",                   Help = "color2",           Type = ShaderParamType.Color,  DefaultValue =  "[1 1 1]",Flags = 0 },
		new(){ Name = "$srgbtint",                 Help = "tint value to be applied when running on new-style srgb parts",            Type = ShaderParamType.Color,   DefaultValue = "[1 1 1]", Flags =0 },
	];

	public string GetName() => GetType().Name;
	public virtual int GetFlags() => 0;

	public int CurrentMaterialVarFlags() {
		return Params![(int)ShaderMaterialVars.Flags].GetIntValue();
	}

	public bool IsWhite(int colorVar) {
		if (colorVar < 0)
			return true;

		if (!Params[colorVar].IsDefined())
			return true;

		Span<float> color = stackalloc float[3];
		Params[colorVar].GetVecValue(color);
		return color[0] >= 1.0f && color[1] >= 1.0f && color[2] >= 1.0f;
	}

	public virtual int GetNumParams() {
		return (int)ShaderMaterialVars.Count;
	}

	public virtual ReadOnlySpan<char> GetParamName(int paramIndex) {
		Dbg.Assert(paramIndex < (int)ShaderMaterialVars.Count);
		return StandardParams[paramIndex].Name;
	}

	public virtual ReadOnlySpan<char> GetParamHelp(int paramIndex) {
		Dbg.Assert(paramIndex < (int)ShaderMaterialVars.Count);
		return StandardParams[paramIndex].Help;
	}

	public virtual ShaderParamType GetParamType(int paramIndex) {
		Dbg.Assert(paramIndex < (int)ShaderMaterialVars.Count);
		return StandardParams[paramIndex].Type;
	}

	public virtual ReadOnlySpan<char> GetParamDefault(int paramIndex) {
		Dbg.Assert(paramIndex < (int)ShaderMaterialVars.Count);
		return StandardParams[paramIndex].DefaultValue;
	}

	public virtual string? GetFallbackShader(IMaterialVar[] vars) => null;

	public void InitShaderParams(IMaterialVar[] vars, IShaderAPI shaderAPI, ReadOnlySpan<char> materialName) {
		Assert(Params == null);
		Params = vars;
		ShaderAPI = shaderAPI;
		OnInitShaderParams(vars, materialName);
		ShaderAPI = null;
		Params = null;
	}

	protected virtual void OnInitShaderParams(IMaterialVar[] vars, ReadOnlySpan<char> materialName) {

	}

	protected virtual void OnInitShaderInstance(IMaterialVar[] vars, ReadOnlySpan<char> materialName) {

	}

	protected virtual void OnDrawElements(IMaterialVar[] vars, IShaderDynamicAPI shaderAPI, VertexCompressionType vertexCompression) {

	}

	public virtual bool IsTranslucent(IMaterialVar[]? parms) => IsFlagSet(parms, (int)MaterialVarFlags.Translucent);

	public void DrawElements(IMaterialVar[] vars, IShaderShadow? shadow, IShaderDynamicAPI? shaderAPI, VertexCompressionType vertexCompression) {
		Assert(Params == null);
		Params = vars;
		ShaderShadow = shadow;
		ShaderAPI = shaderAPI;

		if (IsSnapshotting())
			SetInitialShadowState();

		OnDrawElements(vars, shaderAPI, vertexCompression);

		Params = null;
		ShaderShadow = null;
		ShaderAPI = null;
		// MeshBuilder = null
	}

	private void SetInitialShadowState() {
		ShaderShadow!.SetDefaultState();
		MaterialVarFlags flags = (MaterialVarFlags)Params![(int)ShaderMaterialVars.Flags].GetIntValue();

		if ((flags & MaterialVarFlags.IgnoreZ) != 0) {
			ShaderShadow.EnableDepthTest(false);
			ShaderShadow.EnableDepthWrites(false);
		}

		if ((flags & MaterialVarFlags.ZNearer) != 0) {
			ShaderShadow.DepthFunc(ShaderDepthFunc.Nearer);
		}
	}

	[MemberNotNullWhen(true, nameof(ShaderShadow))]
	protected bool IsSnapshotting() => ShaderShadow != null;

	public bool TextureIsTranslucent(int textureVar = -1, bool isBaseTexture = true) {
		if (textureVar < 0)
			return false;

		IMaterialVar[] shaderParams = Params!;
		if (shaderParams[textureVar].GetVarType() == MaterialVarType.Texture) {
			if (!isBaseTexture) {
				return shaderParams[textureVar].GetTextureValue()!.IsTranslucent();
			}
			else {
				// Override translucency settings if this flag is set.
				if (IsFlagSet(shaderParams, MaterialVarFlags.OpaqueTexture))
					return false;

				if ((CurrentMaterialVarFlags() & (int)(MaterialVarFlags.SelfIllum | MaterialVarFlags.BaseAlphaEnvMapMask)) == 0) {
					if ((CurrentMaterialVarFlags() & (int)MaterialVarFlags.Translucent) != 0 ||
						(CurrentMaterialVarFlags() & (int)MaterialVarFlags.AlphaTest) != 0) {
						return shaderParams[textureVar].GetTextureValue()!.IsTranslucent();
					}
				}
			}
		}

		return false;
	}

	public virtual void InitShaderInstance(IMaterialVar[] shaderParams, IShaderAPI shaderAPI, IShaderInit shaderInit, ReadOnlySpan<char> materialName, ReadOnlySpan<char> textureGroupName) {
		Assert(Params == null);

		Params = shaderParams;
		ShaderAPI = shaderAPI;
		ShaderInit = shaderInit;
		TextureGroupName = new(textureGroupName);

		OnInitShaderInstance(shaderParams, materialName);

		Params = null;
		ShaderAPI = null;
		ShaderInit = null;
		TextureGroupName = null;
	}

	protected void LoadCubeMap(int envmapVar) {
		if (Params == null || envmapVar == -1)
			return;

		IMaterialVar nameVar = Params[envmapVar];
		if (nameVar != null && nameVar.IsDefined()) {
			// TODO: ShaderInit.LoadCubeMap(Params, nameVar, additionalCreationFlags);
		}
	}

	protected void LoadTexture(int textureVar, int additionalCreationFlags = 0) {
		if (Params == null || textureVar == -1)
			return;

		IMaterialVar nameVar = Params[textureVar];
		if (nameVar != null && nameVar.IsDefined()) {
			ShaderInit!.LoadTexture(nameVar, TextureGroupName, additionalCreationFlags);
		}
	}

	private void GetColorParameter(Span<IMaterialVar> parms, Span<float> colorOut) {
		Span<float> color2 = stackalloc float[3];
		parms[(int)ShaderMaterialVars.Color].GetVecValue(colorOut[..3]);
		parms[(int)ShaderMaterialVars.Color2].GetVecValue(color2[..3]);

		colorOut[0] *= color2[0];
		colorOut[1] *= color2[1];
		colorOut[2] *= color2[2];

		if (HardwareConfig.UsesSRGBCorrectBlending()) {
			Span<float> SRGBTint = stackalloc float[3];
			parms[(int)ShaderMaterialVars.SRGBTint].GetVecValue(SRGBTint[..3]);

			colorOut[0] *= SRGBTint[0];
			colorOut[1] *= SRGBTint[1];
			colorOut[2] *= SRGBTint[2];
		}

	}

	private float GetAlpha(Span<IMaterialVar> parms) {
		if (parms.IsEmpty)
			parms = Params;

		if (parms.IsEmpty)
			return 1.0f;

		if ((parms[(int)ShaderMaterialVars.Flags].GetIntValue() & (int)MaterialVarFlags.NoAlphaMod) != 0)
			return 1.0f;

		float alpha = parms[(int)ShaderMaterialVars.Alpha].GetFloatValue();
		return Math.Clamp(alpha, 0, 1);
	}
}
