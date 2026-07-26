global using static Source.Engine.ShadowMgrGlobals;

using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;

using System.Numerics;

namespace Source.Engine;

public static class ShadowMgrGlobals
{
	public static readonly ShadowMgr g_ShadowMgr = new();

	public const float BACKFACE_EPSILON = 0.01f;

	public const int SHADOW_VERTEX_SMALL_CACHE_COUNT = 8;
	public const int SHADOW_VERTEX_LARGE_CACHE_COUNT = 32;
	public const int SHADOW_VERTEX_TEMP_COUNT = 48;
	public const int MAX_CLIP_PLANE_COUNT = 4;
	public const int SURFACE_BOUNDS_CACHE_COUNT = 1024;
	public const int SHADOW_DECAL_CACHE_COUNT = 16 * 1024;
	public const int MAX_SHADOW_DECAL_CACHE_COUNT = 64 * 1024;

	static readonly ConVar r_shadows = new("r_shadows", "1");
	static readonly ConVar r_shadows_gamecontrol = new("r_shadows_gamecontrol", "-1", FCvar.Cheat);
	static readonly ConVar r_shadowwireframe = new("r_shadowwireframe", "0", FCvar.Cheat);
	static readonly ConVar r_shadowids = new("r_shadowids", "0", FCvar.Cheat);
	static readonly ConVar r_flashlightdrawsweptbbox = new("r_flashlightdrawsweptbbox", "0");
	static readonly ConVar r_flashlightdrawfrustumbbox = new("r_flashlightdrawfrustumbbox", "0");
	static readonly ConVar r_flashlightnodraw = new("r_flashlightnodraw", "0");
	static readonly ConVar r_flashlightupdatedepth = new("r_flashlightupdatedepth", "1");
	static readonly ConVar r_flashlightdrawdepth = new("r_flashlightdrawdepth", "0");
	static readonly ConVar r_flashlightrenderworld = new("r_flashlightrenderworld", "1");
	static readonly ConVar r_flashlightrendermodels = new("r_flashlightrendermodels", "1");
	static readonly ConVar r_flashlightrender = new("r_flashlightrender", "1");
	static readonly ConVar r_flashlightculldepth = new("r_flashlightculldepth", "1");
	static readonly ConVar r_flashlight_version2 = new("r_flashlight_version2", "0", FCvar.Cheat | FCvar.DevelopmentOnly);

	public static ref uint FirstShadowOnModel(ModelInstanceHandle_t h) => throw new NotImplementedException();

	public static ref uint FirstModelInShadow(ShadowHandle_t h) => ref g_ShadowMgr.FirstModelInShadow(h);
}

public struct ShadowClipState
{
	public int CurrVert;
	public int TempCount;
	public int ClipCount;
	public ShadowVertex[] TempVertices;
	public ShadowVertex[][] ClipVertices;
}

public class ShadowMgr : IShadowMgrInternal, ISpatialLeafEnumerator
{
	public const ShadowFlags SHADOW_DISABLED = (ShadowFlags)((int)ShadowFlags.LastFlag << 1);

	struct SurfaceBounds_t
	{
		public fltx4 Mins;
		public fltx4 Maxs;
		public Vector3 Center;
		public float Radius;
		public int SurfaceIndex;
	}

	struct ShadowVertexSmallList
	{
		public ShadowVertex[] Verts;
	}

	struct ShadowVertexLargeList
	{
		public ShadowVertex[] Verts;
	}

	struct ShadowVertexCache
	{
		public ushort Count;
		public ShadowHandle_t Shadow;
		public ushort CachedVerts;
		public ShadowVertex[]? Verts;
	}

	struct Shadow
	{
		public ShadowInfo_t Info;
		public Vector3 ProjectionDir;
		public IMaterial? Material;
		public IMaterial? ModelMaterial;
		public object? BindProxy;
		public ShadowCreateFlags Flags;
		public ushort SortOrder;
		public float SphereRadius;
		public Ray Ray;
		public Vector3 SphereCenter;

		public FlashlightHandle_t FlashlightHandle;
		public ITexture? FlashlightDepthTexture;

		public ushort ClipPlaneCount;
		public Vector3[] ClipPlane;
		public float[] ClipDist;

		public ShadowSurfaceIndex_t FirstDecal;

		public uint FirstModel;

