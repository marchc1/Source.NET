#define SPEW_DECALS

using CommunityToolkit.HighPerformance;

using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;

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
			Trees[sort] = new SortedSet<DecalMaterialSortData>(Comparer<DecalMaterialSortData>.Create((decal1, decal2) => Render.DecalSortTreeSortLessFunc(in decal1, in decal2) ? -1 : Render.DecalSortTreeSortLessFunc(in decal2, in decal1) ? 1 : 0));

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
		int nextIndex = Render.INVALID_CACHE_ENTRY;
		for (int cacheHandle = decal.CacheHandle; cacheHandle != Render.INVALID_CACHE_ENTRY; cacheHandle = nextIndex) {
			ref DecalCache cache = ref Cache[cacheHandle];
			nextIndex = GetIndex(ref cache, DecalIndexOrdinal.NextVertBlockIndex);
			Assert(GetIndex(ref cache, DecalIndexOrdinal.DecalIndex) == decal.DecalPool);
			FreeBlock(cacheHandle);
		}
		decal.CacheHandle = Render.INVALID_CACHE_ENTRY;
		decal.ClippedVertCount = 0;
	}

	public Span<DecalVert> GetCachedVerts(Decal decal) {
		throw new NotImplementedException();
	}

	public void Init() {
		FirstFree = 0;
		FreeTestIndex = 0;
		for (int i = 0; i < Render.DECALCACHE_ENTRY_COUNT; i++) {
			SetNext(i, i + 1);
			SetIndex(ref Cache[i], DecalIndexOrdinal.DecalIndex, -1);
			SetFree(i, true);
		}
		SetNext(Render.DECALCACHE_ENTRY_COUNT - 1, Render.INVALID_CACHE_ENTRY);
		FreeBlockCount = Render.DECALCACHE_ENTRY_COUNT;
	}

	int GetIndex(ref DecalCache block, DecalIndexOrdinal index) => block.DecalVert[(int)index].DecalIndex;

	void SetIndex(ref DecalCache block, DecalIndexOrdinal index, int value) => block.DecalVert[(int)index].DecalIndex = value;

	void SetNext(int cur, int next) => SetIndex(ref Cache[cur], DecalIndexOrdinal.NextVertBlockIndex, next);

	void SetFree(int block, bool free) => SetIndex(ref Cache[block], DecalIndexOrdinal.IsFreeIndex, free ? 1 : 0);

	bool IsFree(int block) => GetIndex(ref Cache[block], DecalIndexOrdinal.IsFreeIndex) != 0;

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

	// The pointer comparisons in Source order materials arbitrarily but consistently; materials are unique by name here
	static int MaterialOrder(IMaterial? material1, IMaterial? material2) {
		if (ReferenceEquals(material1, material2))
			return 0;
		if (material1 == null)
			return -1;
		if (material2 == null)
			return 1;

		return material1.GetName().CompareTo(material2.GetName(), StringComparison.Ordinal);
	}

	public static bool DecalSortTreeSortLessFunc(in DecalMaterialSortData decal1, in DecalMaterialSortData decal2) {
		if (decal1.LightmapPage == -1 || decal2.LightmapPage == -1)
			return MaterialOrder(decal1.Material, decal2.Material) < 0;

		if (MaterialOrder(decal1.Material, decal2.Material) == 0)
			return decal1.LightmapPage < decal2.LightmapPage;
		else
			return MaterialOrder(decal1.Material, decal2.Material) < 0;
	}

	[ConCommand("r_printdecalinfo")]
	static void r_printdecalinfo_f() {
		throw new NotImplementedException();
	}

	public static float ComputeDecalLightmapOffset(SurfaceHandle_t surfID) {
		float offset;
		ref BSPMSurface2 surf = ref ModelLoader.SurfaceHandleFromIndex(surfID);
		if ((ModelLoader.MSurf_Flags(ref surf) & SurfDraw.BumpLight) != 0) {
			SourceDllMain.materials.GetLightmapPageSize(MatSys.SortInfoToLightmapPage(ModelLoader.MSurf_MaterialSortID(ref surf)), out int width, out int height);
			int xExtent = ModelLoader.MSurf_LightmapExtents(ref surf)[0] + 1;

			offset = width != 0 ? (float)xExtent / (float)width : 0.0f;
		}
		else {
			offset = 0.0f;
		}
		return offset;
	}

	static VertexFormat GetUncompressedFormat(IMaterial material) {
		return material.GetVertexFormat(); // compressed todo?
	}

	public static void Shader_DecalDrawPoly(Span<DecalVert> v, IMaterial material, SurfaceHandle_t surfID, int vertCount, Decal decal, float fade) {
#if !SWDS
		throw new NotImplementedException();
#endif
	}

	public static void DecalGetMaterialAndSize(int decalIndex, out IMaterial? decalMaterial, out float w, out float h) {
		throw new NotImplementedException();
	}

