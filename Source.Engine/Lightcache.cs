global using AmbientCube = Source.InlineArray6<System.Numerics.Vector3>;

using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Engine;

public struct LightingState
{
	public AmbientCube BoxColor;
	public int NumLights;

	public InlineArrayMaxLocalLights<BSPDWorldLightPtr> LocalLight;

	public void ZeroLightingState() {
		for (int i = 0; i < 6; i++)
			BoxColor[i].Init();
		NumLights = 0;
	}

	public bool HasAmbientColors() {
		for (int i = 0; i < 6; i++)
			if (!BoxColor[i].IsZero(1e-4f))
				return true;
		return false;
	}

	public void AddLocalLight(in BSPDWorldLightPtr localLight) {
		if (NumLights >= Render.MAXLOCALLIGHTS)
			return;

		for (int i = 0; i < NumLights; i++)
			if (LocalLight[i] == localLight)
				return;

		LocalLight[NumLights] = localLight;
		NumLights++;
	}

	public void AddAllLocalLights(in LightingState src) {
		for (int i = 0; i < src.NumLights; i++)
			AddLocalLight(src.LocalLight[i]);
	}

	public void CopyLocalLights(in LightingState src) {
		NumLights = src.NumLights;
		for (int i = 0; i < src.NumLights; i++)
			LocalLight[i] = src.LocalLight[i];
	}
}
[InlineArray(Render.MAXLOCALLIGHTS)] public struct InlineArrayMaxLocalLights<T> { public T item; }

[Flags]
public enum LightCacheFlags
{
	Static = 0x1,
	Dynamic = 0x2,
	LightStyle = 0x4,
	AllowFast = 0x8,
}

public struct LightcacheGetDynamic_Stats
{
	public bool HasNonSwitchableLightStyles;
	public bool HasSwitchableLightStyles;
	public bool HasDLights;
	public bool NeedsSwitchableLightStyleUpdate;
}

public enum HackLightCacheFlags
{
	HasSwitchableLightStyle = 0x1,
	HasNonSwitchableLightStyle = 0x2,
	HasDoneStaticLighting = 0x4
}

public class LightingStateInfo
{
	public InlineArrayMaxLocalLights<float> Illum;
	public bool LightingStateHasSkylight;

	public LightingStateInfo() {
		LightingStateHasSkylight = false;
		Clear();
	}

	public void Clear() {
		for (int i = 0; i < Render.MAXLOCALLIGHTS; i++)
			Illum[i] = 0;
		LightingStateHasSkylight = false;
	}

	public void CopyFrom(LightingStateInfo src) {
		for (int i = 0; i < Render.MAXLOCALLIGHTS; i++)
			Illum[i] = src.Illum[i];
		LightingStateHasSkylight = src.LightingStateHasSkylight;
	}
}

[InlineArray(BaseLightCache.MAX_LIGHTSTYLE_BYTES)] public struct InlineArrayLightstyleBytes { public byte item; }

public class BaseLightCache : LightingStateInfo
{
	public const int MAX_LIGHTSTYLE_BITS = BSPFileCommon.MAX_LIGHTSTYLES;
	public const int MAX_LIGHTSTYLE_BYTES = (MAX_LIGHTSTYLE_BITS + 7) / 8;

	public BaseLightCache() {
		LightingOrigin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		LastFrameUpdatedLightStyles = -1;
		LastFrameUpdatedDynamicLighting = -1;
		LightingFlags = 0;
		Leaf = -1;
		EnvCubemapTexture = null;
	}

	public bool HasLightStyle() => (LightingFlags & (int)(HackLightCacheFlags.HasSwitchableLightStyle | HackLightCacheFlags.HasNonSwitchableLightStyle)) != 0;
	public bool HasSwitchableLightStyle() => (LightingFlags & (int)HackLightCacheFlags.HasSwitchableLightStyle) != 0;
	public bool HasNonSwitchableLightStyle() => (LightingFlags & (int)HackLightCacheFlags.HasNonSwitchableLightStyle) != 0;

