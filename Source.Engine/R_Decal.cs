using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.MaterialSystem;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Engine;

[InlineArray(Render.DECALCACHE_ENTRY_COUNT)] public struct InlineArrayDecalCacheEntryCount<T> { public T item; }
[InlineArray(Render.DECALSORT_TYPE_COUNT)] public struct InlineArrayDecalSortTypeCount<T> { public T item; }
[InlineArray((int)MatSortGroup.Max + 1)] public struct InlineArrayMaxMatSortGroups<T> { public T item; }

public enum DecalType
{
	Normal = 0x00,
	Custom = 0x01
}

[Flags]
public enum FDecal : short
{
	Permanent = 0x01,
	Reference = 0x02,
	Custom = 0x04,
	HFlip = 0x08,
	VFlip = 0x10,
	UseSAxis = 0x80,
	Dynamic = 0x100,
	SecondPass = 0x200,
	DontSave = 0x800,
	PlayerSpray = 0x1000,
	DistanceScale = 0x2000,
	HasUpdated = 0x4000
}

public class Decal
{
	public Decal? Next;
	public Decal? DestroyList;
	public SurfaceHandle_t SurfID;
	public IMaterial? Material;
	public float LightmapOffset;
	public Vector3 Position;
	public Vector3 SAxis;
	public float Dx;
	public float Dy;
	public float Scale;
	public float Size;
	public float FadeDuration;
	public TimeUnit_t FadeStartTime;
	public Color Color;
	public object? UserData;
	public DispDecalHandle DispDecal;
	public ushort ClippedVertCount;
	public ushort CacheHandle;
	public ushort DecalPool;
	public FDecal Flags;
	public short EntityIndex;
	public nint SortTree;
	public nint SortMaterial;
}

public class DecalInfo
{
	public Vector3 Position;
	public Vector3 SAxis;
	public Model? Model;
	public WorldBrushData? Brush;
	public IMaterial? Material;
	public float Size;
	public FDecal Flags;
	public int Entity;
	public float Scale;
	public float FadeDuration;
	public TimeUnit_t FadeStartTime;
	public int DecalWidth;
	public int DecalHeight;
	public Color Color;
	public InlineArray3<Vector3> Basis;
	public object? UserData;
	public Vector3? Normal;
	public readonly List<SurfaceHandle_t> ApplySurfs = [];
}

public struct DecalCache
{
	public InlineArray4<DecalVert> DecalVert;
}

public enum DecalSortType
{
	PermanentLightmap = 0,
	Lightmap,
	NonLightmap,
	Count
}

public struct DecalSortVertexFormat
{
	public VertexFormat VertexFormat;
	public nint SortTree;
}

public struct DecalMaterialSortData
{
	public IMaterial? Material;
	public int LightmapPage;
	public nint Bucket;
}

public struct DecalMaterialBucket
{
	public nint Head;
	public int CheckCount;
}

public struct DecalSortTrees
{
	public InlineArrayDecalSortTypeCount<SortedSet<DecalMaterialSortData>> Trees;
	public InlineArrayMaxMatSortGroups<InlineArrayDecalSortTypeCount<List<DecalMaterialBucket>>> DecalSortBuckets;

	public DecalSortTrees() {
		for (int sort = 0; sort < Render.DECALSORT_TYPE_COUNT; sort++)
			Trees[sort] = new SortedSet<DecalMaterialSortData>(Comparer<DecalMaterialSortData>.Create((decal1, decal2) => Render.DecalSortTreeSortLessFunc(in decal1, in decal2) ? -1 : 1));

		for (int group = 0; group < (int)MatSortGroup.Max + 1; group++)
			for (int sort = 0; sort < Render.DECALSORT_TYPE_COUNT; sort++)
				DecalSortBuckets[group][sort] = [];
	}
}

public struct DecalBatchList
{
	public IMaterial? Material;
	public object? Proxy;
	public int LightmapPage;
	public ushort StartIndex;
	public ushort IndexCount;
}

public struct DecalMeshList
{
	public IMesh? Mesh;
	public List<DecalBatchList> Batches;
}

public struct DecalContext(IMatRenderContext renderContext, in Vector3 modelOrg)
{
	public Vector3 ModelOrg = modelOrg;
	public Vector3 SAxis;
	public float SOffset = -1;
	public Vector3 TAxis;
	public float TOffset = -1;
	public float SScale = -1;
	public float TScale = -1;
	public IMatRenderContext? RenderContext = renderContext;
	public SurfaceHandle_t Surf = -1; // TODO: global INVALID_SURFACE_HANDLE, shoulda done that sooner