#if !SWDS
	static Decal? MSurf_DecalPointer(SurfaceHandle_t surfID) {
		WorldDecalHandle_t handle = ModelLoader.MSurf_Decals(ref ModelLoader.SurfaceHandleFromIndex(surfID));
		if (handle == ModelLoader.WORLD_DECAL_HANDLE_INVALID)
			return null;

		return s_DecalPool[handle];
	}

	static WorldDecalHandle_t DecalToHandle(Decal? decal) {
		if (decal == null)
			return ModelLoader.WORLD_DECAL_HANDLE_INVALID;

		int decalIndex = decal.DecalPool;
		Assert(decalIndex >= 0 && decalIndex < g_MaxDecals);
		return (WorldDecalHandle_t)decalIndex;
	}

	public static void DecalInit() {
		g_MaxDecals = int.Parse(r_decals.GetDefault());
		g_MaxDecals = Math.Max(64, g_MaxDecals);
		Assert(g_DecalAllocator.Count() == 0);
		g_DynamicDecals = 0;
		g_StaticDecals = 0;
		g_LastReplacedDynamic = -1;

		s_DecalPool.Clear();
		s_DecalPool.SetSize(g_MaxDecals);

		if (SourceDllMain.host_state.WorldBrush != null) {
			for (int i = 0; i < SourceDllMain.host_state.WorldBrush.NumSurfaces; i++) {
				ref BSPMSurface2 surfID = ref ModelLoader.SurfaceHandleFromIndex(i);
				ModelLoader.MSurf_Decals(ref surfID) = ModelLoader.WORLD_DECAL_HANDLE_INVALID;
			}
		}

		for (int decal = 0; decal < g_MaxDecals; ++decal)
			s_DecalPool[decal] = null;

		g_DecalVertCache.Init();

		DecalSortInit();
	}

	public static void DecalTerm(WorldBrushData? brushData, bool termPermanentDecals) {
		throw new NotImplementedException();
	}

	public static void DecalTermAll() {
		s_DecalDestroyList = null;
		for (nint i = 0; i < s_DecalPool.Count; i++)
			DecalUnlink(s_DecalPool[(int)i], SourceDllMain.host_state.WorldBrush);
	}

	static void DecalCacheClear(Decal decal) {
		g_DecalVertCache.FreeCachedVerts(decal);
	}

	public static void DecalFlushDestroyList() {
		Decal? decal = s_DecalDestroyList;
		while (decal != null) {
			Decal? next = decal.DestroyList;
			DecalUnlink(decal, SourceDllMain.host_state.WorldBrush);
			decal = next;
		}
		s_DecalDestroyList = null;
	}

	static void DecalAddToDestroyList(Decal decal) {
		if (decal.DestroyList == null) {
			decal.DestroyList = s_DecalDestroyList;
			s_DecalDestroyList = decal;
		}
	}

	public static void DecalUnlink(Decal? decal, WorldBrushData? data) {
		if (decal == null)
			return;

		Decal? tmp;

		DecalCacheClear(decal);
		if (decal.SurfID != -1) {
			ref BSPMSurface2 surf = ref ModelLoader.SurfaceHandleFromIndex(decal.SurfID);
			if (MSurf_DecalPointer(decal.SurfID) == decal)
				ModelLoader.MSurf_Decals(ref surf) = DecalToHandle(decal.Next);
			else {
				tmp = MSurf_DecalPointer(decal.SurfID);
				if (tmp == null)
					Sys.Error("Bad decal list");
				while (tmp!.Next != null) {
					if (tmp.Next == decal) {
						tmp.Next = decal.Next;
						break;
					}
					tmp = tmp.Next;
				}
			}

			if (ModelLoader.SurfaceHasDispInfo(ref surf)) {
				IDispInfo? dispInfo = surf.DispInfo;

				dispInfo?.NotifyRemoveDecal(decal.DispDecal);
			}
		}

		decal.SurfID = -1;

		if ((decal.Flags & FDecal.Permanent) == 0) {
			--g_DynamicDecals;
			Assert(g_DynamicDecals >= 0);
		}
		else {
			--g_StaticDecals;
			Assert(g_StaticDecals >= 0);
		}

		Assert(s_DecalPool[decal.DecalPool] == decal);
		s_DecalPool[decal.DecalPool] = null;
		g_DecalAllocator.Free(decal);
	}

	public static int FindFreeDecalSlot() {
		for (int i = 0; i < g_MaxDecals; i++) {
			if (s_DecalPool[i] == null)
				return i;
		}
		return -1;
	}