	public LightingState StaticLightingState;
	public LightingState LightStyleLightingState;
	public int LastFrameUpdatedLightStyles;
	public LightingState DynamicLightingState;
	public int LastFrameUpdatedDynamicLighting;
	public int LightingFlags;
	public int Leaf;
	public InlineArrayLightstyleBytes Lightstyles;
	public Vector3 LightingOrigin;
	public ITexture? EnvCubemapTexture;
}

public class LightCache : BaseLightCache
{
	public LightCache() {
		StaticPrecalcNumLocalLights = 0;
		Next = Bucket = 0xFFFF;
		LruPrev = LruNext = 0xFFFF;
		X = Y = Z = int.MinValue;
	}

	public InlineArrayMaxLocalLights<BSPDWorldLightPtr> StaticPrecalcLocalLight;
	public ushort StaticPrecalcNumLocalLights;
	public LightingStateInfo StaticPrecalcLightingStateInfo = new();
	public ushort Next;
	public ushort Bucket;
	public ushort LruPrev;
	public ushort LruNext;
	public int X, Y, Z;
	public int Index;
}

public partial class Render
{
	public const int MAXLOCALLIGHTS = 4;
	public static readonly ConVar r_lightcache_zbuffercache = new("r_lightcache_zbuffercache", "0", 0);
	public static readonly ConVar r_radiosity = new("r_radiosity", "4", FCvar.Cheat, "0: no radiosity\n1: radiosity with ambient cube (6 samples)\n2: radiosity with 162 samples\n3: 162 samples for static props, 6 samples for everything else");
	public static readonly ConVar r_ambientlightingonly = new("r_ambientlightingonly", "0", FCvar.Cheat, "Set this to 1 to light models with only ambient lighting (and no static lighting).");
	public static readonly ConVar r_worldlights = new("r_worldlights", "4", 0, "number of world lights to use per vertex");
	public static readonly ConVar r_avglight = new("r_avglight", "1", FCvar.Cheat);
	public static readonly ConVar r_lightcachecenter = new("r_lightcachecenter", "1", FCvar.Cheat);
	public static readonly ConVar r_lightcache_numambientsamples = new("r_lightcache_numambientsamples", "162", FCvar.Cheat, "number of random directions to fire rays when computing ambient lighting");
	public static readonly ConVar lightcache_maxmiss = new("lightcache_maxmiss", "2", FCvar.Cheat);

	private const int MAX_CACHE_ENTRY = 200;
	private const int MAX_CACHE_BUCKETS = MAX_CACHE_ENTRY;
	private const int HASH_GRID_SIZEX = 5;
	private const int HASH_GRID_SIZEY = 5;
	private const int HASH_GRID_SIZEZ = 7;
	private const float LIGHTCACHE_SNAP_EPSILON = 0.5f;
	private const int LIGHT_LRU_HEAD_INDEX = MAX_CACHE_ENTRY;
	private const int LIGHT_LRU_TAIL_INDEX = MAX_CACHE_ENTRY + 1;

	[InlineArray(MAX_CACHE_ENTRY + 2)] private struct LightCacheArray { public LightCache Item; }
	[InlineArray(MAX_CACHE_BUCKETS)] private struct LightBucketArray { public ushort Item; }

	private static LightCacheArray LightCaches;
	private static LightBucketArray LightBuckets;

	private static int CachedRWorldLights = -1;
	private static int CachedRRadiosity = -1;
	private static int CachedRAvgLight = -1;
	private static int CachedMatFullbright = -1;
	private static int CachedRLightcacheNumAmbientSamples = -1;

	private static byte FrameMissCount = 0;
	private static int FrameIndex = 0;
	public static int r_framecount = 0;

	private static ushort GetLightCacheIndex(LightCache cache) => (ushort)cache.Index;
	private static LightCache GetLightLRUHead() => LightCaches[LIGHT_LRU_HEAD_INDEX];
	private static LightCache GetLightLRUTail() => LightCaches[LIGHT_LRU_TAIL_INDEX];

