using Source.Common;
using Source.Common.Bitmap;
using Source.Common.Formats.Keyvalues;
using Source.Common.Launcher;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;
using Source.Common.ShaderAPI;

using System;
using System.Collections.Generic;
using System.Numerics;
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

public class DummyMesh : IMesh
{
	public void BeginCastBuffer(VertexFormat format) {
		throw new NotImplementedException();
	}

	public void BeginCastBuffer(MaterialIndexFormat format) {
		throw new NotImplementedException();
	}

	public void Draw(int firstIndex = -1, int indexCount = 0) {
		throw new NotImplementedException();
	}

	public void EndCastBuffer() {
		throw new NotImplementedException();
	}

	public int GetRoomRemaining() {
		throw new NotImplementedException();
	}

	public VertexFormat GetVertexFormat() {
		throw new NotImplementedException();
	}

	public int IndexCount() {
		throw new NotImplementedException();
	}

	public MaterialIndexFormat IndexFormat() {
		throw new NotImplementedException();
	}

	public bool IsDynamic() {
		throw new NotImplementedException();
	}

	public bool Lock(int vertexCount, bool append, ref VertexDesc desc) {
		throw new NotImplementedException();
	}

	public int Lock(bool readOnly, int firstIndex, int indexCount, ref IndexDesc desc) {
		throw new NotImplementedException();
	}

	public void LockMesh(int vertexCount, int indexCount, ref MeshDesc desc) {
		Lock(vertexCount);
		Lock(indexCount)
	}

	public void MarkAsDrawn() {
		throw new NotImplementedException();
	}

	public void ModifyBegin(int firstVertex, int vertexCount, int firstIndex, int indexCount, ref MeshDesc desc) {
		throw new NotImplementedException();
	}

	public void ModifyEnd(ref MeshDesc desc) {
		throw new NotImplementedException();
	}

	public void SetColorMesh(IMesh colorMesh, int vertexOffset) {
		throw new NotImplementedException();
	}

	public void SetPrimitiveType(MaterialPrimitiveType type) {
		throw new NotImplementedException();
	}

	public bool Unlock(int vertexCount, ref VertexDesc desc) {
		throw new NotImplementedException();
	}

	public bool Unlock(int writtenIndexCount, ref IndexDesc desc) {
		throw new NotImplementedException();
	}

	public void UnlockMesh(int vertexCount, int indexCount, ref MeshDesc desc) {
		throw new NotImplementedException();
	}

	public int VertexCount() {
		throw new NotImplementedException();
	}
}

public class DummyTexture : ITexture
{

}

public class DummyMaterial : IMaterial {

}

public class DummyMaterialSystem : IMaterialSystemStub, IShaderUtil, IMatRenderContext
{
	internal static readonly DummyMaterial g_DummyMaterial = new();
	internal static readonly DummyTexture g_DummyTexture = new();
	internal static DummyMesh g_DummyMesh = null!;
	public static DummyMesh GetDummyMesh() => (g_DummyMesh ??= new());

