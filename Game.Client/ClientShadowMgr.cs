global using static Game.Client.ClientShadowMgrGlobals;

using CommunityToolkit.HighPerformance;

using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;

using System.Numerics;

namespace Game.Client;

public static class ClientShadowMgrGlobals
{
	public static readonly ConVar r_flashlightdrawfrustum = new("r_flashlightdrawfrustum", "0");
	public static readonly ConVar r_flashlightmodels = new("r_flashlightmodels", "1");
	public static readonly ConVar r_shadowrendertotexture = new("r_shadowrendertotexture", "0");
	public static readonly ConVar r_flashlight_version2 = new("r_flashlight_version2", "0", FCvar.Cheat | FCvar.DevelopmentOnly);

	public static readonly ConVar r_flashlightdepthtexture = new("r_flashlightdepthtexture", "1");
	public static readonly ConVar r_flashlightdepthres = new("r_flashlightdepthres", "1024");

	public static readonly ConVar r_threaded_client_shadow_manager = new("r_threaded_client_shadow_manager", "0");

	public const TextureHandle_t INVALID_TEXTURE_HANDLE = unchecked((TextureHandle_t)~0);

	public const float TEXEL_SIZE_PER_CASTER_SIZE = 2.0f;
	public const int MAX_FALLOFF_AMOUNT = 240;
	public const int MAX_CLIP_PLANE_COUNT = 4;
	public const float SHADOW_CULL_TOLERANCE = 0.5f;

	public static readonly ConVar r_shadows = new("r_shadows", "1");
	public static readonly ConVar r_shadowmaxrendered = new("r_shadowmaxrendered", "32");
	public static readonly ConVar r_shadows_gamecontrol = new("r_shadows_gamecontrol", "-1", FCvar.Cheat);

	public static readonly ClientShadowMgr s_ClientShadowMgr = new();
	public static readonly IClientShadowMgr g_pClientShadowMgr = s_ClientShadowMgr;

	public static readonly VisibleShadowList s_VisibleShadowList = new();

	public static readonly List<C_BaseAnimating> s_NPCShadowBoneSetups = [];
	public static readonly List<C_BaseAnimating> s_NonNPCShadowBoneSetups = [];
}

public class TextureAllocator
{
	public const FragmentHandle_t INVALID_FRAGMENT_HANDLE = unchecked((FragmentHandle_t)~0);
	public const int TEXTURE_PAGE_SIZE = 1024;
	public const int MAX_TEXTURE_POWER = 8;
	public const int MIN_TEXTURE_POWER = 4;
	public const int MAX_TEXTURE_SIZE = 1 << MAX_TEXTURE_POWER;
	public const int MIN_TEXTURE_SIZE = 1 << MIN_TEXTURE_POWER;
	public const int BLOCK_SIZE = MAX_TEXTURE_SIZE;
	public const int BLOCKS_PER_ROW = TEXTURE_PAGE_SIZE / MAX_TEXTURE_SIZE;
	public const int BLOCK_COUNT = BLOCKS_PER_ROW * BLOCKS_PER_ROW;

	public struct TextureInfo_t
	{
		public FragmentHandle_t Fragment;
		public ushort Size;
		public ushort Power;
	}

	public struct FragmentInfo_t
	{
		public ushort Block;
		public ushort Index;
		public TextureHandle_t Texture;

		public uint FrameUsed;
	}

	public struct BlockInfo_t
	{
		public ushort FragmentPower;
	}

	public struct Cache_t
	{
		public ushort List;
	}

	ITexture? TexturePage;

	readonly PooledLinkedList<TextureInfo_t> Textures = new();
	readonly PooledLinkedList<FragmentInfo_t> Fragments = new();

	Cache_t[] Cache = new Cache_t[MAX_TEXTURE_POWER + 1];
	BlockInfo_t[] Blocks = new BlockInfo_t[BLOCK_COUNT];
	uint CurrentFrame;

	public void Init() => throw new NotImplementedException();
	public void Shutdown() => throw new NotImplementedException();

	public void Reset() => throw new NotImplementedException();

	public void DeallocateAllTextures() => throw new NotImplementedException();

	public TextureHandle_t AllocateTexture(int w, int h) => throw new NotImplementedException();
	public void DeallocateTexture(TextureHandle_t h) => throw new NotImplementedException();

	public bool UseTexture(TextureHandle_t h, bool willRedraw, float area) => throw new NotImplementedException();
	public bool HasValidTexture(TextureHandle_t h) => throw new NotImplementedException();

	public void AdvanceFrame() => throw new NotImplementedException();

	public void GetTextureRect(TextureHandle_t handle, out int x, out int y, out int w, out int h) => throw new NotImplementedException();

	public ITexture? GetTexture() => throw new NotImplementedException();