	public void InitSurface(SurfaceHandle_t surfID) {
		throw new NotImplementedException();
	}

	public readonly float ComputeS(in Vector3 pos) {
		throw new NotImplementedException();
	}

	public readonly float ComputeT(in Vector3 pos) {
		throw new NotImplementedException();
	}
}

public class DecalVertCache
{
	enum DecalIndexOrdinal
	{
		DecalIndex = 0,
		NextVertBlockIndex = 1,
		IsFreeIndex = 2,
		FrameCountIndex = 3
	}

	InlineArrayDecalCacheEntryCount<DecalCache> Cache;
	int FreeBlockCount;
	int FirstFree;
	int FrameBlocks;
	int LastFrameCount;
	int FreeTestIndex;

	public void StoreVertsInCache(Decal decal, Span<DecalVert> list) {
		throw new NotImplementedException();
	}

	public void FreeCachedVerts(Decal decal) {
		throw new NotImplementedException();
	}

	public Span<DecalVert> GetCachedVerts(Decal decal) {
		throw new NotImplementedException();
	}

	public void Init() {
		throw new NotImplementedException();
	}

	int GetIndex(ref DecalCache block, DecalIndexOrdinal index) {
		throw new NotImplementedException();
	}

	void SetIndex(ref DecalCache block, DecalIndexOrdinal index, int value) {
		throw new NotImplementedException();
	}

	void SetNext(int cur, int next) {
		throw new NotImplementedException();
	}

	void SetFree(int block, bool free) {
		throw new NotImplementedException();
	}

	bool IsFree(int block) {
		throw new NotImplementedException();
	}

	ref DecalCache NextBlock(ref DecalCache cache) {
		throw new NotImplementedException();
	}

	void FreeBlock(int cacheIndex) {
		throw new NotImplementedException();
	}

	void FindFreeBlocks(int blockCount) {
		throw new NotImplementedException();
	}

	int AllocBlock() {
		throw new NotImplementedException();
	}

	int AllocBlocks(int blockCount) {
		throw new NotImplementedException();
	}
}

public partial class Render
{
	public const int DECAL_DISTANCE = 4;
	public const int DECALCACHE_ENTRY_COUNT = 1024;
	public const int INVALID_CACHE_ENTRY = 0xFFFF;
	public const int DECALSORT_RBTREE_SIZE = 16;
	public const int DECALSORT_TYPE_COUNT = 3;

	public static readonly ConVar r_decal_overlap_count = new("r_decal_overlap_count", "3", 0);
	public static readonly ConVar r_decal_overlap_area = new("r_decal_overlap_area", "0.4", 0);
	public static readonly ConVar r_decal_cover_count = new("r_decal_cover_count", "4", 0);
	public static readonly ConVar r_dscale_nearscale = new("r_dscale_nearscale", "1", FCvar.Cheat);
	public static readonly ConVar r_dscale_neardist = new("r_dscale_neardist", "100", FCvar.Cheat);
	public static readonly ConVar r_dscale_farscale = new("r_dscale_farscale", "4", FCvar.Cheat);
	public static readonly ConVar r_dscale_fardist = new("r_dscale_fardist", "2000", FCvar.Cheat);
	public static readonly ConVar r_dscale_basefov = new("r_dscale_basefov", "90", FCvar.Cheat);
	public static readonly ConVar r_spray_lifetime = new("r_spray_lifetime", "2", 0, "Number of rounds player sprays are visible");
	public static readonly ConVar r_queued_decals = new("r_queued_decals", "0", 0, "Offloads a bit of decal rendering setup work to the material system queue when enabled.");
	public static readonly ConVar r_drawdecals = new("r_drawdecals", "1", FCvar.Cheat, "Render decals.");
	public static readonly ConVar r_drawbatchdecals = new("r_drawbatchdecals", "1", 0, "Render decals batched.");

	static uint s_DecalScaleVarCache = 0;
	static uint s_DecalFadeVarCache = 0;

	static readonly ClassMemoryPool<Decal> g_DecalAllocator = new();
	static int g_DynamicDecals = 0;
	static int g_StaticDecals = 0;
	static int g_LastReplacedDynamic = -1;

	public static readonly List<Decal?> s_DecalPool = [];

	static readonly DecalVertCache g_DecalVertCache = new();
	static Decal? s_DecalDestroyList = null;

	public static int g_MaxDecals = 0;

	public static Matrix4x4 g_BrushToWorldMatrix;

