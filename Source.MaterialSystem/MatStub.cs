using Source.Common;
using Source.Common.Bitmap;
using Source.Common.Formats.Keyvalues;
using Source.Common.Launcher;
using Source.Common.MaterialSystem;
using Source.Common.ShaderAPI;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.MaterialSystem;

public class DummyHardwareConfig : IMaterialSystemHardwareConfig
{
	public bool HasDestAlphaBuffer() => false;
	public bool HasStencilBuffer() => false;
	public int StencilBufferBits() => 0;	
	public int GetFrameBufferColorDepth() => 0;
	public int GetSamplerCount() => 0;
	public bool HasSetDeviceGammaRamp() => false;
	public bool SupportsCompressedTextures() => false;
	public VertexCompressionType SupportsCompressedVertices() => VertexCompressionType.None;
	public bool SupportsVertexAndPixelShaders() => false;
	public bool SupportsPixelShaders_1_4() => false;
	public bool SupportsPixelShaders_2_0() => false;
	public bool SupportsPixelShaders_2_b() => false;
	public bool ActuallySupportsPixelShaders_2_b() => false;
	public bool SupportsStaticControlFlow() => false;
	public bool SupportsVertexShaders_2_0() => false;
	public bool SupportsShaderModel_3_0() => false;
	public int MaximumAnisotropicLevel() => 1;
	public int MaxTextureWidth() => 0;
	public int MaxTextureHeight() => 0;
	public int MaxTextureDepth() => 0;
	public nint TextureMemorySize() => 0;
	public bool SupportsOverbright() => false;
	public bool SupportsCubeMaps() => false;
	public bool SupportsMipmappedCubemaps() => false;
	public bool SupportsNonPow2Textures() => false;

	// The number of texture stages represents the number of computations
	// we can do in the pixel pipeline, it is *not* related to the
	// simultaneous number of textures we can use
	public int GetTextureStageCount() => 0;
	public int NumVertexShaderConstants() => 0;
	public int NumBooleanVertexShaderConstants() => 0;
	public int NumIntegerVertexShaderConstants() => 0;
	public int NumPixelShaderConstants() => 0;
	public int MaxNumLights() => 0;
	public bool SupportsHardwareLighting() => false;
	public int MaxBlendMatrices() => 0;
	public int MaxBlendMatrixIndices() => 0;
	public int MaxTextureAspectRatio() => 0;
	public int MaxVertexShaderBlendMatrices() => 0;
	public int MaxUserClipPlanes() => 0;
	public bool UseFastClipping() => false;
	public bool UseFastZReject() => false;
	public bool PreferReducedFillrate() => false;

	// This here should be the major item looked at when checking for compat
	// from anywhere other than the material system	shaders
	public int GetDXSupportLevel() => 90;
	public ReadOnlySpan<char> GetShaderDLLName() => null;

	public bool ReadPixelsFromFrontBuffer() => false;

	// Are dx dynamic textures preferred?
	public bool PreferDynamicTextures() => false;

	public bool SupportsHDR() => false;
	public HDRType GetHDRType() => HDRType.None;
	public HDRType GetHardwareHDRType() => HDRType.None;

	public bool HasProjectedBumpEnv() => false;
	public bool SupportsSpheremapping() => false;
	public bool NeedsAAClamp() => false;
	public bool HasFastZReject() => false;
	public bool NeedsATICentroidHack() => false;
	public bool SupportsColorOnSecondStream() => false;
	public bool SupportsStaticPlusDynamicLighting() => false;
	public bool SupportsStreamOffset() => false;
	public int GetMaxDXSupportLevel() => 90;
	public bool SpecifiesFogColorInLinearSpace() => false;
	public bool SupportsSRGB() => false;
	public bool FakeSRGBWrite() => false;
	public bool CanDoSRGBReadFromRTs() => true;
	public bool SupportsGLMixedSizeTargets() => false;
	public bool IsAAEnabled() => false;
	public int GetVertexTextureCount() => 0;
	public int GetMaxVertexTextureDimension() => 0;
	public int MaxViewports() => 1;
	public void OverrideStreamOffsetSupport(bool bEnabled, bool bEnableSupport) { }
	public int GetShadowFilterMode() => 0;
	public int NeedsShaderSRGBConversion() => 0;
	public bool UsesSRGBCorrectBlending() => false;
	public bool HasFastVertexTextures() => false;
	public int MaxHWMorphBatchCount() => 0;
	public bool SupportsHDRMode(HDRType nMode) => false;
	public bool IsDX10Card() => false;
	public bool GetHDREnabled() => true;
	public void SetHDREnabled(bool bEnable) { }
	public bool SupportsBorderColor() => true;
	public bool SupportsFetch4() => false;
	public bool CanStretchRectFromTextures() => false;

	public static readonly DummyHardwareConfig g_DummyHardwareConfig = new();
}

public class DummyMaterialSystem : IMaterialSystemStub
{
	public event Action? Restore;

	public void AddModeChangeCallBack(Action func) {
		throw new NotImplementedException();
	}