	public void R_StudioInitLightingCache() {
		int i;

		for (i = 0; i < MAX_CACHE_ENTRY + 2; i++)
			LightCaches[i] = new() { Index = i };

		for (i = 0; i < MAX_CACHE_ENTRY + 2; i++)
			LightCaches[i].Bucket = 0xFFFF;

		for (i = 0; i < MAX_CACHE_BUCKETS; i++)
			LightBuckets[i] = 0xFFFF;

		ushort last = LIGHT_LRU_HEAD_INDEX;
		for (i = 0; i < MAX_CACHE_ENTRY - 1; i++) {
			LightCaches[i].LruPrev = last;
			LightCaches[i].LruNext = (ushort)(i + 1);
			last = (ushort)i;
		}
		LightCaches[i].LruPrev = last;
		LightCaches[i].LruNext = LIGHT_LRU_TAIL_INDEX;

		LightCaches[LIGHT_LRU_HEAD_INDEX].LruNext = 0;
		LightCaches[LIGHT_LRU_TAIL_INDEX].LruPrev = (ushort)i;

		if (HardwareConfig.MaxNumLights() < r_worldlights.GetInt())
			r_worldlights.SetValue(HardwareConfig.MaxNumLights());

		CachedRWorldLights = r_worldlights.GetInt();
		CachedRRadiosity = r_radiosity.GetInt();
		CachedRAvgLight = r_avglight.GetInt();
		CachedMatFullbright = MatSysInterface.MaterialSystemConfig.Fullbright;
		CachedRLightcacheNumAmbientSamples = r_lightcache_numambientsamples.GetInt();

		InvalidateStaticLightingCache();
	}

	private static void LightcacheMark(LightCache cache) {
		if (cache.LruNext == 0 && cache.LruPrev == 0)
			return;

		if (GetLightCacheIndex(cache) == LightCaches[LIGHT_LRU_TAIL_INDEX].LruPrev)
			return;

		LightCaches[cache.LruPrev].LruNext = cache.LruNext;
		LightCaches[cache.LruNext].LruPrev = cache.LruPrev;

		LightCaches[GetLightLRUTail().LruPrev].LruNext = GetLightCacheIndex(cache);
		cache.LruPrev = GetLightLRUTail().LruPrev;

		cache.LruNext = LIGHT_LRU_TAIL_INDEX;
		GetLightLRUTail().LruPrev = GetLightCacheIndex(cache);
	}

	private static void LightcacheUnlink(LightCache cache) {
		ushort bucket = cache.Bucket;

		if (bucket == 0xFFFF)
			return;

		ushort cacheIndex = GetLightCacheIndex(cache);

		ushort list = LightBuckets[bucket];

		if (list == cacheIndex)
			LightBuckets[bucket] = cache.Next;
		else {
			bool found = false;
			while (list != 0xFFFF) {
				if (LightCaches[list].Next == cacheIndex) {
					LightCaches[list].Next = cache.Next;
					found = true;
					break;
				}
				list = LightCaches[list].Next;
			}
			Assert(found);
		}
	}

	private static LightCache LightcacheGetLRU() {
		LightCache cache = LightCaches[GetLightLRUHead().LruNext];

		LightcacheMark(cache);

		LightcacheUnlink(cache);

		cache.Leaf = -1;
		return cache;
	}

	private static int LightcacheHashKey(int x, int y, int z, int leaf) {
		uint key = (uint)(((x << 20) + (y << 8) + z) ^ leaf);
		key %= MAX_CACHE_BUCKETS;
		return (int)key;
	}

	private static LightCache? FindInCache(int bucket, int x, int y, int z, int leaf) {
		ushort cacheIndex;
		for (cacheIndex = LightBuckets[bucket]; cacheIndex != 0xFFFF; cacheIndex = LightCaches[cacheIndex].Next) {
			LightCache cache = LightCaches[cacheIndex];

			if (cache.X == x && cache.Y == y && cache.Z == z && cache.Leaf == leaf)
				return cache;
		}
		return null;
	}

