using Source.Common.MaterialSystem;
using Source.Common.ShaderAPI;
using Source.Common.ShaderLib;

namespace Source.StdShader.Gl46;

public class VertexLitGeneric : BaseVSShader
{

	public static string HelpString = "Help for VertexLitGeneric";
	public static int Flags = 0;
	public static List<ShaderParam> ShaderParams = [];
	public static ShaderParam[] ShaderParamOverrides = new ShaderParam[(int)ShaderMaterialVars.Count];

	public class ShaderParam
	{
		public readonly ShaderParamInfo Info;
		public readonly int Index;
		public ShaderParam(ShaderMaterialVars var, ShaderParamType type, ReadOnlySpan<char> defaultParam, ReadOnlySpan<char> help, int flags) {
			Info.Name = "override";
			Info.Type = type;
			Info.DefaultValue = new(defaultParam);
			Info.Help = new(help);
			Info.Flags = (ShaderParamFlags)flags;

			if (ShaderParamOverrides[(int)var] == null) {

			}
			else {
				AssertMsg(false, "ShaderParamOverrides at var index had null value");
			}

			ShaderParamOverrides[(int)var] = this;
			Index = (int)var;
		}
		public ShaderParam(string name, ShaderParamType type, ReadOnlySpan<char> defaultParam, ReadOnlySpan<char> help, int flags = 0) {
			Info.Name = name;
			Info.Type = type;
			Info.DefaultValue = new(defaultParam);
			Info.Help = new(help);
			Info.Flags = (ShaderParamFlags)flags;
			Index = (int)ShaderMaterialVars.Count + ShaderParams.Count;
			ShaderParams.Add(this);
		}
		public static implicit operator int(ShaderParam param) => param.Index;
		public ReadOnlySpan<char> GetName() => Info.Name;
		public ShaderParamType GetType() => Info.Type;
		public ReadOnlySpan<char> GetDefaultValue() => Info.DefaultValue;
		public int GetFlags() => (int)Info.Flags;
		public ReadOnlySpan<char> GetHelp() => Info.Help;
	}

	public static readonly ShaderParam DETAIL = new($"${nameof(DETAIL)}", ShaderParamType.Texture, "shadertest/detail", "detail texture");
	public static readonly ShaderParam DETAILSCALE = new($"${nameof(DETAILSCALE)}", ShaderParamType.Float, "4", "scale of the detail texture");
	public static readonly ShaderParam DETAILFRAME= new($"${nameof(DETAILFRAME)}", ShaderParamType.Integer, "0", "frame number for $detail");
	public static readonly ShaderParam ENVMAP = new($"${nameof(ENVMAP)}", ShaderParamType.Texture, "shadertest/shadertest_env", "envmap");
	public static readonly ShaderParam ENVMAPFRAME = new($"${nameof(ENVMAPFRAME)}", ShaderParamType.Integer, "0", "");
	public static readonly ShaderParam ENVMAPMASK = new($"${nameof(ENVMAPMASK)}", ShaderParamType.Texture, "shadertest/shadertest_envmask", "envmap mask");
	public static readonly ShaderParam ENVMAPMASKFRAME = new($"${nameof(ENVMAPMASKFRAME)}", ShaderParamType.Integer, "0", "");
	public static readonly ShaderParam ENVMAPMASKSCALE = new($"${nameof(ENVMAPMASKSCALE)}", ShaderParamType.Float, "1", "envmap mask scale");
	public static readonly ShaderParam ENVMAPTINT = new($"${nameof(ENVMAPTINT)}", ShaderParamType.Color, "[1 1 1]", "envmap tint");
	public static readonly ShaderParam ENVMAPOPTIONAL = new($"${nameof(ENVMAPOPTIONAL)}", ShaderParamType.Bool, "0", "Make the envmap only apply to dx9 and higher hardware");
	public static readonly ShaderParam DETAILBLENDMODE = new($"${nameof(DETAILBLENDMODE)}", ShaderParamType.Integer, "0", "mode for combining detail texture with base. 0=normal, 1= additive, 2=alpha blend detail over base, 3=crossfade");
	public static readonly ShaderParam ALPHATESTREFERENCE = new($"${nameof(ALPHATESTREFERENCE)}", ShaderParamType.Float, "0.7", "");
	public static readonly ShaderParam OUTLINE = new($"${nameof(OUTLINE)}", ShaderParamType.Bool, "0", "Enable outline for distance coded textures.");
	public static readonly ShaderParam OUTLINECOLOR = new($"${nameof(OUTLINECOLOR)}", ShaderParamType.Color, "[1 1 1]", "color of outline for distance coded images.");
	public static readonly ShaderParam OUTLINESTART0 = new($"${nameof(OUTLINESTART0)}", ShaderParamType.Float, "0.0", "outer start value for outline");
	public static readonly ShaderParam OUTLINESTART1 = new($"${nameof(OUTLINESTART1)}", ShaderParamType.Float, "0.0", "inner start value for outline");
	public static readonly ShaderParam OUTLINEEND0 = new($"${nameof(OUTLINEEND0)}", ShaderParamType.Float, "0.0", "inner end value for outline");
	public static readonly ShaderParam OUTLINEEND1 = new($"${nameof(OUTLINEEND1)}", ShaderParamType.Float, "0.0", "outer end value for outline");
	public static readonly ShaderParam SEPARATEDETAILUVS = new($"${nameof(SEPARATEDETAILUVS)}", ShaderParamType.Integer, "0", "");