	public void GetTotalTextureSize(out int w, out int h) => throw new NotImplementedException();

	public void DebugPrintCache() => throw new NotImplementedException();

	void AddBlockToLRU(int block) => throw new NotImplementedException();

	void UnlinkFragmentFromCache(ref Cache_t cache, FragmentHandle_t fragment) => throw new NotImplementedException();

	void MarkUsed(FragmentHandle_t fragment) => throw new NotImplementedException();

	void MarkUnused(FragmentHandle_t fragment) => throw new NotImplementedException();

	void DisconnectTextureFromFragment(FragmentHandle_t f) => throw new NotImplementedException();

	int GetFragmentPower(FragmentHandle_t f) => throw new NotImplementedException();
}

public struct VisibleShadowInfo_t
{
	public ClientShadowHandle_t Shadow;
	public float Area;
	public Vector3 AbsCenter;
}

public class VisibleShadowList : IClientLeafShadowEnum
{
	readonly List<VisibleShadowInfo_t> ShadowsInView = [];
	readonly List<int> PriorityIndex = [];

	public int FindShadows(in ViewSetup view, int leafCount, ReadOnlySpan<LeafIndex_t> leafList) => throw new NotImplementedException();
	public int GetVisibleShadowCount() => ShadowsInView.Count;

	public ref readonly VisibleShadowInfo_t GetVisibleShadow(int i) => ref ShadowsInView.AsSpan()[PriorityIndex[i]];

	public void EnumShadow(ClientShadowHandle_t clientShadowHandle) => throw new NotImplementedException();
	float ComputeScreenArea(in Vector3 center, float r) => throw new NotImplementedException();
	void PrioritySort() => throw new NotImplementedException();
}

public class ClientShadowMgr : IClientShadowMgr
{
	public enum ShadowFlags_t
	{
		TextureDirty = (int)ClientShadowFlags.LastFlag << 1,
		BrushModel = (int)ClientShadowFlags.LastFlag << 2,
		UsingLodShadow = (int)ClientShadowFlags.LastFlag << 3,
		LightWorld = (int)ClientShadowFlags.LastFlag << 4,
	}

	public struct ClientShadow_t
	{
		public ClientEntityHandle Entity;
		public ShadowHandle_t ShadowHandle;
		public ClientLeafShadowHandle_t ClientLeafShadowHandle;
		public ushort Flags;
		public Matrix4x4 WorldToShadow;
		public Vector2 WorldSize;
		public Vector3 LastOrigin;
		public QAngle LastAngles;
		public TextureHandle_t ShadowTexture;
		public ITexture? ShadowDepthTexture;
		public int RenderFrame;
		public EHANDLE TargetEntity;
	}

	Vector3 SimpleShadowDir;
	Color AmbientLightColor;
	IMaterial? SimpleShadow;
	IMaterial? RenderShadow;
	IMaterial? RenderModelShadow;
	ITexture? DummyColorTexture;
	readonly PooledLinkedList<ClientShadow_t> Shadows = new();
	readonly TextureAllocator ShadowAllocator = new();

	bool RenderToTextureActive;
	bool RenderTargetNeedsClear;
	bool UpdatingDirtyShadows;
	bool Threaded;
	float ShadowCastDist;
	float MinShadowArea;
	readonly SortedSet<ClientShadowHandle_t> DirtyShadows = [];
	readonly List<ClientShadowHandle_t> TransparentShadows = [];

	bool DepthTextureActive;
	int DepthTextureResolution;

	readonly List<ITexture?> DepthTextureCache = [];
	readonly List<bool> DepthTextureCacheLocks = [];
	int MaxDepthTextureShadows;

	public ClientShadowMgr() => throw new NotImplementedException();

	public ReadOnlySpan<char> Name() => "CCLientShadowMgr";

	public bool Init() => throw new NotImplementedException();
	public void PostInit() { }
	public void Shutdown() => throw new NotImplementedException();
	public void LevelInitPreEntity() => throw new NotImplementedException();
	public void LevelInitPostEntity() { }
	public void LevelShutdownPreClearSteamAPIContext() { }
	public void LevelShutdownPreEntity() { }
	public void LevelShutdownPostEntity() => throw new NotImplementedException();

	public bool IsPerFrame() => true;

	public void PreRender() => throw new NotImplementedException();
	public void Update(double frametime) { }
	public void PostRender() { }

	public void OnSave() { }
	public void OnRestore() { }
	public void SafeRemoveIfDesired() { }

	public ClientShadowHandle_t CreateShadow(ClientEntityHandle entity, int flags) => throw new NotImplementedException();
	public void DestroyShadow(ClientShadowHandle_t handle) => throw new NotImplementedException();

