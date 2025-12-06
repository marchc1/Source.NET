global using AmbientCube = Source.InlineArray6<System.Numerics.Vector3>;

using Source.Common;
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

	public InlineArrayMaxLocalLights<BSPWorldLightPtr> LocalLight;

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

	public void AddLocalLight(in BSPWorldLightPtr localLight) {
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

public partial class Render
{
	public const int MAXLOCALLIGHTS = 4;

	public void StudioCheckReinitLightingCache() {
		throw new NotImplementedException();
	}
	public ref LightingState LightcacheGetStatic(LightCacheHandle_t cache, out ITexture envCubemap, LightCacheFlags flags = LightCacheFlags.Static | LightCacheFlags.Dynamic | LightCacheFlags.LightStyle) {
		throw new NotImplementedException();
	}

	public ITexture? LightcacheGetDynamic(in Vector3 origin, ref LightingState lightingState, ref LightcacheGetDynamic_Stats stats, LightCacheFlags flags = (LightCacheFlags.Static | LightCacheFlags.Dynamic | LightCacheFlags.Dynamic), bool debugModel = false) {
		throw new NotImplementedException();
	}

	public void StudioInitLightingCache() {
		throw new NotImplementedException();
	}

	public void InvalidateStaticLightingCache() {
		throw new NotImplementedException();
	}

	public void ComputeDynamicLighting(in Vector3 pt, in Vector3 normal, out Vector3 color) {
		throw new NotImplementedException();
	}

	public void ComputeLighting(in Vector3 pt, in Vector3 normal, bool clamp, out Vector3 color, Span<Vector3> boxColors) {
		throw new NotImplementedException();
	}

	// Finds ambient lights
	public BSPWorldLightPtr FindAmbientLight() {
		throw new NotImplementedException();
	}

	// Precache lighting
	public LightCacheHandle_t CreateStaticLightingCache(in Vector3 origin, in Vector3 mins, in Vector3 maxs) {
		throw new NotImplementedException();
	}

	public void ClearStaticLightingCache() {
		throw new NotImplementedException();
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

	public void AddWorldLightToAmbientCube(BSPWorldLightPtr worldLight, in Vector3 lightingOrigin, ref AmbientCube ambientCube) {

	}

	public void InitDLightGlobals(int mapVersion) {

	}
}
