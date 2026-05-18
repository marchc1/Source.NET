using Source.Common.MaterialSystem;
using Source.Common.ShaderAPI;
using Source.Common.ShaderLib;

using System.Diagnostics;

namespace Source.StdShader.Gl46;

public class LightmappedGeneric : BaseVSShader
{

	public static string HelpString = "Help for LightmappedGeneric";
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

	public static readonly ShaderParam SELFILLUMTINT = new($"${nameof(SELFILLUMTINT)}", ShaderParamType.Color, "[1 1 1]", "Slef-illumunation tint");
	public static readonly ShaderParam DETAIL = new($"${nameof(DETAIL)}", ShaderParamType.Texture, "shadertest/detail", "detail texture");
	public static readonly ShaderParam DETAILSCALE = new($"${nameof(DETAILSCALE)}", ShaderParamType.Float, "4", "scale of the detail texture");
	public static readonly ShaderParam DETAILBLENDFACTOR = new($"${nameof(DETAILBLENDFACTOR)}", ShaderParamType.Float, "1", "amount of detail texture to apply");
	public static readonly ShaderParam ENVMAP = new($"${nameof(ENVMAP)}", ShaderParamType.Texture, "shadertest/shadertest_env", "envmap");
	public static readonly ShaderParam ENVMAPFRAME = new($"${nameof(ENVMAPFRAME)}", ShaderParamType.Integer, "0", "");
	public static readonly ShaderParam ENVMAPMASK = new($"${nameof(ENVMAPMASK)}", ShaderParamType.Texture, "shadertest/shadertest_envmask", "envmap mask");
	public static readonly ShaderParam ENVMAPMASKFRAME = new($"${nameof(ENVMAPMASKFRAME)}", ShaderParamType.Integer, "0", "");
	public static readonly ShaderParam ENVMAPMASKSCALE = new($"${nameof(ENVMAPMASKSCALE)}", ShaderParamType.Float, "1", "envmap mask scale");
	public static readonly ShaderParam ENVMAPTINT = new($"${nameof(ENVMAPTINT)}", ShaderParamType.Color, "[1 1 1]", "envmap tint");
	public static readonly ShaderParam BUMPMAP = new($"${nameof(BUMPMAP)}", ShaderParamType.Texture, "models/shadertest/shader1_normal", "bump map");
	public static readonly ShaderParam ENVMAPOPTIONAL = new($"${nameof(ENVMAPOPTIONAL)}", ShaderParamType.Bool, "0", "Make the envmap only apply to dx9 and higher hardware");
	public static readonly ShaderParam NODIFFUSEBUMPLIGHTING = new($"${nameof(NODIFFUSEBUMPLIGHTING)}", ShaderParamType.Bool, "0", "0 == Use diffuse bump lighting, 1 = No diffuse bump lighting");
	public static readonly ShaderParam FORCEBUMP = new($"${nameof(FORCEBUMP)}", ShaderParamType.Bool, "0", "0 == Do bumpmapping if the card says it can handle it. 1 == Always do bumpmapping.");
	public static readonly ShaderParam DETAILBLENDMODE = new($"${nameof(DETAILBLENDMODE)}", ShaderParamType.Integer, "0", "mode for combining detail texture with base. 0=normal, 1= additive, 2=alpha blend detail over base, 3=crossfade");
	public static readonly ShaderParam ALPHATESTREFERENCE = new($"${nameof(ALPHATESTREFERENCE)}", ShaderParamType.Float, "0.7", "");
	public static readonly ShaderParam SSBUMP = new($"${nameof(SSBUMP)}", ShaderParamType.Integer, "0", "whether or not to use alternate bumpmap format with height");
	public static readonly ShaderParam OUTLINE = new($"${nameof(OUTLINE)}", ShaderParamType.Bool, "0", "Enable outline for distance coded textures.");
	public static readonly ShaderParam OUTLINECOLOR = new($"${nameof(OUTLINECOLOR)}", ShaderParamType.Color, "[1 1 1]", "color of outline for distance coded images.");
	public static readonly ShaderParam OUTLINESTART0 = new($"${nameof(OUTLINESTART0)}", ShaderParamType.Float, "0.0", "outer start value for outline");
	public static readonly ShaderParam OUTLINESTART1 = new($"${nameof(OUTLINESTART1)}", ShaderParamType.Float, "0.0", "inner start value for outline");
	public static readonly ShaderParam OUTLINEEND0 = new($"${nameof(OUTLINEEND0)}", ShaderParamType.Float, "0.0", "inner end value for outline");
	public static readonly ShaderParam OUTLINEEND1 = new($"${nameof(OUTLINEEND1)}", ShaderParamType.Float, "0.0", "outer end value for outline");
	public static readonly ShaderParam SEPARATEDETAILUVS = new($"${nameof(SEPARATEDETAILUVS)}", ShaderParamType.Integer, "0", "");
	public static readonly ShaderParam SEAMLESS_SCALE = new($"${nameof(SEAMLESS_SCALE)}", ShaderParamType.Float, "0", "Scale factor for 'seamless' texture mapping. 0 means to use ordinary mapping");

