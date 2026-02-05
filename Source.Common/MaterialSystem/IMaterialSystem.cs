using Source.Common.Bitmap;
using Source.Common.Formats.Keyvalues;
using Source.Common.Launcher;
using Source.Common.Mathematics;
using Source.Common.ShaderAPI;

using System.Numerics;

namespace Source.Common.MaterialSystem;

public enum MaterialIndexFormat
{
	Unknown = -1,
	x16 = 0,
	x32,
}

public enum MaterialBufferTypes
{
	Front,
	Back
}

public enum MaterialPrimitiveType
{
	Points,
	Lines,
	Triangles,
	TriangleStrip,
	LineStrip,
	LineLoop,
	Polygon,
	Quads,
	InstancedQuads,
	Heterogenous
}

public enum MaterialFogMode
{
	None,
	Linear,
	LinearBelowFogZ
}

public enum ShaderParamType
{
	Texture,
	Integer,
	Color,
	Vec2,
	Vec3,
	Vec4,
	EnvMap,
	Float,
	Bool,
	FourCC,
	Matrix,
	Material,
	String,
	Matrix4x2
}

public enum MaterialMatrixMode
{
	View,
	Projection,
	Model,
	Count
}

public enum MaterialFindContext
{
	None,
	IsOnAModel
}

public record struct VertexShaderHandle
{
	public static readonly VertexShaderHandle INVALID = new(-1);
	public VertexShaderHandle(nint handle) {
		Handle = handle;
	}

	public nint Handle;
	public static implicit operator nint(VertexShaderHandle handle) => handle.Handle;
	public static implicit operator VertexShaderHandle(nint handle) => new(handle);

	public readonly bool IsValid() => Handle != INVALID;
}

public record struct GeometryShaderHandle
{
	public static readonly GeometryShaderHandle INVALID = new(-1);
	public GeometryShaderHandle(nint handle) {
		Handle = handle;
	}

	public nint Handle;
	public static implicit operator nint(GeometryShaderHandle handle) => handle.Handle;
	public static implicit operator GeometryShaderHandle(nint handle) => new(handle);

	public readonly bool IsValid() => Handle != INVALID;
}

public record struct PixelShaderHandle
{
	public static readonly PixelShaderHandle INVALID = new(-1);
	public PixelShaderHandle(nint handle) {
		Handle = handle;
	}

	public nint Handle;
	public static implicit operator nint(PixelShaderHandle handle) => handle.Handle;
	public static implicit operator PixelShaderHandle(nint handle) => new(handle);

	public readonly bool IsValid() => Handle != INVALID;
}
public struct MaterialVideoMode
{
	public int Width;            // if width and height are 0 and you select 
	public int Height;           // windowed mode, it'll use the window size
	public ImageFormat Format;   // use ImageFormats (ignored for windowed mode)
	public int RefreshRate;      // 0 == default (ignored for windowed mode)
}

public enum CreateRenderTargetFlags
{
	HDR = 0x00000001,
	AutoMipmap = 0x00000002,
	UnfilterableOk = 0x00000004,
}

public enum RenderTargetSizeMode
{
	NoChange = 0,
	Default = 1,
	Picmip = 2,
	HDR = 3,
	FullFrameBuffer = 4,
	Offscreen = 5,
	FullFrameBufferRoundedUp = 6,
	ReplayScreenshot = 7,
	Literal = 8,
	LiteralPicmip = 9
}

public enum MaterialRenderTargetDepth
{
	Shared,
	Separate,
	None,
	Only
}

public enum MaterialPropertyTypes
{
	NeedsLightmap,
	Opacity,
	Reflectivity,
	NeedsBumpedLightmaps
}

public struct StandardLightmap
{
	public const int White = -1;
	public const int WhiteBump = -2;
	public const int UserDefined = -3;
}

public struct MaterialSystem_SortInfo
{
	public IMaterial? Material;
	public int LightmapPageID;
}