	public event Action? Restore;
	public void AddModeChangeCallBack(Action func) { }
	public short AllocateLightmap(int allocationWidth, int allocationHeight, Span<int> offsetIntoLightmapPage, IMaterial? material) => 0;
	public short AllocateWhiteLightmap(IMaterial? material) => 0;
	public void BeginFrame(double frameTime) { }
	public void BeginLightmapAllocation() { }
	public void BeginRender() { }
	public void BeginRenderTargetAllocation() { }
	public void BeginUpdateLightmaps() { }
	public void Bind(IMaterial material, object? proxyData) { }
	public void BindLightmap(Sampler sampler) { }
	public void BindLightmapPage(int lightmapPageID) { }
	public void BindStandardTexture(Sampler sampler, StandardTextureId id) { }
	public bool CanUseEditorMaterials() => false;
	public void ClearBuffers(bool clearColor, bool clearDepth, bool clearStencil = false) { }
	public void ClearColor3ub(byte r, byte g, byte b) { }
	public void ClearColor4ub(byte r, byte g, byte b, byte a) { }
	public float ComputePixelDiameterOfSphere(Vector3 origin, float radius) => 1;
	public float ComputePixelWidthOfSphere(Vector3 origin, float radius) => 1;
	public IMaterial CreateMaterial(ReadOnlySpan<char> name, ReadOnlySpan<char> textureGroupName, KeyValues keyValues) => g_DummyMaterial;
	public IMaterial CreateMaterial(ReadOnlySpan<char> name, KeyValues keyValues) => g_DummyMaterial;
	public ITexture? CreateNamedRenderTargetTextureEx(ReadOnlySpan<char> rtName, int w, int h, RenderTargetSizeMode sizeMode, ImageFormat format, MaterialRenderTargetDepth depthMode, TextureFlags textureFlags, CreateRenderTargetFlags renderTargetFlags) => g_DummyTexture;
	public ITexture CreateProceduralTexture(ReadOnlySpan<char> textureName, ReadOnlySpan<char> textureGroup, int wide, int tall, ImageFormat format, TextureFlags flags) => g_DummyTexture;
	public IMesh CreateStaticMesh(VertexFormat format, ReadOnlySpan<char> textureGroup, IMaterial? material) {
		throw new NotImplementedException();
	}
	public void DepthRange(double near, double far) { }
	public void EndFrame() { }
	public void EndLightmapAllocation() { }
	public void EndRender() { }
	public void EndRenderTargetAllocation() { }
	public void EndUpdateLightmaps() { }
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
	public void Flush(bool flushHardware) { }
	public void GetBackBufferDimensions(out int width, out int height) {
		throw new NotImplementedException();
	}
	public int GetCurrentAdapter() {
		throw new NotImplementedException();
	}
	public MaterialSystem_Config GetCurrentConfigForVideoCard() {
		throw new NotImplementedException();
	}
	public IMaterial? GetCurrentMaterial() {
		throw new NotImplementedException();
	}
	public int GetDisplayAdapterCount() {
		throw new NotImplementedException();
	}
	public IMesh GetDynamicMesh(bool buffered, IMesh? vertexOverride = null, IMesh? indexOverride = null, IMaterial? autoBind = null) {
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
	public int GetMaxIndicesToRender(IMaterial material) {
		throw new NotImplementedException();
	}
	public int GetMaxVerticesToRender(IMaterial material) {
		throw new NotImplementedException();
	}
	public int GetNumSortIDs() {
		throw new NotImplementedException();
	}
	public IMatRenderContext GetRenderContext() => this;
	public ITexture? GetRenderTarget() {
		throw new NotImplementedException();
	}
	public void GetRenderTargetDimensions(out int screenWidth, out int screenHeight) {
		throw new NotImplementedException();
	}
	public IShaderAPI GetShaderAPI() {
		throw new NotImplementedException();
	}
	public IShaderUtil GetShaderUtil() => this;
	public void GetSortInfo(Span<MaterialSystem_SortInfo> materialSortInfoArray) { }
	public void GetViewport(out int x, out int y, out int width, out int height) {
		throw new NotImplementedException();
	}
	public void GetWindowSize(out int w, out int h) {
		throw new NotImplementedException();
	}
	public void GetWorldSpaceCameraPosition(out Vector3 vecCameraPos) {
		throw new NotImplementedException();
	}
	public bool InFlashlightMode() => false;
	public void LoadBoneMatrix(int hardwareID, in Matrix3x4 matrix4x4) { }
	public void LoadIdentity() { }
	public void LoadMatrix(in Matrix3x4 matrix) { }
	public void LoadMatrix(in Matrix4x4 matrix) { }
	public void MatrixMode(MaterialMatrixMode mode) { }
	public void ModInit() { }
	public void ModShutdown() { }
	public bool OnDrawMesh(IMesh mesh, int firstIndex, int indexCount) => false;
	public bool OnFlushBufferedPrimitives() => false;
	public bool OnSetPrimitiveType(IMesh mesh, MaterialPrimitiveType type) => false;
	public void Ortho(double left, double top, double right, double bottom, double near, double far) {}
	public bool OverrideConfig(MaterialSystem_Config config, bool forceUpdate) => false;
	public void PopMatrix() { }
	public void PopRenderTargetAndViewport(){ }
	public void PushMatrix() { }
	public void PushRenderTargetAndViewport(ITexture? thisTexture) { }
	public void PushRenderTargetAndViewport(ITexture? renderTarget, int x, int y, int width, int height) { }
	public void PushRenderTargetAndViewport(ITexture? renderTarget, ITexture? depthTarget, int x, int y, int width, int height) { }
	public void RestoreShaderObjects(IServiceProvider services, int changeFlags) { }
	public void Scale(float x, float y, float z) { }
	public void SetMaterialProxyFactory(IMaterialProxyFactory? factory) { }
	public bool SetMode(IWindow window, MaterialSystem_Config config) => true;
	public void SetNumBoneWeights(int v) { }
	public void SwapBuffers() { }
	public void SyncMatrices() { }
	public void SyncMatrix(MaterialMatrixMode mode) { }
	public bool UpdateConfig(bool forceUpdate) => false;
	public void UpdateLightmap(int lightmapPageID, Span<int> lightmapSize, Span<int> offsetIntoLightmapPage, Span<float> floatImage, Span<float> floatImageBump1, Span<float> floatImageBump2, Span<float> floatImageBump3) {}
	public void Viewport(int x, int y, int width, int height) {}
}