	protected override void OnInitShaderParams(IMaterialVar[] vars, ReadOnlySpan<char> materialName) {
		IMaterialVar[] shaderParams = Params!;

		InitParamsUnlitGeneric((int)ShaderMaterialVars.BaseTexture, DETAILSCALE, ENVMAPOPTIONAL, ENVMAP, ENVMAPTINT, ENVMAPMASKSCALE, DETAILBLENDMODE);

		// shaderParams[(int)ShaderMaterialVars.FlashLightTexture].SetStringValue("effects/flashlight001");

		SetFlags2(vars, MaterialVarFlags2.LightingLightmap);
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

		LoadTexture((int)ShaderMaterialVars.FlashLightTexture);

		// if (ShouldUseBumpmapping(Params))
		// 	LoadBumpMap(BUMPMAP);

		InitUnlitGeneric((int)ShaderMaterialVars.BaseTexture, DETAIL, ENVMAP, ENVMAPMASK);
		if (vars[(int)ShaderMaterialVars.BaseTexture].IsDefined()) {
			LoadTexture((int)ShaderMaterialVars.BaseTexture);
			if (!vars[(int)ShaderMaterialVars.BaseTexture].GetTextureValue()!.IsTranslucent()) {
				ClearFlags(vars, MaterialVarFlags.SelfIllum);
				ClearFlags(vars, MaterialVarFlags.BaseAlphaEnvMapMask);
			}
		}

		if (IsFlagSet(vars, MaterialVarFlags.SelfIllum) || IsFlagSet(vars, MaterialVarFlags.BaseAlphaEnvMapMask))
			ClearFlags(vars, MaterialVarFlags.AlphaTest);

		if (Params![ENVMAP].IsDefined()) {
			if (!IsFlagSet(Params!, MaterialVarFlags.EnvMapSphere))
				LoadCubeMap(ENVMAP);
			else
				LoadTexture(ENVMAP);

			if (!HardwareConfig.SupportsCubeMaps())
				SetFlags(Params!, MaterialVarFlags.EnvMapSphere);

			if (Params![ENVMAPMASK].IsDefined())
				LoadTexture(ENVMAPMASK);
		}

		if (ShouldUseBumpmapping(Params))
			SetFlags2(Params!, MaterialVarFlags2.NeedsTangentSpaces);
	}
	protected override void OnDrawElements(IMaterialVar[] vars, IShaderDynamicAPI shaderAPI, VertexCompressionType vertexCompression) {
		bool hasFlashlight = false; // UsingFlashlight(Params!);
		bool bump = ShouldUseBumpmapping(Params!) && Params![BUMPMAP].IsTexture() && Params![NODIFFUSEBUMPLIGHTING].GetIntValue() == 0;
		bool ssBump = bump && Params![SSBUMP].GetIntValue() != 0;

		if (hasFlashlight) {
			// DrawFlashlight(Params, shaderAPI, shaderShadow, bump, BUMPMAP, BUMPFRAME, BUMPTRANSFORM,
			// 										FLASHLIGHTTEXTURE, FLASHLIGHTTEXTUREFRAME, true, false, 0, -1, -1);
		}
		else if (bump) {
			// DrawWorldBumpedUsingVertexShader(
			// 		BASETEXTURE, BASETEXTURETRANSFORM,
			// 		BUMPMAP, BUMPFRAME, BUMPTRANSFORM, ENVMAPMASK, ENVMAPMASKFRAME, ENVMAP,
			// 		ENVMAPFRAME, ENVMAPTINT, COLOR, ALPHA, ENVMAPCONTRAST, ENVMAPSATURATION, FRAME, FRESNELREFLECTION,
			// 		false, -1, -1, -1, ssBump);
		}
		else {
			bool bumpedEnvMap = ShouldUseBumpmapping(Params!) && Params![BUMPMAP].IsTexture() && Params![ENVMAP].IsTexture();


			if (!Params![DETAIL].IsTexture()) {
				if (Params![SEAMLESS_SCALE].GetFloatValue() != 0.0f)
					DrawUnbumpedSeamlessUsingVertexShader(Params, shaderAPI, ShaderShadow);
				else
					DrawUnbumpedUsingVertexShader(Params, shaderAPI, ShaderShadow, bumpedEnvMap);
			}
			else
				DrawDetailUsingVertexShader(vars, shaderAPI, ShaderShadow, bumpedEnvMap);
		}
	}