	private static void LinkToBucket(int bucket, LightCache cache) {
		cache.Next = LightBuckets[bucket];
		LightBuckets[bucket] = GetLightCacheIndex(cache);

		cache.Bucket = (ushort)bucket;
	}

	private static LightCache NewLightcacheEntry(int bucket) {
		LightCache cache = LightcacheGetLRU();
		LinkToBucket(bucket, cache);
		return cache;
	}

	private static void ComputeLightcacheBounds(in Vector3 vecOrigin, out Vector3 mins, out Vector3 maxs) {
		bool bXPos = vecOrigin[0] >= 0;
		bool bYPos = vecOrigin[1] >= 0;
		bool bZPos = vecOrigin[2] >= 0;

		int ix = ((int)MathF.Abs(vecOrigin[0])) >> HASH_GRID_SIZEX;
		int iy = ((int)MathF.Abs(vecOrigin[1])) >> HASH_GRID_SIZEY;
		int iz = ((int)MathF.Abs(vecOrigin[2])) >> HASH_GRID_SIZEZ;

		mins = default;
		maxs = default;
		mins.X = (bXPos ? ix : -(ix + 1)) << HASH_GRID_SIZEX;
		mins.Y = (bYPos ? iy : -(iy + 1)) << HASH_GRID_SIZEY;
		mins.Z = (bZPos ? iz : -(iz + 1)) << HASH_GRID_SIZEZ;

		maxs.X = mins.X + (1 << HASH_GRID_SIZEX);
		maxs.Y = mins.Y + (1 << HASH_GRID_SIZEY);
		maxs.Z = mins.Z + (1 << HASH_GRID_SIZEZ);

		Assert((mins.X <= vecOrigin.X) && (mins.Y <= vecOrigin.Y) && (mins.Z <= vecOrigin.Z));
		Assert((maxs.X >= vecOrigin.X) && (maxs.Y >= vecOrigin.Y) && (maxs.Z >= vecOrigin.Z));
	}

	private static void OriginToCacheOrigin(in Vector3 origin, out int x, out int y, out int z) {
		x = ((int)origin[0] + 32768) >> HASH_GRID_SIZEX;
		y = ((int)origin[1] + 32768) >> HASH_GRID_SIZEY;
		z = ((int)origin[2] + 32768) >> HASH_GRID_SIZEZ;
	}

	private static bool AllowFullCacheMiss(LightCacheFlags flags) {
		if (r_framecount < 60 || r_framecount != FrameIndex) {
			FrameMissCount = 0;
			FrameIndex = r_framecount;
		}
		if (FrameMissCount < lightcache_maxmiss.GetInt()) {
			FrameMissCount++;
			return true;
		}

		if ((flags & LightCacheFlags.AllowFast) != 0)
			return false;

		return true;
	}

	private static LightCache? FindNearestCache(int x, int y, int z, int leafIndex) {
		int bestDist = int.MaxValue;
		LightCache? best = null;
		ushort current = GetLightLRUTail().LruPrev;
		while (current != LIGHT_LRU_HEAD_INDEX) {
			LightCache cache = LightCaches[current];
			int dist = 0;
			int dx = Math.Abs(cache.X - x);
			int dy = Math.Abs(cache.Y - y);
			int dz = Math.Abs(cache.Z - z);
			if (leafIndex != cache.Leaf)
				dist += 2;
			dist = Math.Max(dist, dx);
			dist = Math.Max(dist, dy);
			dist = Math.Max(dist, dz);
			if (dist < bestDist) {
				best = cache;
				bestDist = dist;
				if (dist <= 1)
					break;
			}
			current = cache.LruPrev;
		}
		return best;
	}

	private static void ComputeAmbientFromLeaf(in Vector3 start, int leafID, Span<Vector3> lightBoxColor, ref bool addedLeafAmbientCube) {
		if (leafID >= 0) {
			ModelLoader.Mod_LeafAmbientColorAtPos(lightBoxColor, start, leafID);
			addedLeafAmbientCube = true;
		}
		else {
			for (int i = 0; i < 6; i++)
				lightBoxColor[i].Init(0.0f, 0.0f, 0.0f);
		}
	}

