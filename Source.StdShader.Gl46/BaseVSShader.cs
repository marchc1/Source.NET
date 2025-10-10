using Source.Common.MaterialSystem;
using Source.Common.ShaderAPI;
using Source.Common.ShaderLib;

namespace Source.StdShader.Gl46;

public abstract class BaseVSShader : BaseShader
{
	public static bool IsTextureSet(int index, Span<IMaterialVar> parms) {
		return index != -1 && parms[index].GetTextureValue() != null;
	}
	public static bool IsBoolSet(int index, Span<IMaterialVar> parms) {
		return index != -1 && parms[index].GetIntValue() != 0;
	}
	public static int GetIntParam(int index, Span<IMaterialVar> parms, int defaultValue = 0) {
		return index != -1 ? parms[index].GetIntValue() : defaultValue;
	}
	public static float GetFloatParam(int index, Span<IMaterialVar> parms, float defaultValue = 0) {
		return index != -1 ? parms[index].GetFloatValue() : defaultValue;
	}
	public static void InitFloatParam(int index, Span<IMaterialVar> parms, float value) {
		if (index != -1 && !parms[index].IsDefined())
			parms[index].SetFloatValue(value);
	}
	public static void InitIntParam(int index, Span<IMaterialVar> parms, int value) {
		if (index != -1 && !parms[index].IsDefined())
			parms[index].SetIntValue(value);
	}

	internal void InitParamsUnlitGeneric(int baseTextureVar, int detailScaleVar, int envmapOptionalVar,
		int envmapVar, int envmapTintVar, int envmapMaskScaleVar, int detailBlendMode) {
		IMaterialVar[] shaderParams = Params!;

		SetFlags2(shaderParams, MaterialVarFlags2.SupportsHardwareSkinning);

		if (envmapTintVar >= 0 && !shaderParams[envmapTintVar].IsDefined()) {
			shaderParams[envmapTintVar].SetVecValue(1.0f, 1.0f, 1.0f);
		}

		if (envmapMaskScaleVar >= 0 && !shaderParams[envmapMaskScaleVar].IsDefined()) {
			shaderParams[envmapMaskScaleVar].SetFloatValue(1.0f);
		}

		if (detailScaleVar >= 0 && !shaderParams[detailScaleVar].IsDefined()) {
			shaderParams[detailScaleVar].SetFloatValue(4.0f);
		}

		// No texture means no self-illum or env mask in base alpha
		if (baseTextureVar >= 0 && !shaderParams[baseTextureVar].IsDefined()) {
			ClearFlags(shaderParams, MaterialVarFlags.BaseAlphaEnvMapMask);
		}

		// If in decal mode, no debug override...
		if (IsFlagSet(shaderParams, MaterialVarFlags.Decal)) {
			SetFlags(shaderParams, MaterialVarFlags.NoDebugOverride);
		}

		// Get rid of the envmap if it's optional for this dx level.
		if (envmapOptionalVar >= 0 && shaderParams[envmapOptionalVar].IsDefined() && shaderParams[envmapOptionalVar].GetIntValue() != 0) {
			if (envmapVar >= 0) {
				shaderParams[envmapVar].SetUndefined();
			}
		}

		// If mat_specular 0, then get rid of envmap
		// TODO: what to do about the materialsystem_config_t type, which is what the "true" is right now as a placeholder
		if (envmapVar >= 0 && baseTextureVar >= 0 && true && shaderParams[envmapVar].IsDefined() && shaderParams[baseTextureVar].IsDefined()) {
			shaderParams[envmapVar].SetUndefined();
		}
	}

	VertexShaderHandle vsh;
	PixelShaderHandle psh;

	public void VertexShaderUnlitGenericPass(int baseTextureVar, int frameVar,
												  int baseTextureTransformVar,
												  int detailVar, int detailTransform,
												  bool bDetailTransformIsScale,
												  int envmapVar, int envMapFrameVar,
												  int envmapMaskVar, int envmapMaskFrameVar,
												  int envmapMaskScaleVar, int envmapTintVar,
												  int alphaTestReferenceVar,
												  int nDetailBlendModeVar,
												  int nOutlineVar,
												  int nOutlineColorVar,
												  int nOutlineStartVar,
												  int nOutlineEndVar,
												  int nSeparateDetailUVsVar,
												  ReadOnlySpan<char> shaderName) {
		IMaterialVar[] shaderParams = Params!;

		bool bBaseTexture = (baseTextureVar >= 0) && shaderParams[baseTextureVar].IsTexture();
		bool bVertexColor = IsFlagSet(Params, MaterialVarFlags.VertexColor);
		if (IsSnapshotting()) {
			ShaderShadow.EnableAlphaTest(IsFlagSet(shaderParams, MaterialVarFlags.AlphaTest));
			if (alphaTestReferenceVar != -1 && shaderParams[alphaTestReferenceVar].GetFloatValue() > 0.0f)
				ShaderShadow.AlphaFunc(ShaderAlphaFunc.GreaterEqual, shaderParams[alphaTestReferenceVar].GetFloatValue());

			if (bBaseTexture)
				ShaderShadow.EnableTexture(Sampler.Sampler0, true);

			if (bBaseTexture)
				SetDefaultBlendingShadowState(baseTextureVar, true);

			ShaderShadow.SetVertexShader(shaderName);
			ShaderShadow.SetPixelShader(shaderName);

			ShaderShadow.VertexShaderVertexFormat(VertexFormat.Position | VertexFormat.Normal | VertexFormat.TexCoord2D_0 | (bVertexColor ? VertexFormat.Color : 0), 1, null, 0);
			SetStandardShaderUniforms();
			// DevMsg("UnlitGeneric snapshotted!\n");
		}
		else {
			if (bBaseTexture) {
				BindTexture(Sampler.Sampler0, baseTextureVar, frameVar);
			}
		}

		Draw();
	}