	private void DrawUnbumpedUsingVertexShader(IMaterialVar[] @params, IShaderDynamicAPI shaderAPI, IShaderShadow? shaderShadow, bool bumpedEnvMap) {
		IMaterialVar[] shaderParams = Params!;

		bool hasEnvmap = shaderParams[ENVMAP].IsTexture() && !bumpedEnvMap;
		bool hasBaseTexture = shaderParams[(int)ShaderMaterialVars.BaseTexture].IsTexture();
		bool hasVertexColor = IsFlagSet(shaderParams, MaterialVarFlags.VertexColor);
		bool hasEnvmapCameraSpace = IsFlagSet(shaderParams, MaterialVarFlags.EnvMapCameraSpace);
		bool hasEnvmapSphere = IsFlagSet(shaderParams, MaterialVarFlags.EnvMapSphere);

		if (hasEnvmap || hasBaseTexture || hasVertexColor || !bumpedEnvMap) {
			if (IsSnapshotting()) {
				ShaderShadow!.EnableAlphaTest(IsFlagSet(shaderParams, MaterialVarFlags.AlphaTest));
				if (shaderParams[ALPHATESTREFERENCE].GetFloatValue() > 0.0f)
					ShaderShadow!.AlphaFunc(ShaderAlphaFunc.GreaterEqual, shaderParams[ALPHATESTREFERENCE].GetFloatValue());

				if (shaderParams[(int)ShaderMaterialVars.BaseTexture].IsTexture())
					ShaderShadow!.EnableTexture(Sampler.Sampler0, true);

				ShaderShadow!.EnableTexture(Sampler.Sampler1, true);

				VertexFormat fmt = VertexFormat.Position;

				if (hasEnvmap) {
					fmt |= VertexFormat.Normal;

					ShaderShadow!.EnableTexture(Sampler.Sampler2, true);

					if (shaderParams[ENVMAPMASK].IsTexture() || IsFlagSet(shaderParams, MaterialVarFlags.BaseAlphaEnvMapMask))
						ShaderShadow!.EnableTexture(Sampler.Sampler3, true);
				}

				if (shaderParams[(int)ShaderMaterialVars.BaseTexture].IsTexture() || bumpedEnvMap)
					SetDefaultBlendingShadowState((int)ShaderMaterialVars.BaseTexture, true);
				else
					SetDefaultBlendingShadowState(ENVMAPMASK, true);

				if (IsFlagSet(shaderParams, MaterialVarFlags.VertexColor))
					fmt |= VertexFormat.Color;

				ShaderShadow!.VertexShaderVertexFormat(fmt | VertexFormat.TexCoord2D_0 | VertexFormat.TexCoord2D_1, 2, null, 0);

				ShaderShadow.SetVertexShader("lightmappedgeneric");
				ShaderShadow.SetPixelShader("lightmappedgeneric");

				// DefaultFog();
			}
			else {
				if (hasBaseTexture) {
					BindTexture(Sampler.Sampler0, (int)ShaderMaterialVars.BaseTexture, (int)ShaderMaterialVars.Frame);
					SetVertexShaderTextureTransform(VertexShaderConst.ShaderSpecificConst0, (int)ShaderMaterialVars.BaseTextureTransform);
				}

				shaderAPI!.BindStandardTexture(Sampler.Sampler1, StandardTextureId.Lightmap);

				if (hasEnvmap) {
					BindTexture(Sampler.Sampler2, ENVMAP, ENVMAPFRAME);

					if (shaderParams[ENVMAPMASK].IsTexture() || IsFlagSet(shaderParams, MaterialVarFlags.BaseAlphaEnvMapMask)) {
						if (shaderParams[ENVMAPMASK].IsTexture())
							BindTexture(Sampler.Sampler3, ENVMAPMASK, ENVMAPMASKFRAME);
						else
							BindTexture(Sampler.Sampler3, (int)ShaderMaterialVars.BaseTexture, (int)ShaderMaterialVars.Frame);

						SetVertexShaderTextureScaledTransform(VertexShaderConst.ShaderSpecificConst2, (int)ShaderMaterialVars.BaseTextureTransform, ENVMAPMASKSCALE);
					}

					if (IsFlagSet(shaderParams, MaterialVarFlags.EnvMapSphere) || IsFlagSet(shaderParams, MaterialVarFlags.EnvMapCameraSpace)) {
						LoadViewMatrixIntoVertexShaderConstant(VertexShaderConst.ViewModel);
					}
					SetEnvMapTintPixelShaderDynamicState(2, ENVMAPTINT, -1);
				}

				if (!hasEnvmap || hasBaseTexture || hasVertexColor)
					SetModulationVertexShaderDynamicState();

				EnablePixelShaderOverbright(0, true, true);
				SetPixelShaderConstant(1, SELFILLUMTINT);
			}
		}

		Draw();
	}