	private static void R_StudioGetAmbientLightForPoint(int leafID, in Vector3 start, Span<Vector3> lightBoxColor, bool isStaticProp, out bool addedLeafAmbientCube) {
		addedLeafAmbientCube = false;

		int i;
		if (MatSysInterface.MaterialSystemConfig.Fullbright == 1) {
			for (i = studiorender.GetNumAmbientLightSamples(); --i >= 0;)
				lightBoxColor[i].Init(1.0f, 1.0f, 1.0f);
			return;
		}

		switch (r_radiosity.GetInt()) {
			case 1:
				// todo: ComputeAmbientFromAxisAlignedSamples
				break;
			case 2:
				// todo: ComputeAmbientFromSphericalSamples
				break;
			case 3:
				// todo: ComputeAmbientFromSphericalSamples / ComputeAmbientFromAxisAlignedSamples
				break;
			case 4:
				if (isStaticProp) {
					// todo: ComputeAmbientFromSphericalSamples
				}
				else
					ComputeAmbientFromLeaf(start, leafID, lightBoxColor, ref addedLeafAmbientCube);
				break;
			default:
				for (i = 6; --i >= 0;)
					lightBoxColor[i].Init(0.0f, 0.0f, 0.0f);
				break;
		}
	}


	private static bool IsCachedLightStylesValid(LightCache cache) {
		// todo
		return false;
	}

	private static void AdjustLightCacheOrigin(LightCache cache, in Vector3 origin, int originLeaf) {
		ComputeLightcacheBounds(origin, out Vector3 cacheMins, out Vector3 cacheMaxs);
		Vector3 center = cacheMins + cacheMaxs;
		center *= 0.5f;

		// todo
		cache.LightingOrigin = origin;
	}

	private ITexture? FindEnvCubemapForPoint(in Vector3 origin) {
		WorldBrushData? brushData = host_state.WorldBrush;
		if (brushData != null && brushData.NumCubemapSamples > 0) {
			int smallestIndex = 0;
			Vector3 delta = origin - brushData.CubemapSamples![0].Origin;
			float smallestDist = MathLib.DotProduct(delta, delta);
			for (int i = 1; i < brushData.NumCubemapSamples; i++) {
				delta = origin - brushData.CubemapSamples![i].Origin;
				float dist = MathLib.DotProduct(delta, delta);
				if (dist < smallestDist) {
					smallestDist = dist;
					smallestIndex = i;
				}
			}

			return brushData.CubemapSamples![smallestIndex].Texture;
		}
		else
			return null;
	}

	private byte[]? ComputeStaticLightingForCacheEntry(LightCache cache, in Vector3 origin, int leaf, bool staticProp = false) {
		// todo
		byte[]? vis = null;

		R_StudioGetAmbientLightForPoint(leaf, origin, cache.StaticLightingState.BoxColor, staticProp, out bool addedLeafAmbientCube);

		if (r_ambientlightingonly.GetInt() == 0)
			AddStaticLighting(cache, origin, vis, staticProp, addedLeafAmbientCube);

		return vis;
	}

	private byte[]? PrecalcLightingState(LightCache cache, byte[]? vis) {
		LightingState lightingState = default;
		lightingState.ZeroLightingState();

		cache.StaticPrecalcLightingStateInfo.Clear();

		int i;
		for (i = 0; i < cache.StaticLightingState.NumLights; i++)
			vis = AddWorldLightToLightingState(cache.StaticLightingState.LocalLight[i], ref lightingState, cache.StaticPrecalcLightingStateInfo, cache.LightingOrigin, vis, true, false);

		for (i = 0; i < 6; i++)
			cache.StaticLightingState.BoxColor[i] += lightingState.BoxColor[i];

		cache.StaticPrecalcNumLocalLights = (ushort)lightingState.NumLights;
		for (i = 0; i < cache.StaticPrecalcNumLocalLights; i++)
			cache.StaticPrecalcLocalLight[i] = lightingState.LocalLight[i];

		return vis;
	}