	private void SetStandardShaderUniforms() {
		for (int i = 0; i < StandardParams.Length; i++) {
			var v = Params![i];
			if (v.IsDefined() && 0 == (StandardParams[i].Flags & ShaderParamFlags.DoNotUpload))
				ShaderShadow!.SetShaderUniform(v);
		}
	}

	private void SetDefaultBlendingShadowState(int baseTextureVar, bool isBaseTexture) {
		if ((CurrentMaterialVarFlags() & (int)MaterialVarFlags.Additive) != 0)
			SetAdditiveBlendingShadowState(baseTextureVar, isBaseTexture); // TODO: additive
		else
			SetNormalBlendingShadowState(baseTextureVar, isBaseTexture);
	}

	private void SetAdditiveBlendingShadowState(int baseTextureVar, bool isBaseTexture) {
		Assert(IsSnapshotting());
		bool isTranslucent = false;

		isTranslucent |= (CurrentMaterialVarFlags() & (int)MaterialVarFlags.VertexAlpha) != 0;

		isTranslucent |= TextureIsTranslucent(baseTextureVar, isBaseTexture) && ((CurrentMaterialVarFlags() & (int)MaterialVarFlags.AlphaTest) == 0);

		if (isTranslucent)
			EnableAlphaBlending(ShaderBlendFactor.SrcAlpha, ShaderBlendFactor.One);
		else
			EnableAlphaBlending(ShaderBlendFactor.One, ShaderBlendFactor.One);
	}

	private void SetNormalBlendingShadowState(int textureVar, bool isBaseTexture) {
		Assert(IsSnapshotting());

		bool isTranslucent = (CurrentMaterialVarFlags() & (int)MaterialVarFlags.VertexAlpha) != 0;
		isTranslucent |= TextureIsTranslucent(textureVar, isBaseTexture) && (CurrentMaterialVarFlags() & (int)MaterialVarFlags.AlphaTest) == 0;

		if (isTranslucent) {
			EnableAlphaBlending(ShaderBlendFactor.SrcAlpha, ShaderBlendFactor.OneMinusSrcAlpha);
		}
		else {
			DisableAlphaBlending();
		}
	}

	protected void EnableAlphaBlending(ShaderBlendFactor srcFactor, ShaderBlendFactor dstFactor) {
		ShaderShadow!.EnableBlending(true);
		ShaderShadow!.BlendFunc(srcFactor, dstFactor);
		ShaderShadow!.EnableDepthWrites(false);
	}

	protected void DisableAlphaBlending() {
		ShaderShadow!.EnableBlending(false);
	}

	protected void BindTexture(Sampler sampler, int textureVarIdx, int frameVarIdx) {
		IMaterialVar textureVar = Params![textureVarIdx];
		IMaterialVar? frameVar = frameVarIdx != -1 ? Params[frameVarIdx] : null;
		var tex = textureVar.GetTextureValue()!;
		ShaderSystem.BindTexture(sampler, tex, frameVar?.GetIntValue() ?? 0);
		ShaderAPI!.SetShaderUniform(ShaderAPI!.LocateShaderUniform(textureVar.GetName()), (int)sampler);
	}

	protected void Draw(bool makeActualDrawCall = true) {
		if (IsSnapshotting())
			return;
		ShaderSystem.Draw(makeActualDrawCall);
	}

	public void InitUnlitGeneric(int baseTextureVar, int detailVar, int envmapVar, int envmapMaskVar) {
		IMaterialVar[] shaderParams = Params!;

		if (baseTextureVar >= 0 && shaderParams[baseTextureVar].IsDefined()) {
			LoadTexture(baseTextureVar);

			if (!shaderParams[baseTextureVar].GetTextureValue()!.IsTranslucent()) {
				if (IsFlagSet(shaderParams, MaterialVarFlags.BaseAlphaEnvMapMask))
					ClearFlags(shaderParams, MaterialVarFlags.BaseAlphaEnvMapMask);
			}
		}
	}
}