#if SPEW_DECALS
	static bool spewdecals = true;

	public static void SpewDecals() {
		if (spewdecals) {
			spewdecals = false;

			for (int i = 0; i < g_MaxDecals; ++i) {
				Decal? decal = s_DecalPool[i];
				Assert(decal != null);
				if (decal != null) {
					bool permanent = (decal.Flags & FDecal.Permanent) != 0;
					Msg($"{i} == {decal.Material!.GetName()} on {(int)decal.EntityIndex} perm {(permanent ? 1 : 0)} at {decal.Position.X:F2} {decal.Position.Y:F2} {decal.Position.Z:F2} on surf {decal.SurfID} ({decal.Dx:F2} {decal.Dy:F2} {decal.Scale,2:F0})\n");
				}
			}
		}
	}
#endif

	public static int FindDynamicDecalSlot(int startAt) {
		if (startAt >= g_MaxDecals || startAt < 0)
			startAt = 0;

		int i = startAt;

		do {
			if (s_DecalPool[i] != null && (s_DecalPool[i]!.Flags & FDecal.Permanent) == 0 && (s_DecalPool[i]!.Flags & FDecal.PlayerSpray) == 0)
				return i;

			++i;

			if (i >= g_MaxDecals)
				i = 0;
		}
		while (i != startAt);

		DevMsg("R_FindDynamicDecalSlot: no slot available.\n");

#if SPEW_DECALS
		SpewDecals();
#endif

		return -1;
	}

	static bool WarningOnce = false;

	static Decal? DecalAlloc(FDecal flags) {
		bool permanent = (flags & FDecal.Permanent) != 0;

		int dynamicDecalLimit = Math.Min(r_decals.GetInt(), g_MaxDecals);

		int slot = -1;
		if (permanent || g_DynamicDecals < dynamicDecalLimit) {
			slot = FindFreeDecalSlot();
		}

		if (slot == -1) {
			slot = FindDynamicDecalSlot(g_LastReplacedDynamic + 1);
			if (slot == -1) {
				if (!WarningOnce) {
					DevWarning(1, $"Exceeded MAX_DECALS ({g_MaxDecals}).\n");
					WarningOnce = true;
				}
				slot = 0;
			}

			DecalUnlink(s_DecalPool[slot], SourceDllMain.host_state.WorldBrush);
			g_LastReplacedDynamic = slot;
		}

		Decal decal = g_DecalAllocator.Alloc();
		s_DecalPool[slot] = decal;
		decal.DestroyList = null;
		decal.DecalPool = (ushort)slot;
		decal.SurfID = -1;
		decal.CacheHandle = INVALID_CACHE_ENTRY;
		decal.ClippedVertCount = 0;

		if (!permanent) {
			++g_DynamicDecals;
		}
		else {
			++g_StaticDecals;
		}

		return decal;
	}

	public static void DecalSurface(SurfaceHandle_t surfID, DecalInfo decalInfo, bool forceForDisplacement) {
		ref BSPMSurface2 surf = ref ModelLoader.SurfaceHandleFromIndex(surfID);

		if (decalInfo.Normal != null) {
			if (Vector3.Dot(ModelLoader.MSurf_Plane(ref surf).Normal, decalInfo.Normal.Value) < 0.0f)
				return;
		}

		ref ModelTexInfo tex = ref ModelLoader.MSurf_TexInfo(ref surf);
		ref Vector4 textureU = ref tex.TextureVecsTexelsPerWorldUnits[0];
		ref Vector4 textureV = ref tex.TextureVecsTexelsPerWorldUnits[1];

		float s = Vector3.Dot(decalInfo.Position, textureU.AsVector3D()) + textureU.W - ModelLoader.MSurf_TextureMins(ref surf)[0];
		float t = Vector3.Dot(decalInfo.Position, textureV.AsVector3D()) + textureV.W - ModelLoader.MSurf_TextureMins(ref surf)[1];

		DecalComputeBasis(ModelLoader.MSurf_Plane(ref surf).Normal, (decalInfo.Flags & FDecal.UseSAxis) != 0 ? decalInfo.SAxis : null, decalInfo.Basis);

		float w = MathF.Abs(decalInfo.DecalWidth * Vector3.Dot(textureU.AsVector3D(), decalInfo.Basis[0])) + MathF.Abs(decalInfo.DecalHeight * Vector3.Dot(textureU.AsVector3D(), decalInfo.Basis[1]));
		float h = MathF.Abs(decalInfo.DecalWidth * Vector3.Dot(textureV.AsVector3D(), decalInfo.Basis[0])) + MathF.Abs(decalInfo.DecalHeight * Vector3.Dot(textureV.AsVector3D(), decalInfo.Basis[1]));

		s -= w * 0.5f;
		t -= h * 0.5f;

		if (!forceForDisplacement) {
			if (s <= -w || t <= -h ||
				s > ModelLoader.MSurf_TextureExtents(ref surf)[0] + w || t > ModelLoader.MSurf_TextureExtents(ref surf)[1] + h) {
				return;
			}
		}

		DecalCreate(decalInfo, surfID, s, t, forceForDisplacement);
	}

	static void DecalNodeSurfaces(BSPMNode node, DecalInfo decalInfo) {
		SurfaceHandle_t surfID = node.FirstSurface;
		for (int i = 0; i < node.NumSurfaces; ++i, ++surfID) {
			ref BSPMSurface2 surf = ref ModelLoader.SurfaceHandleFromIndex(surfID);
			if ((ModelLoader.MSurf_Flags(ref surf) & SurfDraw.NoDecals) != 0)
				continue;

			if (ModelLoader.SurfaceHasDispInfo(ref surf))
				continue;

			DecalSurface(surfID, decalInfo, false);
		}
	}

	public static void DecalLeaf(BSPMLeaf leaf, DecalInfo decalInfo) {
		Span<SurfaceHandle_t> handles = SourceDllMain.host_state.WorldBrush!.MarkSurfaces.AsSpan(leaf.FirstMarkSurface);
		for (int i = 0; i < leaf.NumMarkSurfaces; i++) {
			SurfaceHandle_t surfID = handles[i];
			ref BSPMSurface2 surf = ref ModelLoader.SurfaceHandleFromIndex(surfID);

			if ((ModelLoader.MSurf_Flags(ref surf) & (SurfDraw.Node | SurfDraw.NoDecals)) != 0)
				continue;

			if (decalInfo.ApplySurfs.IndexOf(surfID) != -1)
				continue;

			Assert(!ModelLoader.SurfaceHasDispInfo(ref surf));

			float dist = MathF.Abs(Vector3.Dot(decalInfo.Position, ModelLoader.MSurf_Plane(ref surf).Normal) - ModelLoader.MSurf_Plane(ref surf).Dist);
			if (dist < DECAL_DISTANCE) {
				DecalSurface(surfID, decalInfo, false);
			}
		}

		for (int i = 0; i < leaf.DispCount; i++) {
			IDispInfo dispInfo = DispInfo.MLeaf_Disaplcement(leaf, i)!;

			SurfaceHandle_t surfID = (int)dispInfo.GetParent().SurfNum;

			if ((ModelLoader.MSurf_Flags(ref ModelLoader.SurfaceHandleFromIndex(surfID)) & SurfDraw.NoDecals) != 0)
				continue;

			if (dispInfo.GetTag())
				continue;

			dispInfo.SetTag();

			dispInfo.GetBoundingBox(out Vector3 bbMin, out Vector3 bbMax);
			if (decalInfo.Position.X - decalInfo.Size < bbMax.X && decalInfo.Position.X + decalInfo.Size > bbMin.X &&
				decalInfo.Position.Y - decalInfo.Size < bbMax.Y && decalInfo.Position.Y + decalInfo.Size > bbMin.Y &&
				decalInfo.Position.Z - decalInfo.Size < bbMax.Z && decalInfo.Position.Z + decalInfo.Size > bbMin.Z) {
				DecalSurface((int)dispInfo.GetParent().SurfNum, decalInfo, true);
			}
		}
	}

	static void DecalNode(BSPMNode? node, DecalInfo decalInfo) {
		CollisionPlane splitplane;
		float dist;

		if (node == null)
			return;
		if (node.Contents >= 0) {
			DecalLeaf((BSPMLeaf)node, decalInfo);
			return;
		}

		splitplane = node.Plane;
		dist = Vector3.Dot(decalInfo.Position, splitplane.Normal) - splitplane.Dist;

		if (dist > decalInfo.Size)
			DecalNode(node.Children[0], decalInfo);
		else if (dist < -decalInfo.Size)
			DecalNode(node.Children[1], decalInfo);
		else {
			if (dist < DECAL_DISTANCE && dist > -DECAL_DISTANCE)
				DecalNodeSurfaces(node, decalInfo);

			DecalNode(node.Children[0], decalInfo);
			DecalNode(node.Children[1], decalInfo);
		}
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

	static bool DecalUnProject(Decal decal, ref DecalList entry) {
		throw new NotImplementedException();
	}

	static void DecalShoot_(IMaterial? material, int entity, Model? model, in Vector3 position, Vector3? saxis, FDecal flags, in Color rgbaColor, Vector3? normal, object? userData = null) {
		DecalInfo decalInfo = new();
		decalInfo.FadeDuration = 0;
		decalInfo.FadeStartTime = 0;

		decalInfo.Position = position;

		if (model == null || model.Type != ModelType.Brush || material == null)
			return;

		decalInfo.Model = model;
		decalInfo.Brush = model.Brush.Shared;

		if (saxis != null) {
			flags |= FDecal.UseSAxis;
			decalInfo.SAxis = saxis.Value;
		}

		decalInfo.Material = material;
		decalInfo.UserData = userData;

		decalInfo.Flags = flags;
		decalInfo.Entity = entity;
		decalInfo.Size = (int)material.GetMappingWidth() >> 1;
		if (((int)material.GetMappingHeight() >> 1) > decalInfo.Size)
			decalInfo.Size = (int)material.GetMappingHeight() >> 1;

		IMaterialVar decalScaleVar = decalInfo.Material.FindVar("$decalScale", out bool found, false);
		if (found) {
			decalInfo.Scale = 1.0f / decalScaleVar.GetFloatValue();
			decalInfo.Size *= decalScaleVar.GetFloatValue();
		}
		else {
			decalInfo.Scale = 1.0f;
		}

		decalInfo.DecalWidth = (int)(material.GetMappingWidth() / decalInfo.Scale);
		decalInfo.DecalHeight = (int)(material.GetMappingHeight() / decalInfo.Scale);
		decalInfo.Color = rgbaColor;
		decalInfo.Normal = normal;
		decalInfo.ApplySurfs.Clear();

		DispInfo.DispInfo_ClearAllTags(decalInfo.Brush!.DispInfos);

		BSPMNode nodes = decalInfo.Brush.Nodes![decalInfo.Model.Brush.FirstNode];
		DecalNode(nodes, decalInfo);
	}

	public static void DecalShoot(int textureIndex, int entity, Model? model, in Vector3 position, Vector3? saxis, FDecal flags, in Color rgbaColor, Vector3? normal) {
		IMaterial? material = Draw_DecalMaterial(textureIndex);
		DecalShoot_(material, entity, model, in position, saxis, flags, in rgbaColor, normal);
	}

	public static void PlayerDecalShoot(IMaterial material, object? userData, int entity, Model? model, in Vector3 position, Vector3? saxis, FDecal flags, in Color rgbaColor) {
		Assert(userData != null);

		List<Decal> decalVec = [];

		for (nint i = 0; i < s_DecalPool.Count; i++) {
			Decal? decal = s_DecalPool[(int)i];

			if (decal != null && (decal.Flags & FDecal.PlayerSpray) != 0 && decal.UserData == userData)
				decalVec.Add(decal);
		}

		for (nint i = 0; i < decalVec.Count; i++)
			DecalUnlink(decalVec[(int)i], SourceDllMain.host_state.WorldBrush);

		flags |= FDecal.PlayerSpray;

		DecalShoot_(material, entity, model, in position, saxis, flags, in rgbaColor, null, userData);
	}

	static void DecalVertsLight(Span<DecalVert> v, in DecalContext context, SurfaceHandle_t surfID, int vertCount) {
		throw new NotImplementedException();
	}

	static Decal? DecalFindOverlappingDecals(DecalInfo decalInfo, SurfaceHandle_t surfID) {
		Decal? last = null;
		IMaterial? material = decalInfo.Material;
		int count = 0;
		ref BSPMSurface2 surf = ref ModelLoader.SurfaceHandleFromIndex(surfID);

		Span<int> mapSize = [(int)material!.GetMappingWidth(), (int)material.GetMappingHeight()];
		Span<Vector3> decalExtents = stackalloc Vector3[2];
		float minProjectedWidth = mapSize[0] / decalInfo.Scale * 0.5f;
		decalExtents[0] = decalInfo.Basis[0] * minProjectedWidth;
		decalExtents[1] = decalInfo.Basis[1] * (mapSize[1] / decalInfo.Scale) * 0.5f;

		float areaThreshold = r_decal_overlap_area.GetFloat();
		float lastArea = 0;
		bool fullMatch = false;
		Decal? decal = MSurf_DecalPointer(surfID);
		List<Decal> coveredList = [];
		while (decal != null) {
			material = decal.Material;

			if ((decal.Flags & FDecal.Permanent) == 0 &&
				(decal.Flags & FDecal.PlayerSpray) == 0 && material != null) {
				Span<Vector3> testBasis = stackalloc Vector3[3];
				Span<float> testWorldScale = stackalloc float[2];
				SetupDecalTextureSpaceBasis(decal, ref ModelLoader.MSurf_Plane(ref surf).Normal, material, testBasis, testWorldScale);

				Vector2 decalMin = new(Vector3.Dot(decalInfo.Position - decalExtents[0], testBasis[0]) - decal.Dx + 0.5f, Vector3.Dot(decalInfo.Position - decalExtents[1], testBasis[1]) - decal.Dy + 0.5f);
				Vector2 decalMax = new(Vector3.Dot(decalInfo.Position + decalExtents[0], testBasis[0]) - decal.Dx + 0.5f, Vector3.Dot(decalInfo.Position + decalExtents[1], testBasis[1]) - decal.Dy + 0.5f);
				Vector2 unionMin = new(MathF.Max(decalMin.X, 0.0f), MathF.Max(decalMin.Y, 0.0f));
				Vector2 unionMax = new(MathF.Min(decalMax.X, 1.0f), MathF.Min(decalMax.Y, 1.0f));

				float projectWidthTestedDecal = decal.Material!.GetMappingWidth() / decal.Scale;

				float sizex = unionMax.X - unionMin.X;
				float sizey = unionMax.Y - unionMin.Y;
				if (sizex >= 0 && sizey >= 0) {
					float area = sizex * sizey;

					if (projectWidthTestedDecal < minProjectedWidth) {
						if (area > 0.999f)
							coveredList.Add(decal);
					}
					else {
						if (area > areaThreshold) {
							float areaScaled = area * projectWidthTestedDecal;
							count++;
							if (last == null || areaScaled > lastArea) {
								last = decal;
								lastArea = areaScaled;
								fullMatch = area >= 0.9f;
							}
						}
					}
				}
			}

			decal = decal.Next;
		}

		if (last != null) {
			if (count < r_decal_overlap_count.GetInt() && !fullMatch)
				last = null;
		}
		if (coveredList.Count > r_decal_cover_count.GetInt()) {
			nint lastCovered = coveredList.Count - r_decal_cover_count.GetInt();
			for (nint i = 0; i < lastCovered; i++)
				DecalUnlink(coveredList[(int)i], SourceDllMain.host_state.WorldBrush);
		}

		return last;
	}

	static void AddDecalToSurface(Decal decal, SurfaceHandle_t surfID, DecalInfo decalInfo) {
		ref BSPMSurface2 surf = ref ModelLoader.SurfaceHandleFromIndex(surfID);

		decal.Next = null;
		Decal? old = MSurf_DecalPointer(surfID);
		if (old != null) {
			while (old.Next != null)
				old = old.Next;
			old.Next = decal;
		}
		else
			ModelLoader.MSurf_Decals(ref surf) = DecalToHandle(decal);

		decal.SurfID = surfID;
		decal.Size = decalInfo.Size;
		decal.LightmapOffset = ComputeDecalLightmapOffset(surfID);

		if (ModelLoader.SurfaceHasDispInfo(ref surf))
			decal.DispDecal = surf.DispInfo!.NotifyAddDecal(DecalToHandle(decal), decalInfo.Size);

		decalInfo.ApplySurfs.Add(surfID);
	}

	public static void DecalSortInit() {
		g_DecalFormats.Clear();

		g_DecalSortTrees.Clear();
		g_DecalSortPool.Clear();
		g_DecalSortPool.EnsureCapacity(g_MaxDecals);
		g_DecalSortPool.SetGrowSize(128);
		g_DecalSortCheckCount = 0;
		g_BrushModelDecalSortCheckCount = 0;

		g_DispDecalSortTrees.Clear();
		g_DispDecalSortPool.Clear();
		g_DispDecalSortPool.EnsureCapacity(g_MaxDecals);
		g_DispDecalSortPool.SetGrowSize(128);
		g_DispDecalSortCheckCount = 0;
	}

	public static void DecalSurfacesInit(bool brushModel) {
		throw new NotImplementedException();
	}

	static void DecalMaterialSort(Decal decal, SurfaceHandle_t surfID) {
		DecalMaterialSortData sort = default;
		if (decal.Material!.InMaterialPage())
			sort.Material = decal.Material.GetMaterialPage();
		else
			sort.Material = decal.Material;
		sort.LightmapPage = MatSys.MaterialSortInfoArray![ModelLoader.MSurf_MaterialSortID(ref ModelLoader.SurfaceHandleFromIndex(surfID))].LightmapPageID;

		VertexFormat vertexFormat = GetUncompressedFormat(sort.Material!);
		nint format = 0;
		nint formatCount = g_DecalFormats.Count;
		for (; format < formatCount; ++format) {
			if (g_DecalFormats[(int)format].VertexFormat == vertexFormat)
				break;
		}

		if (format == formatCount) {
			format = g_DecalFormats.Count;
			g_DecalFormats.Add(new DecalSortVertexFormat() { VertexFormat = vertexFormat });
			nint sortTreeIndex = g_DecalSortTrees.Count;
			g_DecalSortTrees.Add(new DecalSortTrees());
			g_DispDecalSortTrees.Add(new DecalSortTrees());
			g_DecalFormats[(int)format] = g_DecalFormats[(int)format] with { SortTree = sortTreeIndex };
		}

		nint iSortTree = g_DecalFormats[(int)format].SortTree;
		int treeType;

		if (sort.Material!.GetPropertyFlag(MaterialPropertyTypes.NeedsLightmap)) {
			if ((decal.Flags & FDecal.Permanent) != 0)
				treeType = (int)DecalSortType.PermanentLightmap;
			else
				treeType = (int)DecalSortType.Lightmap;
		}
		else {
			treeType = (int)DecalSortType.NonLightmap;
			sort.LightmapPage = -1;
		}

		ref DecalSortTrees decalSortTree = ref g_DecalSortTrees.AsSpan()[(int)iSortTree];
		ref DecalSortTrees dispDecalSortTree = ref g_DispDecalSortTrees.AsSpan()[(int)iSortTree];

		if (!decalSortTree.Trees[treeType].TryGetValue(sort, out DecalMaterialSortData found)) {
			List<DecalMaterialBucket> decalSortBucket = decalSortTree.DecalSortBuckets[0][treeType];

			nint bucket = decalSortBucket.Count;
			decalSortBucket.Add(new() { CheckCount = -1 });

			List<DecalMaterialBucket> dispDecalSortBucket = dispDecalSortTree.DecalSortBuckets[0][treeType];
			dispDecalSortBucket.Add(new() { CheckCount = -1 });

			for (int group = 1; group < (int)MatSortGroup.Max + 1; ++group) {
				List<DecalMaterialBucket> decalSortBucketGroup = decalSortTree.DecalSortBuckets[group][treeType];
				decalSortBucketGroup.Add(new() { CheckCount = -1 });

				List<DecalMaterialBucket> dispDecalSortBucketGroup = dispDecalSortTree.DecalSortBuckets[group][treeType];
				dispDecalSortBucketGroup.Add(new() { CheckCount = -1 });
			}

			sort.Bucket = bucket;
			decalSortTree.Trees[treeType].Add(sort);
			dispDecalSortTree.Trees[treeType].Add(sort);

			decal.SortTree = iSortTree;
			decal.SortMaterial = sort.Bucket;
		}
		else {
			decal.SortTree = iSortTree;
			decal.SortMaterial = found.Bucket;
		}
	}

	public static void DecalReSortMaterials() {
		throw new NotImplementedException();
	}

	static void DecalCreate(DecalInfo decalInfo, SurfaceHandle_t surfID, float x, float y, bool forceForDisplacement) {
		Decal? decal;

		if (surfID == -1) {
			ConMsg("psurface NULL in R_DecalCreate!\n");
			return;
		}

		Decal? old = DecalFindOverlappingDecals(decalInfo, surfID);
		if (old != null) {
			DecalUnlink(old, SourceDllMain.host_state.WorldBrush);
			old = null;
		}

		decal = DecalAlloc(decalInfo.Flags);
		decal!.Flags = decalInfo.Flags;
		decal.Color = decalInfo.Color;
		decal.Position = decalInfo.Position;
		if ((decal.Flags & FDecal.UseSAxis) != 0)
			decal.SAxis = decalInfo.SAxis;
		decal.Dx = x;
		decal.Dy = y;
		decal.Material = decalInfo.Material;
		Assert(decal.Material != null);
		decal.UserData = decalInfo.UserData;

		decal.Scale = decalInfo.Scale;
		decal.EntityIndex = (short)decalInfo.Entity;

		if (decalInfo.FadeDuration > 0.0f) {
			decal.Flags |= FDecal.Dynamic;
			decal.FadeDuration = decalInfo.FadeDuration;
			decal.FadeStartTime = decalInfo.FadeStartTime;
			decal.FadeStartTime += cl.GetTime();
		}

		if ((decal.Flags & FDecal.PlayerSpray) != 0) {
			decal.FadeStartTime = 0.0f;
			decal.Scale = 1.0f;
		}

		if (!forceForDisplacement) {
			DecalVertsClip(default, decal, surfID, decalInfo.Material!);
			if (decal.ClippedVertCount == 0) {
				DecalUnlink(decal, SourceDllMain.host_state.WorldBrush);
				return;
			}
		}

		AddDecalToSurface(decal, surfID, decalInfo);

		DecalMaterialSort(decal, surfID);
	}

	public static bool DecalUpdate(Decal decal) {
		throw new NotImplementedException();
	}

	public static Span<DecalVert> DecalSetupVerts(ref DecalContext context, Decal decal, SurfaceHandle_t surfID, IMaterial material) {
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

	public static void DrawDecalsAllImmediate_GatherDecals(IMatRenderContext renderContext, int group, int treeType, List<Decal> drawDecals) {
		throw new NotImplementedException();
	}

	public static void DrawDecalsAllImmediate_Gathered(IMatRenderContext renderContext, Span<Decal> decals, int decalCount, in Vector3 modelOrg, float fade) {
		throw new NotImplementedException();
	}

	public static void DrawDecalsAllImmediate(IMatRenderContext renderContext, int group, int treeType, in Vector3 modelOrg, int checkCount, float fade) {
		throw new NotImplementedException();
	}

	static void DrawDecalMeshList(ref DecalMeshList meshList) {
		throw new NotImplementedException();
	}

	public static void DrawDecalsAll_GatherDecals(IMatRenderContext renderContext, int group, int treeType, List<Decal> drawDecals) {
		throw new NotImplementedException();
	}

	public static void DecalsGetMaxMesh(IMatRenderContext renderContext, out int decalSortMaxVerts, out int decalSortMaxIndices) {
		throw new NotImplementedException();
	}

	public static void DrawDecalsAll_Gathered(IMatRenderContext renderContext, Span<Decal> decals, int decalCount, in Vector3 modelOrg, float fade) {
		throw new NotImplementedException();
	}

	public static void DrawDecalsAll(IMatRenderContext renderContext, int group, int treeType, in Vector3 modelOrg, int checkCount, float fade) {
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
#endif
}