	private static void CopyPrecalcedLightingState(LightCache cache, ref LightingState lightingState, LightingStateInfo info) {
		info.CopyFrom(cache.StaticPrecalcLightingStateInfo);

		int i;
		for (i = 0; i < 6; i++)
			lightingState.BoxColor[i] = cache.StaticLightingState.BoxColor[i];

		lightingState.NumLights = cache.StaticPrecalcNumLocalLights;
		for (i = 0; i < lightingState.NumLights; i++)
			lightingState.LocalLight[i] = cache.StaticPrecalcLocalLight[i];
	}

	private byte[]? AddLightingState(ref LightingState dst, in LightingState src, LightingStateInfo info, in Vector3 bucketOrigin, byte[]? vis, bool dynamic, bool ignoreVis) {
		int i;
		for (i = 0; i < src.NumLights; i++)
			vis = AddWorldLightToLightingState(src.LocalLight[i], ref dst, info, bucketOrigin, vis, dynamic, ignoreVis);

		for (i = 0; i < 6; i++)
			dst.BoxColor[i] += src.BoxColor[i];

		return vis;
	}

	private byte[]? AddWorldLightToLightingState(in BSPDWorldLightPtr light, ref LightingState lightingState, LightingStateInfo info, in Vector3 lightingOrigin, byte[]? vis, bool dynamic, bool ignoreVis) {
		// todo
		return vis;
	}

	private void AddStaticLighting(LightCache cache, in Vector3 origin, byte[]? vis, bool staticProp, bool addedLeafAmbientCube) {
		// todo
	}

	private byte[]? ComputeLightStyles(LightCache cache, ref LightingState lightingState, in Vector3 lightingOrigin, int leaf, byte[]? vis) {
		cache.DynamicLightingState.ZeroLightingState();
		// todo
		return vis;
	}

	private byte[]? ComputeDynamicLighting(LightCache cache, ref LightingState lightingState, in Vector3 lightingOrigin, int leaf, byte[]? vis) {
		cache.DynamicLightingState.ZeroLightingState();
		// todo
		return vis;
	}

	public void StudioCheckReinitLightingCache() {
		if (HardwareConfig.MaxNumLights() < r_worldlights.GetInt())
			r_worldlights.SetValue(HardwareConfig.MaxNumLights());

		if (CachedRWorldLights != r_worldlights.GetInt() ||
			CachedRRadiosity != r_radiosity.GetInt() ||
			CachedRAvgLight != r_avglight.GetInt() ||
			CachedMatFullbright != MatSysInterface.MaterialSystemConfig.Fullbright ||
			CachedRLightcacheNumAmbientSamples != r_lightcache_numambientsamples.GetInt()) {
			R_StudioInitLightingCache();
		}
	}
	public ref LightingState LightcacheGetStatic(LightCacheHandle_t cache, out ITexture envCubemap, LightCacheFlags flags = LightCacheFlags.Static | LightCacheFlags.Dynamic | LightCacheFlags.LightStyle) {
		throw new NotImplementedException();
	}