public interface IMaterialSystem
{
	public const int NUM_MODEL_TRANSFORMS = 53;
	public const float OVERBRIGHT = 2;
	public const float OO_OVERBRIGHT = 1f / 2f;
	public const float GAMMA = 2.2f;
	public const float TEXGAMMA = 2.2f;

	event Action Restore;
	IMatRenderContext GetRenderContext();
	void ModInit();
	void ModShutdown();
	void BeginFrame(double frameTime);
	void EndFrame();
	void SwapBuffers();
	MaterialSystem_Config GetCurrentConfigForVideoCard();
	bool UpdateConfig(bool forceUpdate);
	bool OverrideConfig(MaterialSystem_Config config, bool forceUpdate);
	int GetDisplayAdapterCount();
	int GetCurrentAdapter();
	int GetModeCount(int adapter);
	bool SetMode(IWindow window, MaterialSystem_Config config);
	void AddModeChangeCallBack(Action func);
	IMaterial CreateMaterial(ReadOnlySpan<char> name, ReadOnlySpan<char> textureGroupName, KeyValues keyValues);
	IMaterial CreateMaterial(ReadOnlySpan<char> name, KeyValues keyValues);
	bool CanUseEditorMaterials();
	IMaterial FindMaterial(ReadOnlySpan<char> filename, ReadOnlySpan<char> textureGroup, bool complain = false, ReadOnlySpan<char> complainPrefix = default);
	IMaterial? FindProceduralMaterial(ReadOnlySpan<char> materialName, ReadOnlySpan<char> textureGroupName, KeyValues keyValues);
	void RestoreShaderObjects(IServiceProvider services, int changeFlags);
	ITexture CreateProceduralTexture(ReadOnlySpan<char> textureName, ReadOnlySpan<char> textureGroup, int wide, int tall, ImageFormat format, TextureFlags flags);
	ITexture? CreateNamedRenderTargetTextureEx(ReadOnlySpan<char> rtName, int w, int h, RenderTargetSizeMode sizeMode, ImageFormat format, MaterialRenderTargetDepth depthMode, TextureFlags textureFlags, CreateRenderTargetFlags renderTargetFlags);
	void BeginRenderTargetAllocation();
	void EndRenderTargetAllocation();
	int GetNumSortIDs();
	void EndLightmapAllocation();
	void BeginLightmapAllocation();
	short AllocateLightmap(int allocationWidth, int allocationHeight, Span<int> offsetIntoLightmapPage, IMaterial? material);
	short AllocateWhiteLightmap(IMaterial? material);
	void UpdateLightmap(int lightmapPageID, Span<int> lightmapSize, Span<int> offsetIntoLightmapPage, Span<float> floatImage, Span<float> floatImageBump1, Span<float> floatImageBump2, Span<float> floatImageBump3);
	void GetLightmapPageSize(int lightmap, out int width, out int height);
	void GetSortInfo(Span<MaterialSystem_SortInfo> materialSortInfoArray);
	void GetBackBufferDimensions(out int width, out int height);
	IShaderUtil GetShaderUtil();
	ITexture FindTexture(ReadOnlySpan<char> textureName, ReadOnlySpan<char> textureGroupName, bool complain = true, int additionalCreationFlags = 0);
	ITexture GetErrorTexture();
	IMaterial? FindMaterialEx(ReadOnlySpan<char> materialName, ReadOnlySpan<char> textureGroupName, MaterialFindContext isOnAModel, bool complain = true, ReadOnlySpan<char> complainPrefix = default);
	void BeginUpdateLightmaps();
	void EndUpdateLightmaps();
	void SetMaterialProxyFactory(IMaterialProxyFactory? factory);
	IMaterialProxyFactory? GetMaterialProxyFactory();
}

public interface IMatRenderContext
{
	void BeginRender();
	void EndRender();
	void Flush(bool flushHardware);