	static readonly List<SurfaceHandle_t>[] s_DecalSurfaces = new List<SurfaceHandle_t>[(int)MatSortGroup.Max + 1].InstantiateArray();

	public static readonly List<DecalSortVertexFormat> g_DecalFormats = [];

	public static readonly PooledLinkedList<int> g_DecalSortPool = new();
	public static readonly List<DecalSortTrees> g_DecalSortTrees = [];
	public static int g_DecalSortCheckCount = 0;
	public static int g_BrushModelDecalSortCheckCount = 0;

	public static readonly PooledLinkedList<int> g_DispDecalSortPool = new();
	public static readonly List<DecalSortTrees> g_DispDecalSortTrees = [];
	public static int g_DispDecalSortCheckCount = 0;

	public static bool DecalSortTreeSortLessFunc(in DecalMaterialSortData decal1, in DecalMaterialSortData decal2) {
		throw new NotImplementedException();
	}

	[ConCommand("r_printdecalinfo")]
	static void r_printdecalinfo_f() {
		throw new NotImplementedException();
	}

	public static float ComputeDecalLightmapOffset(SurfaceHandle_t surfID) {
		throw new NotImplementedException();
	}

	static VertexFormat GetUncompressedFormat(IMaterial material) {
		throw new NotImplementedException();
	}

	public static void Shader_DecalDrawPoly(Span<DecalVert> v, IMaterial material, SurfaceHandle_t surfID, int vertCount, Decal decal, float fade) {
		throw new NotImplementedException();
	}

	public static void R_DecalGetMaterialAndSize(int decalIndex, out IMaterial? decalMaterial, out float w, out float h) {
		throw new NotImplementedException();
	}

	static Decal? MSurf_DecalPointer(SurfaceHandle_t surfID) {
		throw new NotImplementedException();
	}

	static WorldDecalHandle_t DecalToHandle(Decal? decal) {
		throw new NotImplementedException();
	}

	public static void R_DecalInit() {
		throw new NotImplementedException();
	}

	public static void R_DecalTerm(WorldBrushData? brushData, bool termPermanentDecals) {
		throw new NotImplementedException();
	}

	public static void R_DecalTermAll() {
		throw new NotImplementedException();
	}

	static void R_DecalCacheClear(Decal decal) {
		throw new NotImplementedException();
	}

	public static void R_DecalFlushDestroyList() {
		throw new NotImplementedException();
	}

	static void R_DecalAddToDestroyList(Decal decal) {
		throw new NotImplementedException();
	}

	public static void R_DecalUnlink(Decal decal, WorldBrushData? data) {
		throw new NotImplementedException();
	}

	public static int R_FindFreeDecalSlot() {
		throw new NotImplementedException();
	}

	public static void SpewDecals() {
		throw new NotImplementedException();
	}

	public static int R_FindDynamicDecalSlot(int startAt) {
		throw new NotImplementedException();
	}

	static Decal? R_DecalAlloc(FDecal flags) {
		throw new NotImplementedException();
	}

	public static void R_DecalSurface(SurfaceHandle_t surfID, DecalInfo decalInfo, bool forceForDisplacement) {
		throw new NotImplementedException();
	}

	static void R_DecalNodeSurfaces(BSPMNode node, DecalInfo decalInfo) {
		throw new NotImplementedException();
	}

	public static void R_DecalLeaf(BSPMLeaf leaf, DecalInfo decalInfo) {
		throw new NotImplementedException();
	}

	static void R_DecalNode(BSPMNode? node, DecalInfo decalInfo) {
		throw new NotImplementedException();
	}

	static int DecalListAdd(Span<DecalList> list, int count) {
		throw new NotImplementedException();
	}

	static bool DecalDepthCompare(in DecalList elem1, in DecalList elem2) {
		throw new NotImplementedException();
	}

	public static int DecalListCreate(Span<DecalList> list) {
		throw new NotImplementedException();
	}

	static bool R_DecalUnProject(Decal decal, ref DecalList entry) {
		throw new NotImplementedException();
	}

	static void R_DecalShoot_(IMaterial? material, int entity, Model? model, in Vector3 position, Vector3? saxis, FDecal flags, in Color rgbaColor, Vector3? normal, object? userData = null) {
		throw new NotImplementedException();
	}

	public static void R_DecalShoot(int textureIndex, int entity, Model? model, in Vector3 position, Vector3? saxis, FDecal flags, in Color rgbaColor, Vector3? normal) {
		throw new NotImplementedException();
	}