	protected override void OnInitShaderParams(IMaterialVar[] vars, ReadOnlySpan<char> materialName) {
		InitParamsUnlitGeneric((int)ShaderMaterialVars.BaseTexture, DETAILSCALE, ENVMAPOPTIONAL, ENVMAP, ENVMAPTINT, ENVMAPMASKSCALE, DETAILBLENDMODE);
	}

	public override string? GetFallbackShader(IMaterialVar[] vars) {
		return null;
	}
	public override int GetFlags() => Flags;
	public override int GetNumParams() => base.GetNumParams() + ShaderParams.Count;
	public override ReadOnlySpan<char> GetParamName(int paramIndex) {
		int baseClassParamCount = base.GetNumParams();
		if (paramIndex < baseClassParamCount)
			return base.GetParamName(paramIndex);
		else
			return ShaderParams[paramIndex - baseClassParamCount].GetName();
	}
	public override ReadOnlySpan<char> GetParamHelp(int paramIndex) {
		int baseClassParamCount = base.GetNumParams();
		if (paramIndex < baseClassParamCount)
			return base.GetParamHelp(paramIndex);
		else
			return ShaderParams[paramIndex - baseClassParamCount].GetHelp();
	}
	public override ShaderParamType GetParamType(int paramIndex) {
		int baseClassParamCount = base.GetNumParams();
		if (paramIndex < baseClassParamCount)
			return base.GetParamType(paramIndex);
		else
			return ShaderParams[paramIndex - baseClassParamCount].GetType();
	}
	public override ReadOnlySpan<char> GetParamDefault(int paramIndex) {
		int baseClassParamCount = base.GetNumParams();
		if (paramIndex < baseClassParamCount)
			return base.GetParamDefault(paramIndex);
		else
			return ShaderParams[paramIndex - baseClassParamCount].GetDefaultValue();
	}
	protected override void OnInitShaderInstance(IMaterialVar[] vars, ReadOnlySpan<char> materialName) {
		InitUnlitGeneric((int)ShaderMaterialVars.BaseTexture, DETAIL, ENVMAP, ENVMAPMASK);
	}
	protected override void OnDrawElements(IMaterialVar[] vars, IShaderDynamicAPI shaderAPI, VertexCompressionType vertexCompression) {
		DrawUnbumpedUsingVertexShader(vars, shaderAPI, ShaderShadow, false);
	}

	private void DrawUnbumpedUsingVertexShader(IMaterialVar[] vars, IShaderDynamicAPI shaderAPI, IShaderShadow? shaderShadow, bool skipEnvmap) {
		if (shaderShadow != null) {
			shaderShadow.EnableTexture(Sampler.Sampler0, true);
			shaderShadow.EnableAlphaTest(IsFlagSet(vars, MaterialVarFlags.AlphaTest));

			if (vars[ALPHATESTREFERENCE].GetFloatValue() > 0.0f)
				shaderShadow.AlphaFunc(ShaderAlphaFunc.GreaterEqual, vars[ALPHATESTREFERENCE].GetFloatValue());


			VertexFormat fmt = VertexFormat.Position | VertexFormat.Normal;

			if (IsFlagSet(vars, MaterialVarFlags.VertexColor) || IsFlagSet(vars, MaterialVarFlags.VertexAlpha))
				fmt |= VertexFormat.Color;

			if (vars[ENVMAP].IsTexture() && !skipEnvmap) {
				// envmap todo
			}

			if (vars[(int)ShaderMaterialVars.BaseTexture].IsTexture())
				SetDefaultBlendingShadowState((int)ShaderMaterialVars.BaseTexture, true);
			else
				SetDefaultBlendingShadowState(ENVMAPMASK, false);

			if (vars[DETAIL].IsTexture())
				shaderShadow.EnableTexture(Sampler.Sampler3, true);

			shaderShadow.VertexShaderVertexFormat(fmt, 1, null, 0);

			shaderShadow.SetVertexShader("vertexlitgeneric");
			shaderShadow.SetPixelShader("vertexlitgeneric");

			shaderShadow.EnableAlphaWrites(true);
		}
		if (shaderAPI != null) {
			if (vars[(int)ShaderMaterialVars.BaseTexture].IsTexture()) {
				BindTexture(Sampler.Sampler0, (int)ShaderMaterialVars.BaseTexture, (int)ShaderMaterialVars.Frame);
				// TODO: base texture transform...
			}

			if (vars[ENVMAP].IsTexture() && !skipEnvmap) {
				BindTexture(Sampler.Sampler1, ENVMAP, ENVMAPFRAME);

				if (vars[ENVMAPMASK].IsTexture() || IsFlagSet(vars, MaterialVarFlags.BaseAlphaEnvMapMask)) {
					if (vars[ENVMAPMASK].IsTexture())
						BindTexture(Sampler.Sampler2, ENVMAPMASK, ENVMAPMASKFRAME);
					else
						BindTexture(Sampler.Sampler2, (int)ShaderMaterialVars.BaseTexture, (int)ShaderMaterialVars.Frame);
				}

				if (IsFlagSet(vars, MaterialVarFlags.EnvMapSphere) || IsFlagSet(vars, MaterialVarFlags.EnvMapCameraSpace)) {
					// LoadViewMatrixIntoVertexShaderConstant(VERTEX_SHADER_VIEWMODEL);
				}

				// SetEnvMapTintPixelShaderDynamicState(2, ENVMAPTINT, -1);
			}

			if (vars[DETAIL].IsTexture()) 
				BindTexture(Sampler.Sampler3, DETAIL, DETAILFRAME);

			// TODO: Set skinning, etc
		}
		Draw();
	}
}