	void ClearBuffers(bool clearColor, bool clearDepth, bool clearStencil = false);

	void Viewport(int x, int y, int width, int height);
	void GetViewport(out int x, out int y, out int width, out int height);

	void ClearColor3ub(byte r, byte g, byte b);
	void ClearColor4ub(byte r, byte g, byte b, byte a);
	void DepthRange(double near, double far);

	void MatrixMode(MaterialMatrixMode mode);
	void PushMatrix();
	void PopMatrix();
	void LoadIdentity();
	void Bind(IMaterial material, object? proxyData);
	IMaterial? GetCurrentMaterial();
	IShaderAPI GetShaderAPI();
	bool InFlashlightMode();
	IMesh GetDynamicMesh(bool buffered, IMesh? vertexOverride = null, IMesh? indexOverride = null, IMaterial? autoBind = null);
	void GetRenderTargetDimensions(out int screenWidth, out int screenHeight);
	void Scale(float x, float y, float z);
	void Ortho(double left, double top, double right, double bottom, double near, double far);
	void PushRenderTargetAndViewport(ITexture? thisTexture);
	void PopRenderTargetAndViewport();
	void PushRenderTargetAndViewport(ITexture? renderTarget, int x, int y, int width, int height);
	void PushRenderTargetAndViewport(ITexture? renderTarget, ITexture? depthTarget, int x, int y, int width, int height);
	void GetWindowSize(out int w, out int h);
	ITexture? GetRenderTarget();
	IMesh CreateStaticMesh(VertexFormat format, ReadOnlySpan<char> textureGroup, IMaterial? material);
	int GetMaxVerticesToRender(IMaterial material);
	int GetMaxIndicesToRender(IMaterial material);
	void LoadMatrix(in Matrix3x4 matrix);
	void LoadMatrix(in Matrix4x4 matrix);
	float ComputePixelDiameterOfSphere(Vector3 origin, float radius);
	float ComputePixelWidthOfSphere(Vector3 origin, float radius);
	void SetNumBoneWeights(int v);
	void LoadBoneMatrix(int hardwareID, in Matrix3x4 matrix4x4);
	void GetWorldSpaceCameraPosition(out Vector3 vecCameraPos);
	void BindLightmapPage(int lightmapPageID);
	void BindLightmap(Sampler sampler);
	void BindStandardTexture(Sampler sampler, StandardTextureId id);
}

