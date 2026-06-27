using Source.Common;
using Source.Common.Bitmap;
using Source.Common.Formats.Keyvalues;
using Source.Common.Launcher;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;
using Source.Common.ShaderAPI;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
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
	static unsafe ushort* g_DummyIndices;
	static unsafe float* dummyFloat;
	static unsafe byte* dummyChar;
	static unsafe void allocCheckDummyBuffers() {
		if (g_DummyIndices == null) g_DummyIndices = (ushort*)Marshal.AllocHGlobal(6 * sizeof(ushort));
		if (dummyFloat == null) dummyFloat = (float*)Marshal.AllocHGlobal(32 * sizeof(float));
		if (dummyChar == null) dummyChar = (byte*)Marshal.AllocHGlobal(32);
	}

	public void BeginCastBuffer(VertexFormat format) { }
	public void BeginCastBuffer(MaterialIndexFormat format) { }
	public void Draw(int firstIndex = -1, int indexCount = 0) { }
	public void EndCastBuffer() { }
	public int GetRoomRemaining() => 1;
	public VertexFormat GetVertexFormat() => VertexFormat.Position;
	public int IndexCount() => 0;
	public MaterialIndexFormat IndexFormat() => MaterialIndexFormat.x16;
	public bool IsDynamic() => false;
	public unsafe bool Lock(int vertexCount, bool append, ref VertexDesc desc) {
		allocCheckDummyBuffers();
		memreset(ref desc);
		desc.Position = dummyFloat;
		desc.BoneWeight = dummyFloat;
		desc.BoneMatrixIndex = dummyChar;
		desc.Normal = dummyFloat;
		desc.Color = dummyChar;
		desc.Specular = dummyChar;
		for (int i = 0; i < IMesh.VERTEX_MAX_TEXTURE_COORDINATES; i++)
			desc.SetTexCoord(i, dummyFloat);
		desc.TangentS = dummyFloat;
		desc.TangentT = dummyFloat;
		desc.Wrinkle = dummyFloat;

		// user data
		desc.UserData = dummyFloat;
		desc.FirstVertex = 0;
		desc.OffsetVertex = 0;
		return true;
	}
	public int Lock(bool readOnly, int firstIndex, int indexCount, ref IndexDesc desc) => 0;
	public void LockMesh(int vertexCount, int indexCount, ref MeshDesc desc) {
		Lock(vertexCount, false, ref desc.Vertex);
		Lock(indexCount, false, ref desc.Index);
	}
	// FIXME: Make this work! Unsupported methods of IIndexBuffer
	unsafe bool Lock(int maxIndexCount, bool append, ref IndexDesc desc) {
		allocCheckDummyBuffers();
		desc.Indices = g_DummyIndices;
		desc.IndexSize = 0;
		desc.FirstIndex = 0;
		desc.OffsetIndex = 0;
		return true;
	}
	public void MarkAsDrawn() { }
	public unsafe void ModifyBegin(int firstVertex, int vertexCount, int firstIndex, int indexCount, ref MeshDesc desc) {
		allocCheckDummyBuffers();
		desc.Index.Indices = g_DummyIndices;
		desc.Index.IndexSize = 0;
		desc.Index.FirstIndex = 0;
		desc.Index.OffsetIndex = 0;
	}
	public void ModifyEnd(ref MeshDesc desc) { }
	public void SetColorMesh(IMesh colorMesh, int vertexOffset) { }
	public void SetPrimitiveType(MaterialPrimitiveType type) { }
	public bool Unlock(int vertexCount, ref VertexDesc desc) => false;
	public bool Unlock(int writtenIndexCount, ref IndexDesc desc) => false;
	public void UnlockMesh(int vertexCount, int indexCount, ref MeshDesc desc) { }
	public int VertexCount() => 0;
}

public class DummyTexture : ITexture
{
	public void Dispose() { }
	public void Download(Rectangle rect = default, int additionalCreationFlags = 0) { }
	public void ForceLODOverride(int numLodOverrideUpOrDown) { }
	public int GetActualDepth() => 1;
	public int GetActualHeight() => 512;
	public int GetActualWidth() => 512;
	public nint GetApproximateVidMemBytes() => 64;
	public int GetFlags() => 0;
	public ImageFormat GetImageFormat() => ImageFormat.RGBA8888;
	public void GetLowResColorSample(float s, float t, Span<float> color) { }
	public int GetMappingDepth() => 1;
	public int GetMappingHeight() => 512;
	public int GetMappingWidth() => 512;
	public ReadOnlySpan<char> GetName() => "DummyTexture";
	public NormalDecodeMode GetNormalDecodeMode() => NormalDecodeMode.None;
	public int GetNumAnimationFrames() => 0;
	public Span<byte> GetResourceData(uint type) => null;