	private void DrawUnbumpedSeamlessUsingVertexShader(IMaterialVar[] @params, IShaderDynamicAPI shaderAPI, IShaderShadow? shaderShadow) {
		throw new NotImplementedException();
		if (IsSnapshotting()) {
			ShaderShadow!.EnableTexture(Sampler.Sampler0, true);
			ShaderShadow!.EnableTexture(Sampler.Sampler1, true);
			ShaderShadow!.EnableTexture(Sampler.Sampler2, true);
			ShaderShadow!.EnableTexture(Sampler.Sampler3, true);

			ShaderShadow!.VertexShaderVertexFormat(VertexFormat.Position, 2, null, 0);

			ShaderShadow.SetVertexShader("lightmappedgeneric");
			ShaderShadow.SetPixelShader("lightmappedgeneric");
		}
		else {

		}
		Draw();
	}

	private void DrawDetailUsingVertexShader(IMaterialVar[] vars, IShaderDynamicAPI shaderAPI, IShaderShadow? shaderShadow, bool bumpedEnvMap) {
		if (!Params![ENVMAP].IsTexture())
			DrawDetailNoEnvmap(vars, shaderAPI, shaderShadow, IsFlagSet(vars, MaterialVarFlags.SelfIllum));
		else {
			if (!Params![(int)ShaderMaterialVars.BaseTexture].IsTexture())
				DrawUnbumpedUsingVertexShader(Params, shaderAPI, shaderShadow, bumpedEnvMap);
			else
				DrawDetailMode1(Params, shaderAPI, shaderShadow, bumpedEnvMap);
		}
	}

	private void DrawDetailMode1(IMaterialVar[] vars, IShaderDynamicAPI shaderAPI, IShaderShadow? shaderShadow, bool bumpedEnvMap) {
		DrawDetailNoEnvmap(vars, shaderAPI, shaderShadow, bumpedEnvMap);

		if (!bumpedEnvMap) {
			DrawAdditiveEnvmap(vars, shaderAPI, shaderShadow);
		}
		else {
			// DrawWorldBumpedSpecularLighting(
			// 		BUMPMAP, ENVMAP, BUMPFRAME, ENVMAPFRAME,
			// 		ENVMAPTINT, ALPHA, ENVMAPCONTRAST, ENVMAPSATURATION,
			// 		BUMPTRANSFORM, FRESNELREFLECTION,
			// 		true);
		}
	}

