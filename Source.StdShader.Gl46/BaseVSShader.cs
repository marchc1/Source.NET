using Source.Common.Commands;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;
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

	public void LoadViewMatrixIntoVertexShaderConstant(int vertexReg) {
		ShaderAPI!.GetMatrix(MaterialMatrixMode.View, out Matrix4x4 mat);

		Matrix4x4 t = Matrix4x4.Transpose(mat);
		Span<float> rows = [t.M11, t.M12, t.M13, t.M14, t.M21, t.M22, t.M23, t.M24, t.M31, t.M32, t.M33, t.M34,];
		ShaderAPI!.SetVertexShaderConstant(vertexReg, rows);
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

	public static void ColorVarsToVector(int colorVar, int alphaVar, Span<float> color) {
		IMaterialVar[] shaderParams = Params!;

		color[0] = color[1] = color[2] = color[3] = 1.0f;
		if (colorVar != -1) {
			IMaterialVar pColorVar = shaderParams[colorVar];
			if (pColorVar.GetVarType() == MaterialVarType.Vector)
				pColorVar.GetVecValue(color.Slice(0, 3));
			else
				color[0] = color[1] = color[2] = pColorVar.GetFloatValue();
		}
		if (alphaVar != -1) {
			float alpha = shaderParams[alphaVar].GetFloatValue();
			color[3] = Math.Clamp(alpha, 0.0f, 1.0f);
		}
	}

	public void SetColorPixelShaderConstant(int nPixelReg, int colorVar, int alphaVar) {
		Span<float> color = stackalloc float[4];
		ColorVarsToVector(colorVar, alphaVar, color);
		ShaderAPI!.SetPixelShaderConstant(nPixelReg, color);
	}

#if DEBUG
	static readonly ConVar mat_envmaptintoverride = new("mat_envmaptintoverride", "-1", FCvar.None);
	static readonly ConVar mat_envmaptintscale = new("mat_envmaptintscale", "-1", FCvar.None);
#endif

	public void SetEnvMapTintPixelShaderDynamicState(int pixelReg, int tintVar, int alphaVar, bool convertFromGammaToLinear = false) {
		IMaterialVar[] shaderParams = Params!;
		MaterialSystem_Config config = Materials.GetCurrentConfigForVideoCard();

		Span<float> color = stackalloc float[4];
		color[0] = color[1] = color[2] = color[3] = 1.0f;
		if (config.ShowSpecular && config.Fullbright != 2) {
			IMaterialVar? pAlphaVar = alphaVar >= 0 ? shaderParams[alphaVar] : null;
			if (pAlphaVar != null)
				color[3] = pAlphaVar.GetFloatValue();

			IMaterialVar pTintVar = shaderParams[tintVar];
#if DEBUG
			pTintVar.GetVecValue(color[..3]);

			float envmapTintOverride = mat_envmaptintoverride.GetFloat();
			float envmapTintScaleOverride = mat_envmaptintscale.GetFloat();

			if (envmapTintOverride != -1.0f)
				color[0] = color[1] = color[2] = envmapTintOverride;
			if (envmapTintScaleOverride != -1.0f) {
				color[0] *= envmapTintScaleOverride;
				color[1] *= envmapTintScaleOverride;
				color[2] *= envmapTintScaleOverride;
			}

			if (convertFromGammaToLinear) {
				color[0] = color[0] > 1.0f ? color[0] : MathLib.GammaToLinear(color[0]);
				color[1] = color[1] > 1.0f ? color[1] : MathLib.GammaToLinear(color[1]);
				color[2] = color[2] > 1.0f ? color[2] : MathLib.GammaToLinear(color[2]);
			}
#else
			if (convertFromGammaToLinear)
				tintVar.GetLinearVecValue(color, 3);
			else
				tintVar.GetVecValue(color[..3]);
#endif
		}
		else {
			color[0] = color[1] = color[2] = color[3] = 0.0f;
		}
		ShaderAPI!.SetPixelShaderConstant(pixelReg, color);
	}

	public static void EnablePixelShaderOverbright(int reg, bool bEnable, bool divideByTwo) {
		float v;
		if (bEnable)
			v = divideByTwo ? IMaterialSystem.OVERBRIGHT / 2.0f : IMaterialSystem.OVERBRIGHT;
		else
			v = divideByTwo ? 1.0f / 2.0f : 1.0f;
		Span<float> val = [v, v, v, v];
		ShaderAPI!.SetPixelShaderConstant(reg, val);
	}

	public void SetModulationVertexShaderDynamicState() {
		Span<float> color = [1.0f, 1.0f, 1.0f, 1.0f];
		ComputeModulationColor(color);
		ShaderAPI!.SetVertexShaderConstant(VertexShaderConst.ModulationColor, color);
	}

	public void SetModulationPixelShaderDynamicState(int modulationVar) {
		Span<float> color = [1.0f, 1.0f, 1.0f, 1.0f];
		ComputeModulationColor(color);
		ShaderAPI!.SetPixelShaderConstant(modulationVar, color);
	}

	public void SetPixelShaderConstant(int pixelReg, int constantVar) {
		Assert(!IsSnapshotting());
		IMaterialVar[] shaderParams = Params!;
		if (shaderParams == null || constantVar == -1)
			return;

		IMaterialVar pPixelVar = shaderParams[constantVar];
		Assert(pPixelVar != null);

		Span<float> val = stackalloc float[4];
		if (pPixelVar.GetVarType() == MaterialVarType.Vector)
			pPixelVar.GetVecValue(val);
		else
			val[0] = val[1] = val[2] = val[3] = pPixelVar.GetFloatValue();
		ShaderAPI!.SetPixelShaderConstant(pixelReg, val);
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

		if (IsFlagSet(shaderParams, MaterialVarFlags.BaseAlphaEnvMapMask))
			ClearFlags(shaderParams, MaterialVarFlags.AlphaTest);

		if (detailVar >= 0 && shaderParams[detailVar].IsDefined())
			LoadTexture(detailVar);

		if (envmapVar >= 0 && shaderParams[envmapVar].IsDefined()) {
			if (!IsFlagSet(shaderParams, MaterialVarFlags.EnvMapSphere))
				LoadCubeMap(envmapVar);
			else
				LoadTexture(envmapVar);

			if (!HardwareConfig.SupportsCubeMaps())
				SetFlags(shaderParams, MaterialVarFlags.EnvMapSphere);

			if (envmapMaskVar >= 0 && shaderParams[envmapMaskVar].IsDefined())
				LoadTexture(envmapMaskVar);
		}
	}
}
