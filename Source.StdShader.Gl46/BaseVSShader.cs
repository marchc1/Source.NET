using Source.Common.MaterialSystem;
using Source.Common.ShaderAPI;
using Source.Common.ShaderLib;

using System.Numerics;

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

		bool baseAlphaEnvmapMask = IsFlagSet(Params, MaterialVarFlags.BaseAlphaEnvMapMask);
		bool envmap = envmapVar >= 0 && shaderParams[envmapVar].IsTexture();
		bool mask = false;
		if (envmap && envmapMaskVar >= 0)
			mask = shaderParams[envmapMaskVar].IsTexture();
		bool detail = detailVar >= 0 && shaderParams[detailVar].IsTexture();
		bool bBaseTexture = (baseTextureVar >= 0) && shaderParams[baseTextureVar].IsTexture();
		bool bVertexColor = IsFlagSet(Params, MaterialVarFlags.VertexColor);
		// bool envmapCameraSpace = IsFlagSet(Params, MaterialVarFlags.EnvMapCameraSpace);
		// bool envmapSpehere = IsFlagSet(Params, MaterialVarFlags.EnvMapSphere);
		// bool detailMultiply = nDetailBlendModeVar >= 0 && (shaderParams[nDetailBlendModeVar].GetIntValue() == 8);
		// bool maskBaseByDetailAlpha = nDetailBlendModeVar >= 0 && (shaderParams[nDetailBlendModeVar].GetIntValue() == 9);
		bool separateDetailUVs = nSeparateDetailUVsVar >= 0 && (shaderParams[nSeparateDetailUVsVar].GetIntValue() != 0);

		if (IsSnapshotting()) {
			ShaderShadow.EnableAlphaTest(IsFlagSet(shaderParams, MaterialVarFlags.AlphaTest));
			if (alphaTestReferenceVar != -1 && shaderParams[alphaTestReferenceVar].GetFloatValue() > 0.0f)
				ShaderShadow.AlphaFunc(ShaderAlphaFunc.GreaterEqual, shaderParams[alphaTestReferenceVar].GetFloatValue());

			if (bBaseTexture)
				ShaderShadow.EnableTexture(Sampler.Sampler0, true);

			if (detail)
				ShaderShadow.EnableTexture(Sampler.Sampler3, true);

			if (envmap) {
				ShaderShadow.EnableTexture(Sampler.Sampler1, true);

				if (mask || baseAlphaEnvmapMask)
					ShaderShadow.EnableTexture(Sampler.Sampler2, true);
			}

			if (bBaseTexture)
				SetDefaultBlendingShadowState(baseTextureVar, true);
			else if (mask)
				SetDefaultBlendingShadowState(baseTextureVar, false);
			else
				SetDefaultBlendingShadowState();

			int numTexCoords = 1;
			if (separateDetailUVs)
				numTexCoords = 2;

			ShaderShadow.SetVertexShader(shaderName);
			ShaderShadow.SetPixelShader(shaderName);

			ShaderShadow.VertexShaderVertexFormat(
				VertexFormat.Position | VertexFormat.Normal | VertexFormat.TexCoord2D_0
				| (bVertexColor ? VertexFormat.Color : 0)
			, numTexCoords, null, 0);
			SetStandardShaderUniforms();
			// DevMsg("UnlitGeneric snapshotted!\n");
		}
		else {
			if (bBaseTexture) {
				BindTexture(Sampler.Sampler0, baseTextureVar, frameVar);
			}

			if (ShaderAPI!.InFlashlightMode()) {
				Draw(false);
				return;
			}
		}

		Draw();
	}

	public void SetStandardShaderUniforms() {
		for (int i = 0; i < StandardParams.Length; i++) {
			var v = Params![i];
			if (v.IsDefined() && 0 == (StandardParams[i].Flags & ShaderParamFlags.DoNotUpload))
				ShaderShadow!.SetShaderUniform(v);
		}
	}

	protected void SetDefaultBlendingShadowState(int baseTextureVar = -1, bool isBaseTexture = true) {
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

	public void SetVertexShaderTextureTransform(int vertexReg, int transformVar) {
		IMaterialVar[] shaderParams = Params!;

		Span<float> transformation = stackalloc float[8];
		if (transformVar >= 0 && shaderParams[transformVar].GetVarType() == MaterialVarType.Matrix) {
			Matrix4x4 mat = shaderParams[transformVar].GetMatrixValue();
			transformation[0] = mat.M11; transformation[1] = mat.M12; transformation[2] = mat.M13; transformation[3] = mat.M14;
			transformation[4] = mat.M21; transformation[5] = mat.M22; transformation[6] = mat.M23; transformation[7] = mat.M24;
		}
		else {
			transformation[0] = 1.0f; transformation[1] = 0.0f; transformation[2] = 0.0f; transformation[3] = 0.0f;
			transformation[4] = 0.0f; transformation[5] = 1.0f; transformation[6] = 0.0f; transformation[7] = 0.0f;
		}

		ShaderAPI!.SetVertexShaderConstant(vertexReg, transformation);
	}

	public void SetVertexShaderTextureScaledTransform(int vertexReg, int transformVar, int scaleVar) {
		IMaterialVar[] shaderParams = Params!;

		Matrix4x4 mat = Matrix4x4.Identity;
		if (transformVar >= 0 && shaderParams[transformVar].GetVarType() == MaterialVarType.Matrix)
			mat = shaderParams[transformVar].GetMatrixValue();

		Span<float> transformation = [mat.M11, mat.M12, mat.M13, mat.M14, mat.M21, mat.M22, mat.M23, mat.M24];

		float scaleX = 1.0f, scaleY = 1.0f;
		if (scaleVar >= 0 && shaderParams[scaleVar].IsDefined()) {
			if (shaderParams[scaleVar].GetVarType() == MaterialVarType.Vector) {
				Span<float> scale = stackalloc float[2];
				shaderParams[scaleVar].GetVecValue(scale);
				scaleX = scale[0]; scaleY = scale[1];
			}
			else {
				scaleX = scaleY = shaderParams[scaleVar].GetFloatValue();
			}
		}

		transformation[0] *= scaleX;
		transformation[1] *= scaleY;
		transformation[4] *= scaleX;
		transformation[5] *= scaleY;
		transformation[3] *= scaleX;
		transformation[7] *= scaleY;

		ShaderAPI!.SetVertexShaderConstant(vertexReg, transformation);
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