	public ClientShadowHandle_t CreateFlashlight(in FlashlightState lightState) => throw new NotImplementedException();
	public void UpdateFlashlightState(ClientShadowHandle_t shadowHandle, in FlashlightState lightState) => throw new NotImplementedException();
	public void DestroyFlashlight(ClientShadowHandle_t shadowHandle) => throw new NotImplementedException();

	public void UpdateProjectedTexture(ClientShadowHandle_t handle, bool force = false) => throw new NotImplementedException();

	public void ComputeBoundingSphere(IClientRenderable? renderable, out Vector3 origin, out float radius) => throw new NotImplementedException();

	public void AddToDirtyShadowList(ClientShadowHandle_t handle, bool force = false) => throw new NotImplementedException();
	public void AddToDirtyShadowList(IClientRenderable? renderable, bool force = false) => throw new NotImplementedException();

	public void MarkRenderToTextureShadowDirty(ClientShadowHandle_t handle) => throw new NotImplementedException();

	public void AddShadowToReceiver(ClientShadowHandle_t handle, IClientRenderable? renderable, ShadowReceiver type) => throw new NotImplementedException();

	public void RemoveAllShadowsFromReceiver(IClientRenderable? renderable, ShadowReceiver type) => throw new NotImplementedException();

	public void ComputeShadowTextures(in ViewSetup view, int leafCount, ReadOnlySpan<LeafIndex_t> leafList) => throw new NotImplementedException();

	public void ComputeShadowDepthTextures(in ViewSetup view) => throw new NotImplementedException();

	public void FreeShadowDepthTextures() => throw new NotImplementedException();

	public ITexture? GetShadowTexture(ushort h) => throw new NotImplementedException();

	public ref readonly ShadowInfo_t GetShadowInfo(ClientShadowHandle_t h) => throw new NotImplementedException();

	public void RenderShadowTexture(int w, int h) => throw new NotImplementedException();

	public void SetShadowDirection(in Vector3 dir) => throw new NotImplementedException();
	public ref readonly Vector3 GetShadowDirection() => ref SimpleShadowDir;

	public void SetShadowColor(byte r, byte g, byte b) => throw new NotImplementedException();
	public void GetShadowColor(out byte r, out byte g, out byte b) => throw new NotImplementedException();

	public void SetShadowDistance(float maxDistance) => throw new NotImplementedException();
	public float GetShadowDistance() => ShadowCastDist;

	public void SetShadowBlobbyCutoffArea(float minArea) => throw new NotImplementedException();
	public float GetBlobbyCutoffArea() => MinShadowArea;

	public void SetFalloffBias(ClientShadowHandle_t handle, byte bias) => throw new NotImplementedException();

	public void RestoreRenderState() => throw new NotImplementedException();

	public void ComputeShadowBBox(IClientRenderable? renderable, in Vector3 absCenter, float radius, out Vector3 absMins, out Vector3 absMaxs) => throw new NotImplementedException();

	public bool WillParentRenderBlobbyShadow(IClientRenderable? renderable) => throw new NotImplementedException();

	public bool ShouldUseParentShadow(IClientRenderable? renderable) => throw new NotImplementedException();

	public void SetShadowsDisabled(bool disabled) => r_shadows_gamecontrol.SetValue(disabled != true ? 1 : 0);

	void UpdateStudioShadow(IClientRenderable? renderable, ClientShadowHandle_t handle) => throw new NotImplementedException();
	void UpdateBrushShadow(IClientRenderable? renderable, ClientShadowHandle_t handle) => throw new NotImplementedException();
	void UpdateShadow(ClientShadowHandle_t handle, bool force) => throw new NotImplementedException();

	IClientRenderable? GetParentShadowEntity(ClientShadowHandle_t handle) => throw new NotImplementedException();

	void AddChildBounds(in Matrix4x4 matWorldToBBox, IClientRenderable? parent, ref Vector3 mins, ref Vector3 maxs) => throw new NotImplementedException();

	void ComputeHierarchicalBounds(IClientRenderable? renderable, out Vector3 mins, out Vector3 maxs) => throw new NotImplementedException();

	void BuildGeneralWorldToShadowMatrix(out Matrix4x4 matWorldToShadow, in Vector3 origin, in Vector3 dir, in Vector3 xvec, in Vector3 yvec) => throw new NotImplementedException();

	void BuildWorldToShadowMatrix(out Matrix4x4 matWorldToShadow, in Vector3 origin, in Quaternion quatOrientation) => throw new NotImplementedException();

	void BuildPerspectiveWorldToFlashlightMatrix(out Matrix4x4 matWorldToShadow, in FlashlightState flashlightState) => throw new NotImplementedException();

	void UpdateProjectedTextureInternal(ClientShadowHandle_t handle, bool force) => throw new NotImplementedException();