	public static void R_PlayerDecalShoot(IMaterial material, object? userData, int entity, Model? model, in Vector3 position, Vector3? saxis, FDecal flags, in Color rgbaColor) {
		throw new NotImplementedException();
	}

	static void R_DecalVertsLight(Span<DecalVert> v, in DecalContext context, SurfaceHandle_t surfID, int vertCount) {
		throw new NotImplementedException();
	}

	static Decal? R_DecalFindOverlappingDecals(DecalInfo decalInfo, SurfaceHandle_t surfID) {
		throw new NotImplementedException();
	}

	static void R_AddDecalToSurface(Decal decal, SurfaceHandle_t surfID, DecalInfo decalInfo) {
		throw new NotImplementedException();
	}

	public static void R_DecalSortInit() {
		throw new NotImplementedException();
	}

	public static void DecalSurfacesInit(bool brushModel) {
		throw new NotImplementedException();
	}

	static void R_DecalMaterialSort(Decal decal, SurfaceHandle_t surfID) {
		throw new NotImplementedException();
	}

	public static void R_DecalReSortMaterials() {
		throw new NotImplementedException();
	}

	static void R_DecalCreate(DecalInfo decalInfo, SurfaceHandle_t surfID, float x, float y, bool forceForDisplacement) {
		throw new NotImplementedException();
	}

	public static bool DecalUpdate(Decal decal) {
		throw new NotImplementedException();
	}

	public static Span<DecalVert> R_DecalSetupVerts(ref DecalContext context, Decal decal, SurfaceHandle_t surfID, IMaterial material) {
		throw new NotImplementedException();
	}

	public static void DecalUpdateAndDrawSingle(ref DecalContext context, SurfaceHandle_t surfID, Decal decal) {
		throw new NotImplementedException();
	}

	public static void DrawDecalsOnSingleSurface_NonQueued(IMatRenderContext renderContext, SurfaceHandle_t surfID, in Vector3 modelOrg) {
		throw new NotImplementedException();
	}

	public static void DrawDecalsOnSingleSurface_QueueHelper(SurfaceHandle_t surfID, Vector3 modelOrg) {
		throw new NotImplementedException();
	}

	public static void DrawDecalsOnSingleSurface(IMatRenderContext renderContext, SurfaceHandle_t surfID) {
		throw new NotImplementedException();
	}

	public static void R_DrawDecalsAllImmediate_GatherDecals(IMatRenderContext renderContext, int group, int treeType, List<Decal> drawDecals) {
		throw new NotImplementedException();
	}

	public static void R_DrawDecalsAllImmediate_Gathered(IMatRenderContext renderContext, Span<Decal> decals, int decalCount, in Vector3 modelOrg, float fade) {
		throw new NotImplementedException();
	}

	public static void R_DrawDecalsAllImmediate(IMatRenderContext renderContext, int group, int treeType, in Vector3 modelOrg, int checkCount, float fade) {
		throw new NotImplementedException();
	}

	static void R_DrawDecalMeshList(ref DecalMeshList meshList) {
		throw new NotImplementedException();
	}

	public static void R_DrawDecalsAll_GatherDecals(IMatRenderContext renderContext, int group, int treeType, List<Decal> drawDecals) {
		throw new NotImplementedException();
	}

	public static void R_DecalsGetMaxMesh(IMatRenderContext renderContext, out int decalSortMaxVerts, out int decalSortMaxIndices) {
		throw new NotImplementedException();
	}

	public static void R_DrawDecalsAll_Gathered(IMatRenderContext renderContext, Span<Decal> decals, int decalCount, in Vector3 modelOrg, float fade) {
		throw new NotImplementedException();
	}

	public static void R_DrawDecalsAll(IMatRenderContext renderContext, int group, int treeType, in Vector3 modelOrg, int checkCount, float fade) {
		throw new NotImplementedException();
	}

	public static void DecalSurfaceDraw_NonQueued(IMatRenderContext renderContext, int renderGroup, in Vector3 modelOrg, int checkCount, float fade) {
		throw new NotImplementedException();
	}

	public static void DecalSurfaceDraw_QueueHelper(bool batched, int renderGroup, Vector3 modelOrg, int checkCount, Span<Decal> decals, int permanentLightmap, int lightmap, int nonLightmap, float fade) {
		throw new NotImplementedException();
	}

	public static void DecalSurfaceDraw(IMatRenderContext renderContext, int renderGroup, float fade = 1.0f) {
		throw new NotImplementedException();
	}

	public static void DecalSurfaceAdd(SurfaceHandle_t surfID, int group) {
		throw new NotImplementedException();
	}
}