	private void DrawAdditiveEnvmap(IMaterialVar[] vars, IShaderDynamicAPI shaderAPI, IShaderShadow? shaderShadow) {
		IMaterialVar[] shaderParams = Params!;

		// bool usingBaseTexture = shaderParams[(int)ShaderMaterialVars.BaseTexture].IsTexture();
		bool usingMask = shaderParams[ENVMAPMASK].IsTexture();
		// bool usingBaseAlphaEnvmapMask = IsFlagSet(shaderParams, MaterialVarFlags.BaseAlphaEnvMapMask);

		if (IsSnapshotting()) {
			ShaderShadow.EnableAlphaTest(false);
			ShaderShadow.EnableTexture(Sampler.Sampler0, false);
			ShaderShadow.EnableTexture(Sampler.Sampler1, false);
			ShaderShadow.EnableTexture(Sampler.Sampler2, true);

			if (shaderParams[ENVMAPMASK].IsTexture() || IsFlagSet(shaderParams, MaterialVarFlags.BaseAlphaEnvMapMask))
				ShaderShadow.EnableTexture(Sampler.Sampler3, true);

			if (shaderParams[(int)ShaderMaterialVars.BaseTexture].IsTexture())
				SetDefaultBlendingShadowState((int)ShaderMaterialVars.BaseTexture, true);
			else
				SetDefaultBlendingShadowState(ENVMAPMASK, true);

			VertexFormat fmt = VertexFormat.Position | VertexFormat.Normal | VertexFormat.TexCoord2D_0 | VertexFormat.TexCoord2D_1;
			ShaderShadow.VertexShaderVertexFormat(fmt, 2, null, 0);

			ShaderShadow.SetVertexShader("lightmappedgeneric");
		}
		else {
			BindTexture(Sampler.Sampler2, ENVMAP, ENVMAPFRAME);

			if (usingMask || IsFlagSet(shaderParams, MaterialVarFlags.BaseAlphaEnvMapMask)) {
				if (usingMask)
					BindTexture(Sampler.Sampler3, ENVMAPMASK, ENVMAPMASKFRAME);
				else
					BindTexture(Sampler.Sampler3, (int)ShaderMaterialVars.BaseTexture, (int)ShaderMaterialVars.Frame);

				SetVertexShaderTextureScaledTransform(VertexShaderConst.ShaderSpecificConst2, (int)ShaderMaterialVars.BaseTextureTransform, ENVMAPMASKSCALE);
			}

			SetPixelShaderConstant(2, ENVMAPTINT);

			if (IsFlagSet(shaderParams, MaterialVarFlags.EnvMapSphere) || IsFlagSet(shaderParams, MaterialVarFlags.EnvMapCameraSpace))
				LoadViewMatrixIntoVertexShaderConstant(VertexShaderConst.ViewModel);

			SetModulationVertexShaderDynamicState();

			shaderAPI!.SetShaderUniform(shaderAPI.LocateShaderUniform("lightmaptexture"), 1);
		}
	}

	private void DrawDetailNoEnvmap(IMaterialVar[] vars, IShaderDynamicAPI shaderAPI, IShaderShadow? shaderShadow, bool doSelfIllum) {
		if (IsSnapshotting()) {
			ShaderShadow.EnableAlphaTest(IsFlagSet(vars, MaterialVarFlags.AlphaTest));
			if (vars[(int)ShaderMaterialVars.BaseTexture].IsTexture())
				ShaderShadow.EnableTexture(Sampler.Sampler0, true);

			ShaderShadow.EnableTexture(Sampler.Sampler1, true); // Lightmap
			ShaderShadow.EnableTexture(Sampler.Sampler2, true); // Detail

			SetDefaultBlendingShadowState((int)ShaderMaterialVars.BaseTexture, true);
			ShaderShadow.VertexShaderVertexFormat(VertexFormat.Position | VertexFormat.Normal | VertexFormat.TexCoord2D_0 | VertexFormat.TexCoord2D_1, 2, null, 0);

			ShaderShadow.SetVertexShader("lightmappedgeneric");
			ShaderShadow.SetPixelShader("lightmappedgeneric");
		}
		else {
			if (vars[(int)ShaderMaterialVars.BaseTexture].IsTexture()) {
				BindTexture(Sampler.Sampler0, (int)ShaderMaterialVars.BaseTexture, (int)ShaderMaterialVars.Frame);
				SetVertexShaderTextureTransform(VertexShaderConst.ShaderSpecificConst0, (int)ShaderMaterialVars.BaseTextureTransform);
			}

			ShaderAPI!.BindStandardTexture(Sampler.Sampler1, StandardTextureId.Lightmap);

			BindTexture(Sampler.Sampler2, DETAIL, (int)ShaderMaterialVars.Frame);
			SetVertexShaderTextureScaledTransform(VertexShaderConst.ShaderSpecificConst4, (int)ShaderMaterialVars.BaseTextureTransform, DETAILSCALE);

			SetModulationVertexShaderDynamicState();
			EnablePixelShaderOverbright(0, true, true);

			if (doSelfIllum)
				SetPixelShaderConstant(1, SELFILLUMTINT);

			float detailBlendFactor = vars[DETAILBLENDFACTOR].GetFloatValue();
			Span<float> c2 = [detailBlendFactor, detailBlendFactor, detailBlendFactor, detailBlendFactor];
			ShaderAPI!.SetPixelShaderConstant(2, c2);

			shaderAPI.SetShaderUniform(shaderAPI.LocateShaderUniform("lightmaptexture"), 1);

			BindTexture(Sampler.Sampler2, DETAIL, (int)ShaderMaterialVars.Frame);
		}
		Draw();
	}

	bool ShouldUseBumpmapping(IMaterialVar[] vars) {
		// if (!Config.UseBumpmapping())
		// 	return false;

		if (!Params![BUMPMAP].IsDefined())
			return false;

		return Params![FORCEBUMP].GetIntValue() != 0;
	}
}