	public void IncrementReferenceCount() { }
	public void DecrementReferenceCount() { }
	public void DeleteIfUnreferenced() { }

	public bool IsCubeMap() => false;
	public bool IsError() => false;
	public bool IsMipmapped() => false;
	public bool IsNormalMap() => false;
	public bool IsProcedural() => false;
	public bool IsRenderTarget() => false;
	public bool IsTranslucent() => false;
	public bool IsVolumeTexture() => false;
	public bool SaveToFile(ReadOnlySpan<char> fileName) => false;
	public void SetTextureRegenerator(ITextureRegenerator textureRegen) { }
	public void SwapContents(ITexture other) { }
}

public class DummyMaterialVar : IMaterialVar
{
	public override void CopyFrom(IMaterialVar materialVar) { }
	public override void GetFourCCValue(ulong type, out object? data) {
		data = null;
	}
	public override IMaterial? GetMaterialValue() => DummyMaterialSystem.g_DummyMaterial;
	public override Matrix4x4 GetMatrixValue() => DummyMaterialSystem.g_DummyMatrix;
	public override ReadOnlySpan<char> GetName() => "DummyMaterialVar";
	public override IMaterial GetOwningMaterial() => DummyMaterialSystem.g_DummyMaterial;
	public override string GetStringValue() => "";
	public override ITexture? GetTextureValue() => DummyMaterialSystem.g_DummyTexture;
	public override void GetVecValue(Span<float> color) {
		for (int i = 0; i < color.Length; i++)
			color[i] = 1;
	}
	public override bool IsDefined() => true;
	public override bool MatrixIsIdentity() => false;
	public override void SetFloatValue(float val) { }
	public override void SetFourCCValue(ulong type, object? data) { }
	public override void SetIntValue(int val) { }
	public override void SetMaterialValue(IMaterial? material) { }
	public override void SetMatrixValue(in Matrix4x4 matrix) { }
	public override void SetStringValue(ReadOnlySpan<char> val) { }
	public override void SetTextureValue(ITexture? texture) { }
	public override void SetUndefined() { }
	public override void SetValueAutodetectType(ReadOnlySpan<char> val) { }
	public override void SetVecComponentValue(float val, int component) { }
	public override void SetVecValue(ReadOnlySpan<float> val) { }
	public override void SetVecValue(float x, float y) { }
	public override void SetVecValue(float x, float y, float z) { }
	public override void SetVecValue(float x, float y, float z, float w) { }
	protected override float GetFloatValueInternal() => 1;
	protected override int GetIntValueInternal() => 1;
	static readonly float[] vecvalX3 = [1, 1, 1];
	static readonly float[] vecvalX4 = [1, 1, 1, 1];
	protected override Span<float> GetVecValueInternal() => vecvalX4;
	protected override void GetVecValueInternal(Span<float> val) {
		for (int i = 0; i < val.Length; i++)
			val[i] = 1;
	}
	protected override int VectorSizeInternal() => 3;
}

public class DummyMaterial : IMaterial
{
	public void CallBindProxy(object? clientEntity) { }
	public void DecrementReferenceCount() { }
	public void DeleteIfUnreferenced() { }
	public IMaterialVar FindVar(ReadOnlySpan<char> varName, out bool found, bool complain = true) {
		found = true;
		return DummyMaterialSystem.g_DummyMaterialVar;
	}
	public IMaterialVar? FindVarFast(ReadOnlySpan<char> name, ref TokenCache lightmapVarCache) => null;
	public int GetEnumerationID() => 0;
	public float GetMappingHeight() => 512;
	public float GetMappingWidth() => 512;
	public IMaterial GetMaterialPage() => null!;
	public ReadOnlySpan<char> GetName() => "dummy material";
	public int GetNumAnimationFrames() => 0;
	public bool GetPropertyFlag(MaterialPropertyTypes needsBumpedLightmaps) => true;
	public IMaterialVar[]? GetShaderParams() => null;
	public string? GetShaderName() => null;
	public VertexFormat GetVertexFormat() => 0;
	public bool HasProxy() => false;
	public void IncrementReferenceCount() { }
	public bool InMaterialPage() => false;
	public bool IsErrorMaterialInternal() => false;
	public bool IsRealTimeVersion() => false;
	public bool IsTranslucent() => false;
	public void Refresh() { }
	public int ShaderParamCount() => 0;
	public bool TryFindVar(ReadOnlySpan<char> varName, [NotNullWhen(true)] out IMaterialVar? found, bool complain = true) {
		found = null;
		return false;
	}
}