	float ComputeLocalShadowOrigin(IClientRenderable? renderable, in Vector3 mins, in Vector3 maxs, in Vector3 localShadowDir, float backupFactor, out Vector3 origin) => throw new NotImplementedException();

	void RemoveShadowFromDirtyList(ClientShadowHandle_t handle) => throw new NotImplementedException();

	ShadowType GetActualShadowCastType(ClientShadowHandle_t handle) => throw new NotImplementedException();
	ShadowType GetActualShadowCastType(IClientRenderable? renderable) => throw new NotImplementedException();

	void BuildOrthoShadow(IClientRenderable? renderable, ClientShadowHandle_t handle, in Vector3 mins, in Vector3 maxs) => throw new NotImplementedException();

	void BuildRenderToTextureShadow(IClientRenderable? renderable, ClientShadowHandle_t handle, in Vector3 mins, in Vector3 maxs) => throw new NotImplementedException();

	void BuildFlashlight(ClientShadowHandle_t handle) => throw new NotImplementedException();

	void SetupRenderToTextureShadow(ClientShadowHandle_t h) => throw new NotImplementedException();
	void CleanUpRenderToTextureShadow(ClientShadowHandle_t h) => throw new NotImplementedException();

	void ComputeExtraClipPlanes(IClientRenderable? renderable, ClientShadowHandle_t handle, ReadOnlySpan<Vector3> vec, in Vector3 mins, in Vector3 maxs, in Vector3 localShadowDir) => throw new NotImplementedException();

	void ClearExtraClipPlanes(ClientShadowHandle_t h) => throw new NotImplementedException();
	void AddExtraClipPlane(ClientShadowHandle_t h, in Vector3 normal, float dist) => throw new NotImplementedException();

	bool CullReceiver(ClientShadowHandle_t handle, IClientRenderable? renderable, IClientRenderable? sourceRenderable) => throw new NotImplementedException();

	bool ComputeSeparatingPlane(IClientRenderable? rend1, IClientRenderable? rend2, out CollisionPlane plane) => throw new NotImplementedException();

	void UpdateAllShadows() => throw new NotImplementedException();

	bool DrawRenderToTextureShadow(ushort clientShadowHandle, float area) => throw new NotImplementedException();
	void DrawRenderToTextureShadowLOD(ushort clientShadowHandle) => throw new NotImplementedException();

	bool DrawShadowHierarchy(IClientRenderable? renderable, in ClientShadow_t shadow, bool child = false) => throw new NotImplementedException();

	bool BuildSetupListForRenderToTextureShadow(ushort clientShadowHandle, float area) => throw new NotImplementedException();
	bool BuildSetupShadowHierarchy(IClientRenderable? renderable, in ClientShadow_t shadow, bool child = false) => throw new NotImplementedException();

	void SetRenderToTextureShadowTexCoords(ShadowHandle_t handle, int x, int y, int w, int h) => throw new NotImplementedException();

	void DrawRenderToTextureDebugInfo(IClientRenderable? renderable, in Vector3 mins, in Vector3 maxs) => throw new NotImplementedException();

	public void AdvanceFrame() => throw new NotImplementedException();

	float GetShadowDistance(IClientRenderable? renderable) => throw new NotImplementedException();
	ref readonly Vector3 GetShadowDirection(IClientRenderable? renderable) => throw new NotImplementedException();

	void InitDepthTextureShadows() => throw new NotImplementedException();
	void ShutdownDepthTextureShadows() => throw new NotImplementedException();

	void InitRenderToTextureShadows() => throw new NotImplementedException();
	void ShutdownRenderToTextureShadows() => throw new NotImplementedException();

	static bool ShadowHandleCompareFunc(ClientShadowHandle_t lhs, ClientShadowHandle_t rhs) => lhs < rhs;

	ClientShadowHandle_t CreateProjectedTexture(ClientEntityHandle entity, int flags) => throw new NotImplementedException();

	bool LockShadowDepthTexture(ref ITexture? shadowDepthTexture) => throw new NotImplementedException();
	public void UnlockAllShadowDepthTextures() => throw new NotImplementedException();

	public void SetFlashlightTarget(ClientShadowHandle_t shadowHandle, EHANDLE targetEntity) => throw new NotImplementedException();

	public void SetFlashlightLightWorld(ClientShadowHandle_t shadowHandle, bool lightWorld) => throw new NotImplementedException();

	bool IsFlashlightTarget(ClientShadowHandle_t shadowHandle, IClientRenderable? renderable) => throw new NotImplementedException();

	int BuildActiveShadowDepthList(in ViewSetup viewSetup, int maxDepthShadows, Span<ClientShadowHandle_t> activeDepthShadows) => throw new NotImplementedException();

	void SetViewFlashlightState(int activeFlashlightCount, ReadOnlySpan<ClientShadowHandle_t> activeFlashlights) => throw new NotImplementedException();
}