public readonly struct MatRenderContextPtr : IDisposable, IMatRenderContext
{
	readonly IMatRenderContext ctx;
	readonly IShaderAPI shaderAPI;
	public readonly IMatRenderContext Context => ctx;

	public MatRenderContextPtr(IMatRenderContext init) {
		ctx = init;
		shaderAPI = init.GetShaderAPI();
		init.BeginRender();
	}
	public MatRenderContextPtr(IMaterialSystem from) {
		ctx = from.GetRenderContext();
		shaderAPI = ctx.GetShaderAPI();
		ctx.BeginRender();
	}

	public readonly void Dispose() => ctx.EndRender();
	public void BeginRender() => ctx.BeginRender();
	public void EndRender() => ctx.EndRender();
	public void Flush(bool flushHardware = false) => ctx.Flush(flushHardware);
	public void ClearBuffers(bool clearColor, bool clearDepth, bool clearStencil = false) => ctx.ClearBuffers(clearColor, clearDepth, clearStencil);
	public void Viewport(int x, int y, int width, int height) => ctx.Viewport(x, y, width, height);
	public void GetViewport(out int x, out int y, out int width, out int height) => ctx.GetViewport(out x, out y, out width, out height);
	public void ClearColor3ub(byte r, byte g, byte b) => ctx.ClearColor3ub(r, g, b);
	public void ClearColor4ub(byte r, byte g, byte b, byte a) => ctx.ClearColor4ub(r, g, b, a);
	public void DepthRange(double near, double far) => ctx.DepthRange(near, far);
	public void MatrixMode(MaterialMatrixMode mode) => ctx.MatrixMode(mode);
	public void PushMatrix() => ctx.PushMatrix();
	public void LoadIdentity() => ctx.LoadIdentity();
	public void Bind(IMaterial material, object? proxyData = null) => ctx.Bind(material, proxyData);
	public void BindLightmap(Sampler sampler) => ctx.BindLightmap(sampler);
	public void BindLightmapPage(int lightmapPageID) => ctx.BindLightmapPage(lightmapPageID);
	public IMaterial? GetCurrentMaterial() => ctx.GetCurrentMaterial();
	public void PopMatrix() => ctx.PopMatrix();
	public IShaderAPI GetShaderAPI() => ctx.GetShaderAPI();
	public IMesh GetDynamicMesh(bool buffered = true, IMesh? vertexOverride = null, IMesh? indexOverride = null, IMaterial? autoBind = null) =>
		ctx.GetDynamicMesh(buffered, vertexOverride, indexOverride, autoBind);
	public bool InFlashlightMode() => ctx.InFlashlightMode();
	public void GetRenderTargetDimensions(out int screenWidth, out int screenHeight) =>
		ctx.GetRenderTargetDimensions(out screenWidth, out screenHeight);
	public void Scale(float x, float y, float z) => ctx.Scale(x, y, z);
	public void Ortho(double left, double top, double right, double bottom, double near, double far) => ctx.Ortho(left, top, right, bottom, near, far);
	public void PushRenderTargetAndViewport(ITexture? thisTexture) => ctx.PushRenderTargetAndViewport(thisTexture);
	public void PopRenderTargetAndViewport() => ctx.PopRenderTargetAndViewport();

	public void PushRenderTargetAndViewport(ITexture? renderTarget, int x, int y, int width, int height)
		=> ctx.PushRenderTargetAndViewport(renderTarget, x, y, width, height);

	public void GetWindowSize(out int w, out int h) => ctx.GetWindowSize(out w, out h);

	public ITexture? GetRenderTarget() => ctx.GetRenderTarget();

	public IMesh CreateStaticMesh(VertexFormat format, ReadOnlySpan<char> textureGroup, IMaterial? material = null) => ctx.CreateStaticMesh(format, textureGroup, material);

	public int GetMaxVerticesToRender(IMaterial material) => ctx.GetMaxVerticesToRender(material);
	public int GetMaxIndicesToRender(IMaterial material) => ctx.GetMaxIndicesToRender(material);

	public void TurnOnToneMapping() {
		// todo
	}

	public void PushRenderTargetAndViewport(ITexture? rtColor, ITexture? rtDepth, int x, int y, int width, int height) => ctx.PushRenderTargetAndViewport(rtColor, rtDepth, x, y, width, height);

	public void LoadMatrix(in Matrix3x4 matrix) => ctx.LoadMatrix(in matrix);
	public void LoadMatrix(in Matrix4x4 matrix) => ctx.LoadMatrix(in matrix);

	public float ComputePixelDiameterOfSphere(Vector3 origin, float radius) => ctx.ComputePixelDiameterOfSphere(origin, radius);
	public float ComputePixelWidthOfSphere(Vector3 origin, float radius) => ctx.ComputePixelWidthOfSphere(origin, radius);

	public void SetNumBoneWeights(int v) => ctx.SetNumBoneWeights(v);

	public void LoadBoneMatrix(int hardwareID, in Matrix3x4 matrix) => ctx.LoadBoneMatrix(hardwareID, in matrix);

	public void GetWorldSpaceCameraPosition(out Vector3 vecCameraPos) {
		ctx.GetWorldSpaceCameraPosition(out vecCameraPos);
	}

	public void BindStandardTexture(Sampler sampler, StandardTextureId id) {
		ctx.BindStandardTexture(sampler, id);
	}
}