public class DummyMaterialSystem : IMaterialSystemStub, IShaderUtil, IMatRenderContext
{
	internal static readonly DummyMaterial g_DummyMaterial = new();
	internal static readonly DummyMaterialVar g_DummyMaterialVar = new();
	internal static Matrix4x4 g_DummyMatrix = Matrix4x4.Identity;
	internal static readonly DummyTexture g_DummyTexture = new();
	internal static DummyMesh g_DummyMesh = null!;
	internal static readonly MaterialSystem_Config g_dummyConfig = new();

	IMaterialSystem? RealMaterialSystem;

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
	public IMesh CreateStaticMesh(VertexFormat format, ReadOnlySpan<char> textureGroup, IMaterial? material) => GetDummyMesh();
	public void DepthRange(double near, double far) { }
	public void EndFrame() { }
	public void EndLightmapAllocation() { }
	public void EndRender() { }
	public void EndRenderTargetAllocation() { }
	public void EndUpdateLightmaps() { }
	public IMaterial FindMaterial(ReadOnlySpan<char> filename, ReadOnlySpan<char> textureGroup, bool complain = false, ReadOnlySpan<char> complainPrefix = default) => g_DummyMaterial;
	public IMaterial? FindMaterialEx(ReadOnlySpan<char> materialName, ReadOnlySpan<char> textureGroupName, MaterialFindContext isOnAModel, bool complain = true, ReadOnlySpan<char> complainPrefix = default) => g_DummyMaterial;
	public IMaterial? FindProceduralMaterial(ReadOnlySpan<char> materialName, ReadOnlySpan<char> textureGroupName, KeyValues keyValues) => g_DummyMaterial;
	public ITexture FindTexture(ReadOnlySpan<char> textureName, ReadOnlySpan<char> textureGroupName, bool complain = true, int additionalCreationFlags = 0) => g_DummyTexture;
	public void Flush(bool flushHardware) { }
	public void GetBackBufferDimensions(out int width, out int height) {
		width = 1024;
		height = 768;
	}
	public int GetCurrentAdapter() => 0;
	public MaterialSystem_Config GetCurrentConfigForVideoCard() => g_dummyConfig;
	public IMaterial? GetCurrentMaterial() => null;
	public int GetDisplayAdapterCount() => 0;
	public IMesh GetDynamicMesh(bool buffered, IMesh? vertexOverride = null, IMesh? indexOverride = null, IMaterial? autoBind = null) => GetDummyMesh();
	public ITexture GetErrorTexture() => g_DummyTexture;
	public void GetLightmapPageSize(int lightmap, out int width, out int height) {
		if (RealMaterialSystem != null)
			RealMaterialSystem.GetLightmapPageSize(lightmap, out width, out height);
		else
			width = height = 32;
	}
	public IMaterialProxyFactory? GetMaterialProxyFactory() => null;
	public int GetMaxIndicesToRender(IMaterial material) => 32768;
	public int GetMaxVerticesToRender(IMaterial material) => 32768;
	public int GetNumSortIDs() => 10;
	public IMatRenderContext GetRenderContext() => this;
	public ITexture? GetRenderTarget() => g_DummyTexture;
	public void GetRenderTargetDimensions(out int screenWidth, out int screenHeight) => screenWidth = screenHeight = 256;
	public IShaderAPI GetShaderAPI() => null!; // todo
	public IShaderUtil GetShaderUtil() => this;
	public void GetSortInfo(Span<MaterialSystem_SortInfo> materialSortInfoArray) { }
	public void GetViewport(out int x, out int y, out int width, out int height) {
		x = y = 0;
		width = height = 640;
	}
	public void GetWindowSize(out int w, out int h) {
		w = h = 0;
		if (RealMaterialSystem != null) {
			using MatRenderContextPtr renderContext = new(RealMaterialSystem);
			renderContext.GetWindowSize(out w, out h);
		}
	}
	public void GetWorldSpaceCameraPosition(out Vector3 vecCameraPos) {
		vecCameraPos = default;
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
	public void Ortho(double left, double top, double right, double bottom, double near, double far) { }
	public bool OverrideConfig(MaterialSystem_Config config, bool forceUpdate) => false;
	public void PopMatrix() { }
	public void PopRenderTargetAndViewport() { }
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
	public void UpdateLightmap(int lightmapPageID, Span<int> lightmapSize, Span<int> offsetIntoLightmapPage, Span<float> floatImage, Span<float> floatImageBump1, Span<float> floatImageBump2, Span<float> floatImageBump3) { }
	public void Viewport(int x, int y, int width, int height) { }
	public void SetRealMaterialSystem(IMaterialSystem? sys) => RealMaterialSystem = sys;
}