	public ITexture? LightcacheGetDynamic(in Vector3 origin, ref LightingState lightingState, ref LightcacheGetDynamic_Stats stats, LightCacheFlags flags = (LightCacheFlags.Static | LightCacheFlags.Dynamic | LightCacheFlags.Dynamic), bool debugModel = false) {
		LightingStateInfo info = new();

		int originLeaf = CM.PointLeafnum(origin);

		OriginToCacheOrigin(origin, out int x, out int y, out int z);

		int bucket = LightcacheHashKey(x, y, z, originLeaf);

		byte[]? vis = null;
		bool computeLightStyles = (flags & LightCacheFlags.LightStyle) != 0;

		LightCache? cache = FindInCache(bucket, x, y, z, originLeaf);

		if (cache != null) {
			LightcacheMark(cache);

			if (computeLightStyles && IsCachedLightStylesValid(cache))
				computeLightStyles = false;
		}
		else if (!AllowFullCacheMiss(flags)) {
			cache = FindNearestCache(x, y, z, originLeaf);
			originLeaf = cache!.Leaf;

			x = cache.X;
			y = cache.Y;
			z = cache.Z;
		}
		if (cache == null) {
			cache = NewLightcacheEntry(bucket);

			cache.X = x;
			cache.Y = y;
			cache.Z = z;
			cache.Leaf = originLeaf;

			if (r_lightcachecenter.GetBool())
				AdjustLightCacheOrigin(cache, origin, originLeaf);
			else
				cache.LightingOrigin = origin;

			cache.EnvCubemapTexture = FindEnvCubemapForPoint(cache.LightingOrigin);

			vis = ComputeStaticLightingForCacheEntry(cache, cache.LightingOrigin, originLeaf);
			vis = PrecalcLightingState(cache, vis);
		}

		stats.HasNonSwitchableLightStyles = cache.HasNonSwitchableLightStyle();
		stats.HasSwitchableLightStyles = cache.HasSwitchableLightStyle();

		if (computeLightStyles) {
			vis = ComputeLightStyles(cache, ref cache.LightStyleLightingState, cache.LightingOrigin, originLeaf, vis);
			stats.NeedsSwitchableLightStyleUpdate = true;
		}
		else
			stats.NeedsSwitchableLightStyleUpdate = false;

		stats.HasDLights = false;
		if ((flags & LightCacheFlags.Dynamic) != 0) {
			vis = ComputeDynamicLighting(cache, ref cache.DynamicLightingState, cache.LightingOrigin, originLeaf, vis);
			if (cache.DynamicLightingState.NumLights > 0)
				stats.HasDLights = true;
		}

		if ((flags & LightCacheFlags.Static) != 0)
			CopyPrecalcedLightingState(cache, ref lightingState, info);
		else
			lightingState.ZeroLightingState();

		if ((flags & LightCacheFlags.LightStyle) != 0)
			vis = AddLightingState(ref lightingState, cache.LightStyleLightingState, info, cache.LightingOrigin, vis, true, false);

		if ((flags & LightCacheFlags.Dynamic) != 0)
			vis = AddLightingState(ref lightingState, cache.DynamicLightingState, info, cache.LightingOrigin, vis, true, false);

		return cache.EnvCubemapTexture;
	}

	public void InvalidateStaticLightingCache() {
		// todo
	}

	public void ComputeDynamicLighting(in Vector3 pt, in Vector3 normal, out Vector3 color) {
		throw new NotImplementedException();
	}

	public void ComputeLighting(in Vector3 pt, in Vector3 normal, bool clamp, out Vector3 color, Span<Vector3> boxColors) {
		throw new NotImplementedException();
	}

	// Finds ambient lights
	public BSPDWorldLightPtr FindAmbientLight() {
		throw new NotImplementedException();
	}

	// Precache lighting
	public LightCacheHandle_t CreateStaticLightingCache(in Vector3 origin, in Vector3 mins, in Vector3 maxs) {
		// throw new NotImplementedException();
		return default; // TODO
	}

	public void ClearStaticLightingCache() {
		// throw new NotImplementedException();
		// todo
	}

	public bool ComputeVertexLightingFromSphericalSamples(in Vector3 vertex, in Vector3 normal, IHandleEntity? ignoreEnt, out Vector3 linearColor) {
		throw new NotImplementedException();
	}

	public bool StaticLightCacheAffectedByDynamicLight(LightCacheHandle_t handle) {
		throw new NotImplementedException();
	}

	public bool StaticLightCacheAffectedByAnimatedLightStyle(LightCacheHandle_t handle) {
		throw new NotImplementedException();
	}

	public bool StaticLightCacheNeedsSwitchableLightUpdate(LightCacheHandle_t handle) {
		throw new NotImplementedException();
	}

	public void AddWorldLightToAmbientCube(BSPDWorldLightPtr worldLight, in Vector3 lightingOrigin, ref AmbientCube ambientCube) {

	}

	public void InitDLightGlobals(int mapVersion) {

	}
}