		public byte ShadowStencilBit;
	}

	struct ShadowDecal
	{
		public SurfaceHandle_t SurfID;
		public ShadowSurfaceIndex_t ShadowListIndex;
		public ShadowHandle_t Shadow;
		public DispShadowHandle DispShadow;
		public ushort ShadowVerts;

		public ShadowDecalHandle_t NextRender;
	}

	struct ShadowBuildInfo
	{
		public ShadowHandle_t Shadow;
		public Vector3 RayStart;
		public Vector3 ProjectionDirection;
		public Vector3 SphereCenter;
		public float SphereRadius;
		public byte[]? Vis;
	}

	struct ShadowRenderInfo
	{
		public int VertexCount;
		public int IndexCount;
		public int MaxVertices;
		public int MaxIndices;
		public int Count;
		public nint[]? Cache;
		public int DispCount;
		public Matrix4x4? ModelToWorld;
		public Matrix4x4 WorldToModel;
		public DispShadowHandle[]? DispCache;
	}

	struct SortOrderInfo
	{
		public IMaterial? MaterialEnum;
		public int RefCount;
	}

	delegate void ShadowDebugFunc(ShadowHandle_t shadowHandle, in Vector3 centroid);

	struct FlashlightInfo
	{
		public FlashlightState FlashlightState;
		public ShadowHandle_t Shadow;
		public Frustum Frustum;
		// public MaterialsBuckets<SurfaceHandle_t> MaterialBuckets;
		// public MaterialsBuckets<SurfaceHandle_t> OccluderBuckets;

		public List<IClientRenderable?> Renderables;
	}

	readonly List<Shadow> Shadows = [];

	readonly List<ShadowDecal> ShadowDecals = [];

	readonly List<ShadowDecalHandle_t> ShadowSurfaces = [];

	readonly List<ShadowDecalHandle_t> RenderQueue = [];

	readonly List<SortOrderInfo> SortOrderIds = [];

	readonly List<ShadowVertexCache> VertexCache = [];

	readonly List<ShadowVertexCache> TempVertexCache = [];

	readonly List<ShadowVertexSmallList> SmallVertexList = [];
	readonly List<ShadowVertexLargeList> LargeVertexList = [];

	readonly BidirectionalSet<ModelInstanceHandle_t, ShadowHandle_t> ShadowsOnModels = new();

	readonly List<SurfaceBounds_t> SurfaceBoundsCache = [];
	SurfaceBoundsCacheIndex_t[]? SurfaceBounds;

	int DecalsToRender;

	readonly List<FlashlightInfo> FlashlightStates = [];
	int NumWorldMaterialBuckets;
	bool Initialized;

	nint[]? ShadowDecalCache;
	DispShadowHandle[]? DispShadowDecalCache;

	public void LevelInit(int surfCount) => throw new NotImplementedException();

	public void LevelShutdown() => throw new NotImplementedException();

	void SetMaterial(ref Shadow shadow, IMaterial? material, IMaterial? modelMaterial, object? bindProxy) => throw new NotImplementedException();

	void CleanupMaterial(ref Shadow shadow) => throw new NotImplementedException();

	public int InvalidShadowIndex() => throw new NotImplementedException();

	public ShadowHandle_t CreateShadow(IMaterial? material, IMaterial? modelMaterial, object? bindProxy, int creationFlags) => throw new NotImplementedException();

	public ShadowHandle_t CreateShadowEx(IMaterial? material, IMaterial? modelMaterial, object? bindProxy, int creationFlags) => throw new NotImplementedException();

	public void DestroyShadow(ShadowHandle_t handle) => throw new NotImplementedException();

	public void SetShadowMaterial(ShadowHandle_t handle, IMaterial? material, IMaterial? modelMaterial, object? bindProxy) => throw new NotImplementedException();

	public void SetShadowTexCoord(ShadowHandle_t handle, float x, float y, float w, float h) => throw new NotImplementedException();

	public void ClearExtraClipPlanes(ShadowHandle_t h) => throw new NotImplementedException();

	public void AddExtraClipPlane(ShadowHandle_t h, in Vector3 normal, float dist) => throw new NotImplementedException();

	public ref readonly ShadowInfo_t GetInfo(ShadowHandle_t handle) => throw new NotImplementedException();