	public short AllocateLightmap(int allocationWidth, int allocationHeight, Span<int> offsetIntoLightmapPage, IMaterial? material) {
		throw new NotImplementedException();
	}

	public short AllocateWhiteLightmap(IMaterial? material) {
		throw new NotImplementedException();
	}

	public void BeginFrame(double frameTime) {
		throw new NotImplementedException();
	}

	public void BeginLightmapAllocation() {
		throw new NotImplementedException();
	}

	public void BeginRenderTargetAllocation() {
		throw new NotImplementedException();
	}

	public void BeginUpdateLightmaps() {
		throw new NotImplementedException();
	}

	public bool CanUseEditorMaterials() {
		throw new NotImplementedException();
	}

	public IMaterial CreateMaterial(ReadOnlySpan<char> name, ReadOnlySpan<char> textureGroupName, KeyValues keyValues) {
		throw new NotImplementedException();
	}

	public IMaterial CreateMaterial(ReadOnlySpan<char> name, KeyValues keyValues) {
		throw new NotImplementedException();
	}

	public ITexture? CreateNamedRenderTargetTextureEx(ReadOnlySpan<char> rtName, int w, int h, RenderTargetSizeMode sizeMode, ImageFormat format, MaterialRenderTargetDepth depthMode, TextureFlags textureFlags, CreateRenderTargetFlags renderTargetFlags) {
		throw new NotImplementedException();
	}

	public ITexture CreateProceduralTexture(ReadOnlySpan<char> textureName, ReadOnlySpan<char> textureGroup, int wide, int tall, ImageFormat format, TextureFlags flags) {
		throw new NotImplementedException();
	}

	public void EndFrame() {
		throw new NotImplementedException();
	}

	public void EndLightmapAllocation() {
		throw new NotImplementedException();
	}

	public void EndRenderTargetAllocation() {
		throw new NotImplementedException();
	}

	public void EndUpdateLightmaps() {
		throw new NotImplementedException();
	}

	public IMaterial FindMaterial(ReadOnlySpan<char> filename, ReadOnlySpan<char> textureGroup, bool complain = false, ReadOnlySpan<char> complainPrefix = default) {
		throw new NotImplementedException();
	}

	public IMaterial? FindMaterialEx(ReadOnlySpan<char> materialName, ReadOnlySpan<char> textureGroupName, MaterialFindContext isOnAModel, bool complain = true, ReadOnlySpan<char> complainPrefix = default) {
		throw new NotImplementedException();
	}

	public IMaterial? FindProceduralMaterial(ReadOnlySpan<char> materialName, ReadOnlySpan<char> textureGroupName, KeyValues keyValues) {
		throw new NotImplementedException();
	}

	public ITexture FindTexture(ReadOnlySpan<char> textureName, ReadOnlySpan<char> textureGroupName, bool complain = true, int additionalCreationFlags = 0) {
		throw new NotImplementedException();
	}

	public void GetBackBufferDimensions(out int width, out int height) {
		throw new NotImplementedException();
	}

	public int GetCurrentAdapter() {
		throw new NotImplementedException();
	}

	public MaterialSystem_Config GetCurrentConfigForVideoCard() {
		throw new NotImplementedException();
	}

	public int GetDisplayAdapterCount() {
		throw new NotImplementedException();
	}

	public ITexture GetErrorTexture() {
		throw new NotImplementedException();
	}

	public void GetLightmapPageSize(int lightmap, out int width, out int height) {
		throw new NotImplementedException();
	}

	public IMaterialProxyFactory? GetMaterialProxyFactory() {
		throw new NotImplementedException();
	}

	public int GetNumSortIDs() {
		throw new NotImplementedException();
	}

	public IMatRenderContext GetRenderContext() {
		throw new NotImplementedException();
	}

	public IShaderUtil GetShaderUtil() {
		throw new NotImplementedException();
	}

	public void GetSortInfo(Span<MaterialSystem_SortInfo> materialSortInfoArray) {
		throw new NotImplementedException();
	}

	public void ModInit() {
		throw new NotImplementedException();
	}

	public void ModShutdown() {
		throw new NotImplementedException();
	}

	public bool OverrideConfig(MaterialSystem_Config config, bool forceUpdate) {
		throw new NotImplementedException();
	}

	public void RestoreShaderObjects(IServiceProvider services, int changeFlags) {
		throw new NotImplementedException();
	}

	public void SetMaterialProxyFactory(IMaterialProxyFactory? factory) {
		throw new NotImplementedException();
	}

	public bool SetMode(IWindow window, MaterialSystem_Config config) {
		throw new NotImplementedException();
	}

	public void SwapBuffers() {
		throw new NotImplementedException();
	}

	public bool UpdateConfig(bool forceUpdate) {
		throw new NotImplementedException();
	}

	public void UpdateLightmap(int lightmapPageID, Span<int> lightmapSize, Span<int> offsetIntoLightmapPage, Span<float> floatImage, Span<float> floatImageBump1, Span<float> floatImageBump2, Span<float> floatImageBump3) {
		throw new NotImplementedException();
	}
}
