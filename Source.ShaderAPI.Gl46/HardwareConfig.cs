using Source.Common.MaterialSystem;

namespace Source.ShaderAPI.Gl46;
public class HardwareConfig : IMaterialSystemHardwareConfig
{
	public bool ActuallySupportsPixelShaders_2_b() {
		throw new NotImplementedException();
	}

	public bool CanDoSRGBReadFromRTs() {
		throw new NotImplementedException();
	}

	public bool FakeSRGBWrite() {
		throw new NotImplementedException();
	}

	public int GetDXSupportLevel() {
		throw new NotImplementedException();
	}

	public int GetFrameBufferColorDepth() {
		throw new NotImplementedException();
	}

	public HDRType GetHardwareHDRType() {
		throw new NotImplementedException();
	}

	public bool GetHDREnabled() {
		throw new NotImplementedException();
	}

	public HDRType GetHDRType() {
		return HDRType.None;
	}

	public int GetMaxDXSupportLevel() {
		throw new NotImplementedException();
	}

	public int GetMaxVertexTextureDimension() {
		throw new NotImplementedException();
	}

	public unsafe int GetSamplerCount() {
		return 2;
	}

	public ReadOnlySpan<char> GetShaderDLLName() {
		throw new NotImplementedException();
	}

	public int GetShadowFilterMode() {
		throw new NotImplementedException();
	}

	public int GetTextureStageCount() {
		return GetSamplerCount();
	}

	public int GetVertexTextureCount() {
		throw new NotImplementedException();
	}

	public bool HasDestAlphaBuffer() {
		throw new NotImplementedException();
	}

	public bool HasFastVertexTextures() {
		throw new NotImplementedException();
	}

	public bool HasProjectedBumpEnv() {
		throw new NotImplementedException();
	}

	public bool HasSetDeviceGammaRamp() {
		throw new NotImplementedException();
	}

	public bool HasStencilBuffer() {
		throw new NotImplementedException();
	}

	public bool IsAAEnabled() {
		throw new NotImplementedException();
	}

	public int MaxBlendMatrices() {
		throw new NotImplementedException();
	}

	public int MaxBlendMatrixIndices() {
		throw new NotImplementedException();
	}

	public int MaxHWMorphBatchCount() {
		throw new NotImplementedException();
	}

	public unsafe int MaximumAnisotropicLevel() {
		float maxAniso;
		glGetFloatv(GL_MAX_TEXTURE_MAX_ANISOTROPY, &maxAniso);
		return (int)maxAniso;
	}

	public int MaxNumLights() {
		throw new NotImplementedException();
	}

	public int MaxTextureAspectRatio() {
		return int.MaxValue;
	}

	public unsafe int MaxTextureDepth() {
		int* maxTextureSize = stackalloc int[1];
		glGetIntegerv(GL_MAX_TEXTURE_SIZE, maxTextureSize);
		return *maxTextureSize;
	}

	public unsafe int MaxTextureHeight() {
		int* maxTextureSize = stackalloc int[1];
		glGetIntegerv(GL_MAX_TEXTURE_SIZE, maxTextureSize);
		return *maxTextureSize;
	}

	public unsafe int MaxTextureWidth() {
		int* maxTextureSize = stackalloc int[1];
		glGetIntegerv(GL_MAX_TEXTURE_SIZE, maxTextureSize);
		return *maxTextureSize;
	}

	public int MaxUserClipPlanes() {
		throw new NotImplementedException();
	}

	public int MaxVertexShaderBlendMatrices() {
		throw new NotImplementedException();
	}

	public int MaxViewports() {
		throw new NotImplementedException();
	}

	public bool NeedsAAClamp() {
		throw new NotImplementedException();
	}

	public bool NeedsATICentroidHack() {
		throw new NotImplementedException();
	}

	public int NeedsShaderSRGBConversion() {
		throw new NotImplementedException();
	}

	public int NumPixelShaderConstants() {
		throw new NotImplementedException();
	}

	public int NumVertexShaderConstants() {
		throw new NotImplementedException();
	}

	public void OverrideStreamOffsetSupport(bool bOverrideEnabled, bool bEnableSupport) {
		throw new NotImplementedException();
	}

	public bool PreferDynamicTextures() {
		throw new NotImplementedException();
	}

	public bool PreferReducedFillrate() {
		throw new NotImplementedException();
	}

	public bool ReadPixelsFromFrontBuffer() {
		throw new NotImplementedException();
	}

	public void SetHDREnabled(bool bEnable) {
		throw new NotImplementedException();
	}

	public bool SpecifiesFogColorInLinearSpace() {
		throw new NotImplementedException();
	}

	public int StencilBufferBits() {
		throw new NotImplementedException();
	}

	public bool SupportsBorderColor() {
		throw new NotImplementedException();
	}

	public bool SupportsColorOnSecondStream() {
		throw new NotImplementedException();
	}

	public bool SupportsCompressedTextures() {
		return true;
	}

	public VertexCompressionType SupportsCompressedVertices() {
		throw new NotImplementedException();
	}

	public bool SupportsCubeMaps() {
		throw new NotImplementedException();
	}

	public bool SupportsFetch4() {
		throw new NotImplementedException();
	}

	public bool SupportsGLMixedSizeTargets() {
		throw new NotImplementedException();
	}

	public bool SupportsHardwareLighting() {
		throw new NotImplementedException();
	}

	public bool SupportsHDR() {
		throw new NotImplementedException();
	}

	public bool SupportsHDRMode(HDRType nHDRMode) {
		throw new NotImplementedException();
	}

	public bool SupportsMipmappedCubemaps() {
		return false;
	}

	public bool SupportsNonPow2Textures() => true;

	public bool SupportsOverbright() {
		throw new NotImplementedException();
	}

	public bool SupportsPixelShaders_1_4() {
		return true;
	}

	public bool SupportsPixelShaders_2_0() {
		return true;
	}

	public bool SupportsPixelShaders_2_b() {
		return true;
	}

	public bool SupportsShaderModel_3_0() {
		return true;
	}

	public bool SupportsSpheremapping() {
		throw new NotImplementedException();
	}

	public bool SupportsSRGB() {
		throw new NotImplementedException();
	}

	public bool SupportsStaticControlFlow() {
		throw new NotImplementedException();
	}

	public bool SupportsStaticPlusDynamicLighting() {
		throw new NotImplementedException();
	}

	public bool SupportsStreamOffset() {
		throw new NotImplementedException();
	}

	public bool SupportsVertexAndPixelShaders() {
		throw new NotImplementedException();
	}

	public bool SupportsVertexShaders_2_0() {
		throw new NotImplementedException();
	}

	public nint TextureMemorySize() {
		throw new NotImplementedException();
	}

	public bool UseFastClipping() {
		throw new NotImplementedException();
	}

	public bool UsesSRGBCorrectBlending() {
		return false; // todo
	}
}