	ShadowVertex[]? GetCachedVerts(in ShadowVertexCache cache) => throw new NotImplementedException();

	void ClearTempCache() => throw new NotImplementedException();

	bool AddDecalToShadowList(ShadowHandle_t handle, ShadowDecalHandle_t decalHandle) => throw new NotImplementedException();

	void RemoveDecalFromShadowList(ShadowHandle_t handle, ShadowDecalHandle_t decalHandle) => throw new NotImplementedException();

	void ComputeSurfaceBounds(ref SurfaceBounds_t bounds, SurfaceHandle_t surfID) => throw new NotImplementedException();

	ref readonly SurfaceBounds_t GetSurfaceBounds(SurfaceHandle_t surfID) => throw new NotImplementedException();

	bool IsShadowNearSurface(ShadowHandle_t h, SurfaceHandle_t surfID, Matrix4x4? modelToWorld, Matrix4x4? worldToModel) => throw new NotImplementedException();

	void AddSurfaceToFlashlightMaterialBuckets(ShadowHandle_t handle, SurfaceHandle_t surfID) => throw new NotImplementedException();

	void AddSurfaceToShadow(ShadowHandle_t handle, SurfaceHandle_t surfID) => throw new NotImplementedException();

	void RemoveSurfaceFromShadow(ShadowHandle_t handle, SurfaceHandle_t surfID) => throw new NotImplementedException();

	void RemoveAllSurfacesFromShadow(ShadowHandle_t handle) => throw new NotImplementedException();

	void RemoveAllShadowsFromSurface(SurfaceHandle_t surfID) => throw new NotImplementedException();

	public void AddShadowToModel(ShadowHandle_t handle, ModelInstanceHandle_t model) => throw new NotImplementedException();

	public void RemoveAllShadowsFromModel(ModelInstanceHandle_t model) => throw new NotImplementedException();

	void RemoveAllModelsFromShadow(ShadowHandle_t handle) => throw new NotImplementedException();

	public void SetModelShadowState(ModelInstanceHandle_t instance) => throw new NotImplementedException();

	public bool ModelHasShadows(ModelInstanceHandle_t instance) => throw new NotImplementedException();

	void ApplyShadowToSurface(ref ShadowBuildInfo build, SurfaceHandle_t surfID) => throw new NotImplementedException();

	void ApplyShadowToDisplacement(ref ShadowBuildInfo build, IDispInfo? dispInfo, bool isFlashlight) => throw new NotImplementedException();

	public void EnableShadow(ShadowHandle_t handle, bool enable) => throw new NotImplementedException();

	public void SetFalloffBias(ShadowHandle_t shadow, byte bias) => throw new NotImplementedException();

	public void ProjectShadow(ShadowHandle_t handle, in Vector3 origin, in Vector3 projectionDir, in Matrix4x4 worldToShadow, in Vector2 size, ReadOnlySpan<int> leafList, float maxHeight, float falloffOffset, float falloffAmount, in Vector3 casterOrigin) => throw new NotImplementedException();

	public void ProjectFlashlight(ShadowHandle_t handle, in Matrix4x4 worldToShadow, ReadOnlySpan<int> leafList) => throw new NotImplementedException();

	void ApplyFlashlightToLeaf(in Shadow shadow, BSPMLeaf? leaf, ref ShadowBuildInfo build) => throw new NotImplementedException();

	void ApplyShadowToLeaf(in Shadow shadow, BSPMLeaf? leaf, ref ShadowBuildInfo build) => throw new NotImplementedException();

	public bool EnumerateLeaf(int leaf, nint context) => throw new NotImplementedException();

	public void AddShadowToBrushModel(ShadowHandle_t handle, Model? model, in Vector3 origin, in QAngle angles) => throw new NotImplementedException();

	public void RemoveAllShadowsFromBrushModel(Model? model) => throw new NotImplementedException();

	public void AddShadowsOnSurfaceToRenderList(ShadowDecalHandle_t decalHandle) => throw new NotImplementedException();

	public void ClearShadowRenderList() => throw new NotImplementedException();

	public void RenderShadows(in Matrix4x4? modelToWorld) => throw new NotImplementedException();

	public void RenderProjectedTextures(in Matrix4x4? modelToWorld) => throw new NotImplementedException();

	bool ProjectVerticesIntoShadowSpace(in Matrix4x4 modelToShadow, float maxDist, ReadOnlySpan<Vector3> position, ref ShadowClipState clip) => throw new NotImplementedException();

	int ProjectAndClipVertices(in Shadow shadow, in Matrix4x4 worldToShadow, Matrix4x4? worldToModel, ReadOnlySpan<Vector3> position, out ShadowVertex[]? outVertex) => throw new NotImplementedException();

	public int ProjectAndClipVertices(ShadowHandle_t handle, ReadOnlySpan<Vector3> position, out ShadowVertex[]? outVertex) => throw new NotImplementedException();

	bool ComputeShadowVertices(ref ShadowDecal decal, Matrix4x4? modelToWorld, Matrix4x4? worldToModel, ref ShadowVertexCache vertexCache) => throw new NotImplementedException();

	void GenerateShadowRenderInfo(MatRenderContextPtr renderContext, ShadowDecalHandle_t decalHandle, ref ShadowRenderInfo info) => throw new NotImplementedException();

	public void ComputeRenderInfo(ref ShadowDecalRenderInfo info, ShadowHandle_t handle) => throw new NotImplementedException();

	int AddNormalShadowsToMeshBuilder(ref MeshBuilder meshBuilder, ref ShadowRenderInfo info) => throw new NotImplementedException();

	int AddDisplacementShadowsToMeshBuilder(ref MeshBuilder meshBuilder, ref ShadowRenderInfo info, int baseIndex) => throw new NotImplementedException();

	void RenderDebuggingInfo(in ShadowRenderInfo info, ShadowDebugFunc func) => throw new NotImplementedException();

	void RenderShadowList(MatRenderContextPtr renderContext, ShadowDecalHandle_t decalHandle, Matrix4x4? modelToWorld) => throw new NotImplementedException();

	public void SetNumWorldMaterialBuckets(int numMaterialSortBins) => throw new NotImplementedException();

	void ClearAllFlashlightMaterialBuckets() => throw new NotImplementedException();

	void AllocFlashlightMaterialBuckets(FlashlightHandle_t flashlightID) => throw new NotImplementedException();

	public void UpdateFlashlightState(ShadowHandle_t shadowHandle, in FlashlightState lightState) => throw new NotImplementedException();

	public void SetFlashlightDepthTexture(ShadowHandle_t shadowHandle, ITexture? flashlightDepthTexture, byte shadowStencilBit) => throw new NotImplementedException();

	void SetStencilAndScissor(MatRenderContextPtr renderContext, ref FlashlightInfo flashlightInfo, bool useStencil) => throw new NotImplementedException();

	public void SetFlashlightStencilMasks(bool doMasking) => throw new NotImplementedException();

	void DisableStencilAndScissorMasking(MatRenderContextPtr renderContext) => throw new NotImplementedException();

	void EnableStencilAndScissorMasking(MatRenderContextPtr renderContext, in FlashlightInfo flashlightInfo, bool doMasking) => throw new NotImplementedException();

	public void SetFlashlightRenderState(ShadowHandle_t handle) => throw new NotImplementedException();

	public void RenderFlashlights(bool doMasking, in Matrix4x4? modelToWorld) => throw new NotImplementedException();

	public ref readonly Frustum GetFlashlightFrustum(ShadowHandle_t handle) => throw new NotImplementedException();

	public ref readonly FlashlightState GetFlashlightState(ShadowHandle_t handle) => throw new NotImplementedException();

	public void DrawFlashlightDecals(int sortGroup, bool doMasking) => throw new NotImplementedException();

	public void DrawFlashlightDecalsOnDisplacements(int sortGroup, ReadOnlySpan<DispInfo?> visibleDisps, int visibleDispCount, bool doMasking) => throw new NotImplementedException();

	public void DrawFlashlightDecalsOnSingleSurface(SurfaceHandle_t surfID, bool doMasking) => throw new NotImplementedException();

	public void DrawFlashlightOverlays(int sortGroup, bool doMasking) => throw new NotImplementedException();

	public void DrawFlashlightDepthTexture() => throw new NotImplementedException();

	public void AddFlashlightRenderable(ShadowHandle_t shadowHandle, IClientRenderable? renderable) => throw new NotImplementedException();

	public ref uint FirstModelInShadow(ShadowHandle_t h) => throw new NotImplementedException();
}
