global using static Game.Client.ClientShadowMgrGlobals;
using static Source.Engine.ShadowMgrGlobals;
using static Source.Engine.StaticPropMgrGlobals;

using CommunityToolkit.HighPerformance;

using Source;
using Source.Common;
using Source.Common.Bitmap;
using Source.Common.Commands;
using Source.Common.Engine;
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

	public static readonly ClientShadowMgr s_ClientShadowMgr = new();
	public static readonly IClientShadowMgr g_ClientShadowMgr = s_ClientShadowMgr;

	public static readonly VisibleShadowList s_VisibleShadowList = new();

	public static void ShadowRestoreFunc(int changeFlags) {
		s_ClientShadowMgr.RestoreRenderState();
	}

	public static readonly List<C_BaseAnimating> s_NPCShadowBoneSetups = [];
	public static readonly List<C_BaseAnimating> s_NonNPCShadowBoneSetups = [];

	[ConCommand("r_shadowangles", "Set shadow angles", FCvar.Cheat)]
	public static void r_shadowangles(in TokenizedCommand args) {
		if (args.ArgC() == 1) {
			Vector3 dir = s_ClientShadowMgr.GetShadowDirection();
			MathLib.VectorAngles(dir, out QAngle angles);
			Msg($"Shadow angles {angles.X} {angles.Y} {angles.Z}\n");
			return;
		}

		if (args.ArgC() == 4) {
			QAngle angles = default;
			angles.X = float.Parse(args[1]);
			angles.Y = float.Parse(args[2]);
			angles.Z = float.Parse(args[3]);
			MathLib.AngleVectors(angles, out Vector3 dir);
			s_ClientShadowMgr.SetShadowDirection(dir);
		}
	}

	[ConCommand("r_shadowcolor", "Set shadow color", FCvar.Cheat)]
	public static void r_shadowcolor(in TokenizedCommand args) {
		if (args.ArgC() == 1) {
			s_ClientShadowMgr.GetShadowColor(out byte r, out byte g, out byte b);
			Msg($"Shadow color {r} {g} {b}\n");
			return;
		}

		if (args.ArgC() == 4) {
			int r = int.Parse(args[1]);
			int g = int.Parse(args[2]);
			int b = int.Parse(args[3]);
			s_ClientShadowMgr.SetShadowColor((byte)r, (byte)g, (byte)b);
		}
	}

	[ConCommand("r_shadowdist", "Set shadow distance", FCvar.Cheat)]
	public static void r_shadowdist(in TokenizedCommand args) {
		if (args.ArgC() == 1) {
			float dist = s_ClientShadowMgr.GetShadowDistance();
			Msg($"Shadow distance {dist:F2}\n");
			return;
		}

		if (args.ArgC() == 2)
			s_ClientShadowMgr.SetShadowDistance(float.Parse(args[1]));
	}

	[ConCommand("r_shadowblobbycutoff", "some shadow stuff", FCvar.Cheat)]
	public static void r_shadowblobbycutoff(in TokenizedCommand args) {
		if (args.ArgC() == 1) {
			float area = s_ClientShadowMgr.GetBlobbyCutoffArea();
			Msg($"Cutoff area {area:F2}\n");
			return;
		}

		if (args.ArgC() == 2)
			s_ClientShadowMgr.SetShadowBlobbyCutoffArea(float.Parse(args[1]));
	}
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

public class ShadowLeafEnum : ISpatialLeafEnumerator
{
	public readonly List<int> LeafList = [];

	public bool EnumerateLeaf(int leaf, nint context) {
		LeafList.Add(leaf);
		return true;
	}
}

public class ClientShadowBox
{
	public ClientShadowMgr.ClientShadow_t Shadow;
}

public class ClientShadowMgr : IClientShadowMgr
{
	public enum ShadowFlags_t
	{
		TextureDirty = ClientShadowFlags.LastFlag << 1,
		BrushModel = ClientShadowFlags.LastFlag << 2,
		UsingLodShadow = ClientShadowFlags.LastFlag << 3,
		LightWorld = ClientShadowFlags.LastFlag << 4,
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
	readonly Dictionary<ClientShadowHandle_t, ClientShadowBox> Shadows = [];
	readonly List<ClientShadowHandle_t> ValidShadowHandles = [];
	ClientShadowHandle_t curShadowHandleIdx;
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

	public ClientShadowMgr() {
		RenderToTextureActive = false;
		DepthTextureActive = false;

		DepthTextureResolution = r_flashlightdepthres.GetInt();
		Threaded = false;
	}

	public ReadOnlySpan<char> Name() => "CCLientShadowMgr";

	public bool Init() {
		RenderTargetNeedsClear = false;
		SimpleShadow = materials.FindMaterial("decals/simpleshadow", MaterialDefines.TEXTURE_GROUP_DECAL);

		Vector3 dir = new(0.1f, 0.1f, -1);
		SetShadowDirection(dir);
		SetShadowDistance(50);

		SetShadowBlobbyCutoffArea(0.005f);

		bool tools = commandLine.CheckParm("-tools");
		MaxDepthTextureShadows = tools ? 4 : 2;

		if (r_shadowrendertotexture.GetBool())
			InitRenderToTextureShadows();

		if (r_flashlightdepthtexture.GetBool() && !materials.SupportsShadowDepthTextures()) {
			r_flashlightdepthtexture.SetValue(0);
			ShutdownDepthTextureShadows();
		}

		if (r_flashlightdepthtexture.GetBool())
			InitDepthTextureShadows();

		materials.AddRestoreFunc(ShadowRestoreFunc);

		return true;
	}
	public void PostInit() { }
	public void Shutdown() {
		SimpleShadow = null;
		Shadows.Clear();
		ValidShadowHandles.Clear();
		ShutdownRenderToTextureShadows();

		ShutdownDepthTextureShadows();

		materials.RemoveRestoreFunc(ShadowRestoreFunc);
	}

	public void LevelInitPreEntity() {
		UpdatingDirtyShadows = false;

		engine.GetAmbientLightColor(out Vector3 ambientColor);
		ambientColor *= 3;
		ambientColor += new Vector3(0.3f, 0.3f, 0.3f);

		byte r = ambientColor[0] > 1.0 ? (byte)255 : (byte)(255 * ambientColor[0]);
		byte g = ambientColor[1] > 1.0 ? (byte)255 : (byte)(255 * ambientColor[1]);
		byte b = ambientColor[2] > 1.0 ? (byte)255 : (byte)(255 * ambientColor[2]);

		SetShadowColor(r, g, b);

		if (RenderToTextureActive) {
			ShadowAllocator.Reset();
			RenderTargetNeedsClear = true;
		}
	}
	public void LevelInitPostEntity() { }
	public void LevelShutdownPreClearSteamAPIContext() { }
	public void LevelShutdownPreEntity() { }
	public void LevelShutdownPostEntity() {
		Assert(Shadows.Count == 0);

		for (int i = ValidShadowHandles.Count - 1; i >= 0; i--)
			DestroyShadow(ValidShadowHandles[i]);

		if (RenderToTextureActive)
			ShadowAllocator.DeallocateAllTextures();

		r_shadows_gamecontrol.SetValue(-1);
	}

	public bool IsPerFrame() => true;

	public void PreRender() {
		if (r_flashlightdepthtexture.GetBool() && !materials.SupportsShadowDepthTextures()) {
			r_flashlightdepthtexture.SetValue(0);
			ShutdownDepthTextureShadows();
		}

		bool depthTextureActive = r_flashlightdepthtexture.GetBool();
		int depthTextureResolution = r_flashlightdepthres.GetInt();

		if ((depthTextureActive != DepthTextureActive) || (depthTextureResolution != DepthTextureResolution)) {
			if ((depthTextureActive == true) && (DepthTextureActive == true) &&
				(depthTextureResolution != DepthTextureResolution)) {
				ShutdownDepthTextureShadows();
				InitDepthTextureShadows();
			}
			else {
				if (DepthTextureActive && !depthTextureActive)
					ShutdownDepthTextureShadows();
				else if (depthTextureActive && !DepthTextureActive)
					InitDepthTextureShadows();
			}
		}

		bool renderToTextureActive = r_shadowrendertotexture.GetBool();
		if (renderToTextureActive != RenderToTextureActive) {
			if (RenderToTextureActive)
				ShutdownRenderToTextureShadows();
			else
				InitRenderToTextureShadows();

			UpdateAllShadows();
			return;
		}

		UpdatingDirtyShadows = true;

		foreach (ClientShadowHandle_t handle in DirtyShadows) {
			Assert(Shadows.ContainsKey(handle));
			UpdateProjectedTextureInternal(handle, false);
		}
		DirtyShadows.Clear();

		int count = TransparentShadows.Count;
		for (int j = 0; j < count; ++j)
			DirtyShadows.Add(TransparentShadows[j]);
		TransparentShadows.Clear();

		UpdatingDirtyShadows = false;
	}
	public void Update(double frametime) { }
	public void PostRender() { }

	public void OnSave() { }
	public void OnRestore() { }
	public void SafeRemoveIfDesired() { }

	public ClientShadowHandle_t CreateShadow(ClientEntityHandle entity, int flags) {
		flags &= ~(int)ShadowFlags.ProjectedTextureTypeMask;
		flags |= (int)ShadowFlags.Shadow | (int)ShadowFlags_t.TextureDirty;
		ClientShadowHandle_t shadowHandle = CreateProjectedTexture(entity, flags);

		IClientRenderable? renderable = cl_entitylist.GetClientRenderableFromHandle(entity);
		if (renderable != null) {
			Assert(!renderable.IsShadowDirty());
			renderable.MarkShadowDirty(true);
		}

		AddToDirtyShadowList(shadowHandle, true);
		return shadowHandle;
	}

	public void DestroyShadow(ClientShadowHandle_t handle) {
		Assert(Shadows.ContainsKey(handle));
		RemoveShadowFromDirtyList(handle);
		g_ShadowMgr.DestroyShadow(Shadows[handle].Shadow.ShadowHandle);
		clientLeafSystem.RemoveShadow(Shadows[handle].Shadow.ClientLeafShadowHandle);
		CleanUpRenderToTextureShadow(handle);
		Shadows.Remove(handle);
		ValidShadowHandles.Remove(handle);
	}

	public ClientShadowHandle_t CreateFlashlight(in FlashlightState lightState) => throw new NotImplementedException();
	public void UpdateFlashlightState(ClientShadowHandle_t shadowHandle, in FlashlightState lightState) => throw new NotImplementedException();
	public void DestroyFlashlight(ClientShadowHandle_t shadowHandle) => throw new NotImplementedException();

	public void UpdateProjectedTexture(ClientShadowHandle_t handle, bool force = false) => throw new NotImplementedException();

	public void ComputeBoundingSphere(IClientRenderable? renderable, out Vector3 origin, out float radius) {
		Assert(renderable != null);
		renderable!.GetShadowRenderBounds(out Vector3 mins, out Vector3 maxs, GetActualShadowCastType(renderable));
		MathLib.VectorSubtract(maxs, mins, out Vector3 size);
		radius = size.Length() * 0.5f;

		MathLib.VectorAdd(mins, maxs, out Vector3 centroid);
		centroid *= 0.5f;

		Span<Vector3> vec = stackalloc Vector3[3];
		MathLib.AngleVectors(renderable.GetRenderAngles(), out vec[0], out vec[1], out vec[2]);
		vec[1] *= -1.0f;

		MathLib.VectorCopy(renderable.GetRenderOrigin(), out origin);
		MathLib.VectorMA(origin, centroid.X, vec[0], out origin);
		MathLib.VectorMA(origin, centroid.Y, vec[1], out origin);
		MathLib.VectorMA(origin, centroid.Z, vec[2], out origin);
	}

	public void AddToDirtyShadowList(ClientShadowHandle_t handle, bool force = false) {
		if (UpdatingDirtyShadows)
			return;

		if (handle == CLIENTSHADOW_INVALID_HANDLE)
			return;

		Assert(!DirtyShadows.Contains(handle));
		DirtyShadows.Add(handle);

		if (force)
			Shadows[handle].Shadow.LastAngles = new(float.MaxValue, float.MaxValue, float.MaxValue);

		IClientRenderable? parent = GetParentShadowEntity(handle);
		if (parent != null)
			AddToDirtyShadowList(parent, force);
	}
	public void AddToDirtyShadowList(IClientRenderable? renderable, bool force = false) {
		if (UpdatingDirtyShadows)
			return;

		if (renderable!.IsShadowDirty())
			return;

		ClientShadowHandle_t handle = renderable.GetShadowHandle();
		if (handle == CLIENTSHADOW_INVALID_HANDLE)
			return;

#if DEBUG
		if (handle != CLIENTSHADOW_INVALID_HANDLE) {
			IClientRenderable? shadowRenderable = cl_entitylist.GetClientRenderableFromHandle(Shadows[handle].Shadow.Entity);
			Assert(renderable == shadowRenderable);
		}
#endif

		renderable.MarkShadowDirty(true);
		AddToDirtyShadowList(handle, force);
	}

	public void MarkRenderToTextureShadowDirty(ClientShadowHandle_t handle) {
		if (handle != CLIENTSHADOW_INVALID_HANDLE) {
			ref ClientShadow_t shadow = ref Shadows[handle].Shadow;
			shadow.Flags |= (ushort)ShadowFlags_t.TextureDirty;

			IClientRenderable? parent = GetParentShadowEntity(handle);
			if (parent != null) {
				ClientShadowHandle_t parentHandle = parent.GetShadowHandle();
				if (parentHandle != CLIENTSHADOW_INVALID_HANDLE)
					Shadows[parentHandle].Shadow.Flags |= (ushort)ShadowFlags_t.TextureDirty;
			}
		}
	}

	public void AddShadowToReceiver(ClientShadowHandle_t handle, IClientRenderable? renderable, ShadowReceiver type) {
		ref ClientShadow_t shadow = ref Shadows[handle].Shadow;

		IClientRenderable? sourceRenderable = cl_entitylist.GetClientRenderableFromHandle(shadow.Entity);

		if (sourceRenderable == renderable)
			return;

		if (!renderable!.ShouldReceiveProjectedTextures(ShadowFlags.ProjectedTextureTypeMask))
			return;

		if (CullReceiver(handle, renderable, sourceRenderable))
			return;

		switch (type) {
			case ShadowReceiver.BrushModel:
				if ((shadow.Flags & (int)ShadowFlags.Flashlight) != 0) {
					if (!shadow.TargetEntity.IsValid() || IsFlashlightTarget(handle, renderable)) {
						g_ShadowMgr.AddShadowToBrushModel(shadow.ShadowHandle, renderable.GetModel(), renderable.GetRenderOrigin(), renderable.GetRenderAngles());
						g_ShadowMgr.AddFlashlightRenderable(shadow.ShadowHandle, renderable);
					}
				}
				else
					g_ShadowMgr.AddShadowToBrushModel(shadow.ShadowHandle, renderable.GetModel(), renderable.GetRenderOrigin(), renderable.GetRenderAngles());
				break;

			case ShadowReceiver.StaticProp:
				if (GetActualShadowCastType(handle) == ShadowType.RenderToTexture) {
					C_BaseEntity? ent = sourceRenderable!.GetIClientUnknown()!.GetBaseEntity();
					if (ent != null && (ent.GetFlags() & (EntityFlags.NPC | EntityFlags.Client)) != 0)
						g_StaticPropMgr.AddShadowToStaticProp(shadow.ShadowHandle, renderable);
				}
				else if ((shadow.Flags & (int)ShadowFlags.Flashlight) != 0) {
					if (!shadow.TargetEntity.IsValid() || IsFlashlightTarget(handle, renderable)) {
						g_StaticPropMgr.AddShadowToStaticProp(shadow.ShadowHandle, renderable);
						g_ShadowMgr.AddFlashlightRenderable(shadow.ShadowHandle, renderable);
					}
				}
				break;

			case ShadowReceiver.StudioModel:
				if ((shadow.Flags & (int)ShadowFlags.Flashlight) != 0) {
					if (!shadow.TargetEntity.IsValid() || IsFlashlightTarget(handle, renderable)) {
						renderable.CreateModelInstance();
						g_ShadowMgr.AddShadowToModel(shadow.ShadowHandle, renderable.GetModelInstance());
						g_ShadowMgr.AddFlashlightRenderable(shadow.ShadowHandle, renderable);
					}
				}
				break;
		}
	}

	public void RemoveAllShadowsFromReceiver(IClientRenderable? renderable, ShadowReceiver type) {
		if (!renderable!.ShouldReceiveProjectedTextures(ShadowFlags.ProjectedTextureTypeMask))
			return;

		switch (type) {
			case ShadowReceiver.BrushModel:
				Model? model = renderable.GetModel();
				g_ShadowMgr.RemoveAllShadowsFromBrushModel(model);
				break;
			case ShadowReceiver.StaticProp:
				g_StaticPropMgr.RemoveAllShadowsFromStaticProp(renderable);
				break;
			case ShadowReceiver.StudioModel:
				if (renderable.GetModelInstance() != MODEL_INSTANCE_INVALID)
					g_ShadowMgr.RemoveAllShadowsFromModel(renderable.GetModelInstance());
				break;
		}
	}

	public void ComputeShadowTextures(in ViewSetup view, int leafCount, ReadOnlySpan<LeafIndex_t> leafList) => throw new NotImplementedException();

	public void ComputeShadowDepthTextures(in ViewSetup view) => throw new NotImplementedException();

	public void FreeShadowDepthTextures() => throw new NotImplementedException();

	public ITexture? GetShadowTexture(ushort h) => throw new NotImplementedException();

	public ref readonly ShadowInfo_t GetShadowInfo(ClientShadowHandle_t h) => throw new NotImplementedException();

	public void RenderShadowTexture(int w, int h) => throw new NotImplementedException();

	public void SetShadowDirection(in Vector3 dir) {
		MathLib.VectorCopy(dir, out SimpleShadowDir);
		MathLib.VectorNormalize(ref SimpleShadowDir);

		if (RenderToTextureActive)
			UpdateAllShadows();
	}

	static Vector3 s_vecDown = new(0, 0, -1);
	public ref readonly Vector3 GetShadowDirection() {
		if (!RenderToTextureActive)
			return ref s_vecDown;

		return ref SimpleShadowDir;
	}

	public void SetShadowColor(byte r, byte g, byte b) {
		float fr = r / 255.0f;
		float fg = g / 255.0f;
		float fb = b / 255.0f;

		SimpleShadow!.ColorModulate(fr, fg, fb);

		if (RenderToTextureActive) {
			RenderShadow!.ColorModulate(fr, fg, fb);
			RenderModelShadow!.ColorModulate(fr, fg, fb);
		}

		AmbientLightColor.R = r;
		AmbientLightColor.G = g;
		AmbientLightColor.B = b;
	}
	public void GetShadowColor(out byte r, out byte g, out byte b) {
		r = AmbientLightColor.R;
		g = AmbientLightColor.G;
		b = AmbientLightColor.B;
	}

	public void SetShadowDistance(float maxDistance) {
		ShadowCastDist = maxDistance;
		UpdateAllShadows();
	}
	public float GetShadowDistance() => ShadowCastDist;

	public void SetShadowBlobbyCutoffArea(float minArea) => MinShadowArea = minArea;
	public float GetBlobbyCutoffArea() => MinShadowArea;

	public void SetFalloffBias(ClientShadowHandle_t handle, byte bias) => throw new NotImplementedException();

	public void RestoreRenderState() {
		foreach (ClientShadowHandle_t h in ValidShadowHandles)
			Shadows[h].Shadow.Flags |= (ushort)ShadowFlags_t.TextureDirty;

		SetShadowColor(AmbientLightColor.R, AmbientLightColor.G, AmbientLightColor.B);
		RenderTargetNeedsClear = true;
	}

	public void ComputeShadowBBox(IClientRenderable? renderable, in Vector3 absCenter, float radius, out Vector3 absMins, out Vector3 absMaxs) => throw new NotImplementedException();

	public bool WillParentRenderBlobbyShadow(IClientRenderable? renderable) {
		if (renderable == null)
			return false;

		IClientRenderable? shadowParent = renderable.GetShadowParent();
		if (shadowParent == null)
			return false;

		ShadowType shadowType = GetActualShadowCastType(shadowParent);
		if (shadowType == ShadowType.None)
			return WillParentRenderBlobbyShadow(shadowParent);

		return shadowType == ShadowType.Simple;
	}

	public bool ShouldUseParentShadow(IClientRenderable? renderable) {
		if (renderable == null)
			return false;

		IClientRenderable? shadowParent = renderable.GetShadowParent();
		if (shadowParent == null)
			return false;

		ShadowType shadowType = GetActualShadowCastType(shadowParent);
		if (shadowType == ShadowType.Simple)
			return false;

		if (shadowType == ShadowType.None)
			return ShouldUseParentShadow(shadowParent);

		return true;
	}

	public void SetShadowsDisabled(bool disabled) => r_shadows_gamecontrol.SetValue(disabled != true ? 1 : 0);

	void UpdateStudioShadow(IClientRenderable? renderable, ClientShadowHandle_t handle) {
		if ((Shadows[handle].Shadow.Flags & (int)ShadowFlags.Flashlight) == 0) {
			ComputeHierarchicalBounds(renderable, out Vector3 mins, out Vector3 maxs);

			ShadowType shadowType = GetActualShadowCastType(handle);
			if (shadowType != ShadowType.RenderToTexture)
				BuildOrthoShadow(renderable, handle, mins, maxs);
			else
				BuildRenderToTextureShadow(renderable, handle, mins, maxs);
		}
		else
			BuildFlashlight(handle);
	}

	void UpdateBrushShadow(IClientRenderable? renderable, ClientShadowHandle_t handle) {
		if ((Shadows[handle].Shadow.Flags & (int)ShadowFlags.Flashlight) == 0) {
			ComputeHierarchicalBounds(renderable, out Vector3 mins, out Vector3 maxs);

			ShadowType shadowType = GetActualShadowCastType(handle);
			if (shadowType != ShadowType.RenderToTexture)
				BuildOrthoShadow(renderable, handle, mins, maxs);
			else
				BuildRenderToTextureShadow(renderable, handle, mins, maxs);
		}
		else
			BuildFlashlight(handle);
	}
	void UpdateShadow(ClientShadowHandle_t handle, bool force) {
		ref ClientShadow_t shadow = ref Shadows[handle].Shadow;

		IClientRenderable? renderable = cl_entitylist.GetClientRenderableFromHandle(shadow.Entity);
		if (renderable == null) {
			DestroyShadow(handle);
			return;
		}

		if (renderable.GetModel() == null) {
			renderable.MarkShadowDirty(false);
			return;
		}

		ref readonly Source.Common.Engine.ShadowInfo_t shadowInfo = ref g_ShadowMgr.GetInfo(shadow.ShadowHandle);
		if (shadowInfo.FalloffBias == 255) {
			g_ShadowMgr.EnableShadow(shadow.ShadowHandle, false);
			TransparentShadows.Add(handle);
			return;
		}

		if (ShouldUseParentShadow(renderable) || WillParentRenderBlobbyShadow(renderable)) {
			g_ShadowMgr.EnableShadow(shadow.ShadowHandle, false);
			renderable.MarkShadowDirty(false);
			return;
		}

		g_ShadowMgr.EnableShadow(shadow.ShadowHandle, true);

		ref readonly Vector3 origin = ref renderable.GetRenderOrigin();
		ref readonly QAngle angles = ref renderable.GetRenderAngles();

		if (force || (origin != shadow.LastOrigin) || (angles != shadow.LastAngles)) {
			MathLib.VectorCopy(origin, out shadow.LastOrigin);
			MathLib.VectorCopy(angles, out shadow.LastAngles);

			using MatRenderContextPtr renderContext = new(materials);
			Model? model = renderable.GetModel();
			// MaterialFogMode fogMode = renderContext.GetFogMode();
			// renderContext.FogMode(MaterialFogMode.None);
			switch (modelinfo.GetModelType(model)) {
				case ModelType.Brush:
					UpdateBrushShadow(renderable, handle);
					break;
				case ModelType.Studio:
					UpdateStudioShadow(renderable, handle);
					break;
				default:
					Assert(false);
					break;
			}
			// renderContext.FogMode(fogMode);
		}

		renderable.MarkShadowDirty(false);
	}

	IClientRenderable? GetParentShadowEntity(ClientShadowHandle_t handle) {
		ref ClientShadow_t shadow = ref Shadows[handle].Shadow;
		IClientRenderable? renderable = cl_entitylist.GetClientRenderableFromHandle(shadow.Entity);
		if (renderable != null) {
			if (ShouldUseParentShadow(renderable)) {
				IClientRenderable? parent = renderable.GetShadowParent();
				while (GetActualShadowCastType(parent) == ShadowType.None) {
					parent = parent!.GetShadowParent();
					Assert(parent != null);
				}
				return parent;
			}
		}
		return null;
	}

	void AddChildBounds(in Matrix3x4 matWorldToBBox, IClientRenderable? parent, ref Vector3 mins, ref Vector3 maxs) {
		IClientRenderable? child = parent!.FirstShadowChild();
		while (child != null) {
			if (GetActualShadowCastType(child) != ShadowType.None) {
				child.GetShadowRenderBounds(out Vector3 childMins, out Vector3 childMaxs, ShadowType.RenderToTexture);
				MathLib.ConcatTransforms(in matWorldToBBox, in child.RenderableToWorldTransform(), out Matrix3x4 childToBBox);
				MathLib.TransformAABB(in childToBBox, in childMins, in childMaxs, out Vector3 newChildMins, out Vector3 newChildMaxs);
				MathLib.VectorMin(mins, newChildMins, out mins);
				MathLib.VectorMax(maxs, newChildMaxs, out maxs);
			}

			AddChildBounds(in matWorldToBBox, child, ref mins, ref maxs);
			child = child.NextShadowPeer();
		}
	}

	void ComputeHierarchicalBounds(IClientRenderable? renderable, out Vector3 mins, out Vector3 maxs) {
		ShadowType shadowType = GetActualShadowCastType(renderable);

		renderable!.GetShadowRenderBounds(out mins, out maxs, shadowType);

		IClientRenderable? child = renderable.FirstShadowChild();

		if (child != null && shadowType != ShadowType.Simple) {
			MathLib.MatrixInvert(in renderable.RenderableToWorldTransform(), out Matrix3x4 matWorldToBBox);
			AddChildBounds(in matWorldToBBox, renderable, ref mins, ref maxs);
		}
	}

	void BuildGeneralWorldToShadowMatrix(out Matrix4x4 matWorldToShadow, in Vector3 origin, in Vector3 dir, in Vector3 xvec, in Vector3 yvec) {
		matWorldToShadow = default;
		MathLib.MatrixSetColumn(ref matWorldToShadow, 0, in xvec);
		MathLib.MatrixSetColumn(ref matWorldToShadow, 1, in yvec);
		MathLib.MatrixSetColumn(ref matWorldToShadow, 2, in dir);
		MathLib.MatrixSetColumn(ref matWorldToShadow, 3, in origin);
		matWorldToShadow[3, 0] = matWorldToShadow[3, 1] = matWorldToShadow[3, 2] = 0.0f;
		matWorldToShadow[3, 3] = 1.0f;

		MathLib.MatrixInverseGeneral(in matWorldToShadow, out matWorldToShadow);
	}

	static void BuildWorldToTextureMatrix(in Matrix4x4 matWorldToShadow, in Vector2 size, out Matrix4x4 matWorldToTexture) {
		MathLib.MatrixBuildScale(out Matrix4x4 shadowToUnit, 1.0f / size.X, 1.0f / size.Y, 1.0f);
		shadowToUnit[0, 3] = shadowToUnit[1, 3] = 0.5f;

		MathLib.MatrixMultiply(in shadowToUnit, in matWorldToShadow, out matWorldToTexture);
	}

	static void SortAbsVectorComponents(in Vector3 src, Span<int> vecIdx) {
		Vector3 absVec = new(MathF.Abs(src[0]), MathF.Abs(src[1]), MathF.Abs(src[2]));

		int maxIdx = (absVec[0] > absVec[1]) ? 0 : 1;
		if (absVec[2] > absVec[maxIdx])
			maxIdx = 2;

		switch (maxIdx) {
			case 0:
				vecIdx[0] = 1;
				vecIdx[1] = 2;
				vecIdx[2] = 0;
				break;
			case 1:
				vecIdx[0] = 2;
				vecIdx[1] = 0;
				vecIdx[2] = 1;
				break;
			case 2:
				vecIdx[0] = 0;
				vecIdx[1] = 1;
				vecIdx[2] = 2;
				break;
		}
	}

	void BuildWorldToShadowMatrix(out Matrix4x4 matWorldToShadow, in Vector3 origin, in Quaternion quatOrientation) => throw new NotImplementedException();

	void BuildPerspectiveWorldToFlashlightMatrix(out Matrix4x4 matWorldToShadow, in FlashlightState flashlightState) => throw new NotImplementedException();

	void UpdateProjectedTextureInternal(ClientShadowHandle_t handle, bool force) {
		ref ClientShadow_t shadow = ref Shadows[handle].Shadow;

		if ((shadow.Flags & (int)ShadowFlags.Flashlight) != 0) {
			Assert((shadow.Flags & (int)ShadowFlags.Shadow) == 0);
			ref ClientShadow_t shadowClient = ref Shadows[handle].Shadow;

			g_ShadowMgr.EnableShadow(shadowClient.ShadowHandle, true);

			UpdateBrushShadow(null, handle);
		}
		else {
			Assert((shadow.Flags & (int)ShadowFlags.Shadow) != 0);
			Assert((shadow.Flags & (int)ShadowFlags.Flashlight) == 0);
			UpdateShadow(handle, force);
		}
	}

	float ComputeLocalShadowOrigin(IClientRenderable? renderable, in Vector3 mins, in Vector3 maxs, in Vector3 localShadowDir, float backupFactor, out Vector3 origin) {
		MathLib.VectorAdd(in mins, in maxs, out Vector3 centroid);
		centroid *= 0.5f;

		MathLib.VectorSubtract(in maxs, in mins, out Vector3 size);
		float radius = size.Length() * 0.5f;

		float centroidProjection = MathLib.DotProduct(centroid, localShadowDir);
		float minDist = -centroidProjection;
		for (int i = 0; i < 3; ++i) {
			if (localShadowDir[i] > 0.0f)
				minDist += localShadowDir[i] * mins[i];
			else
				minDist += localShadowDir[i] * maxs[i];
		}

		minDist *= backupFactor;

		MathLib.VectorMA(in centroid, minDist, in localShadowDir, out origin);

		return radius - minDist;
	}

	void RemoveShadowFromDirtyList(ClientShadowHandle_t handle) {
		if (DirtyShadows.Contains(handle)) {
			IClientRenderable? renderable = cl_entitylist.GetClientRenderableFromHandle(Shadows[handle].Shadow.Entity);
			renderable?.MarkShadowDirty(false);
			DirtyShadows.Remove(handle);
		}
	}

	ShadowType GetActualShadowCastType(ClientShadowHandle_t handle) {
		if (handle == CLIENTSHADOW_INVALID_HANDLE)
			return ShadowType.None;

		if ((Shadows[handle].Shadow.Flags & (int)ClientShadowFlags.UseRenderToTexture) != 0)
			return RenderToTextureActive ? ShadowType.RenderToTexture : ShadowType.Simple;
		else if ((Shadows[handle].Shadow.Flags & (int)ClientShadowFlags.UseDepthTexture) != 0)
			return ShadowType.RenderToDepthTexture;
		else
			return ShadowType.Simple;
	}
	ShadowType GetActualShadowCastType(IClientRenderable? renderable) => GetActualShadowCastType(renderable != null ? renderable.GetShadowHandle() : CLIENTSHADOW_INVALID_HANDLE);

	static void BuildShadowLeafList(ShadowLeafEnum shadowEnum, in Vector3 origin, in Vector3 dir, in Vector2 size, float maxDist) {
		Ray ray = default;
		MathLib.VectorCopy(origin, out ray.Start);
		MathLib.VectorMultiply(dir, maxDist, out ray.Delta);
		ray.StartOffset = new(0, 0, 0);

		float radius = MathF.Sqrt(size.X * size.X + size.Y * size.Y) * 0.5f;
		ray.Extents = new(radius, radius, radius);
		ray.IsRay = false;
		ray.IsSwept = true;

		ISpatialQuery query = engine.GetBSPTreeQuery()!;
		ISpatialLeafEnumerator queryRef = shadowEnum;
		query.EnumerateLeavesAlongRay(in ray, ref queryRef, 0);
	}

	void BuildOrthoShadow(IClientRenderable? renderable, ClientShadowHandle_t handle, in Vector3 mins, in Vector3 maxs) {
		Span<Vector3> vec = stackalloc Vector3[3];
		MathLib.AngleVectors(renderable!.GetRenderAngles(), out vec[0], out vec[1], out vec[2]);
		vec[1] *= -1.0f;

		Vector3 shadowDir = GetShadowDirection(renderable);

		Vector3 localShadowDir = default;
		localShadowDir[0] = MathLib.DotProduct(vec[0], shadowDir);
		localShadowDir[1] = MathLib.DotProduct(vec[1], shadowDir);
		localShadowDir[2] = MathLib.DotProduct(vec[2], shadowDir);

		Span<int> vecIdx = stackalloc int[3];
		SortAbsVectorComponents(in localShadowDir, vecIdx);

		Vector3 xvec = vec[vecIdx[0]];
		Vector3 yvec = vec[vecIdx[1]];

		xvec -= shadowDir * MathLib.DotProduct(shadowDir, xvec);
		yvec -= shadowDir * MathLib.DotProduct(shadowDir, yvec);
		MathLib.VectorNormalize(ref xvec);
		MathLib.VectorNormalize(ref yvec);

		MathLib.VectorSubtract(in maxs, in mins, out Vector3 boxSize);

		Vector2 size = new(boxSize[vecIdx[0]], boxSize[vecIdx[1]]);
		size.X *= MathF.Abs(MathLib.DotProduct(vec[vecIdx[0]], xvec));
		size.Y *= MathF.Abs(MathLib.DotProduct(vec[vecIdx[1]], yvec));

		size.X += boxSize[vecIdx[2]] * MathF.Abs(MathLib.DotProduct(vec[vecIdx[2]], xvec));
		size.Y += boxSize[vecIdx[2]] * MathF.Abs(MathLib.DotProduct(vec[vecIdx[2]], yvec));

		size.X += 10.0f;
		size.Y += 10.0f;

		MathLib.Vector2DMax(in size, new Vector2(10.0f, 10.0f), out size);

		float falloffStart = ComputeLocalShadowOrigin(renderable, in mins, in maxs, in localShadowDir, 2.0f, out Vector3 org);

		Vector3 worldOrigin = renderable.GetRenderOrigin();
		MathLib.VectorMA(in worldOrigin, org.X, vec[0], out worldOrigin);
		MathLib.VectorMA(in worldOrigin, org.Y, vec[1], out worldOrigin);
		MathLib.VectorMA(in worldOrigin, org.Z, vec[2], out worldOrigin);

		float dx = 1.0f / TEXEL_SIZE_PER_CASTER_SIZE;
		worldOrigin.X = (int)(worldOrigin.X / dx) * dx;
		worldOrigin.Y = (int)(worldOrigin.Y / dx) * dx;
		worldOrigin.Z = (int)(worldOrigin.Z / dx) * dx;

		BuildGeneralWorldToShadowMatrix(out Shadows[handle].Shadow.WorldToShadow, in worldOrigin, in shadowDir, in xvec, in yvec);
		BuildWorldToTextureMatrix(in Shadows[handle].Shadow.WorldToShadow, in size, out Matrix4x4 matWorldToTexture);
		MathLib.Vector2DCopy(in size, out Shadows[handle].Shadow.WorldSize);

		float shadowCastDistance = GetShadowDistance(renderable);
		float maxHeight = shadowCastDistance + falloffStart;

		ShadowLeafEnum leafList = new();
		BuildShadowLeafList(leafList, in worldOrigin, in shadowDir, in size, maxHeight);
		Span<int> pLeafList = leafList.LeafList.AsSpan();

		g_ShadowMgr.ProjectShadow(Shadows[handle].Shadow.ShadowHandle, in worldOrigin,
			in shadowDir, in matWorldToTexture, in size, pLeafList, maxHeight, falloffStart, MAX_FALLOFF_AMOUNT, renderable.GetRenderOrigin());

		clientLeafSystem.ProjectShadow(Shadows[handle].Shadow.ClientLeafShadowHandle, pLeafList.Length, pLeafList);
	}

	void BuildRenderToTextureShadow(IClientRenderable? renderable, ClientShadowHandle_t handle, in Vector3 mins, in Vector3 maxs) => throw new NotImplementedException();

	void BuildFlashlight(ClientShadowHandle_t handle) => throw new NotImplementedException();

	void SetupRenderToTextureShadow(ClientShadowHandle_t h) {
		ref ClientShadow_t shadow = ref Shadows[h].Shadow;

		IClientRenderable? renderable = cl_entitylist.GetClientRenderableFromHandle(shadow.Entity);
		if (renderable == null)
			return;

		Vector3 mins, maxs;
		renderable.GetShadowRenderBounds(out mins, out maxs, GetActualShadowCastType(h));

		Vector3 size;
		MathLib.VectorSubtract(maxs, mins, out size);
		float maxSize = Math.Max(size.X, size.Y);
		maxSize = Math.Max(maxSize, size.Z);

		float texelCount = TEXEL_SIZE_PER_CASTER_SIZE * maxSize;

		int textureSize = 1;
		while (textureSize < texelCount)
			textureSize <<= 1;

		shadow.ShadowTexture = ShadowAllocator.AllocateTexture(textureSize, textureSize);
	}
	void CleanUpRenderToTextureShadow(ClientShadowHandle_t h) {
		ref ClientShadow_t shadow = ref Shadows[h].Shadow;
		if (RenderToTextureActive && (shadow.Flags & (int)ClientShadowFlags.UseRenderToTexture) != 0) {
			ShadowAllocator.DeallocateTexture(shadow.ShadowTexture);
			shadow.ShadowTexture = INVALID_TEXTURE_HANDLE;
		}
	}

	void ComputeExtraClipPlanes(IClientRenderable? renderable, ClientShadowHandle_t handle, ReadOnlySpan<Vector3> vec, in Vector3 mins, in Vector3 maxs, in Vector3 localShadowDir) => throw new NotImplementedException();

	void ClearExtraClipPlanes(ClientShadowHandle_t h) => throw new NotImplementedException();
	void AddExtraClipPlane(ClientShadowHandle_t h, in Vector3 normal, float dist) => throw new NotImplementedException();

	bool CullReceiver(ClientShadowHandle_t handle, IClientRenderable? renderable, IClientRenderable? sourceRenderable) {
		if ((Shadows[handle].Shadow.Flags & (int)ShadowFlags.Flashlight) != 0) {
			Assert(sourceRenderable == null);
			Frustum_t frustum = g_ShadowMgr.GetFlashlightFrustum(Shadows[handle].Shadow.ShadowHandle);

			renderable!.GetRenderBoundsWorldspace(out Vector3 mins, out Vector3 maxs);

			return MathLib.R_CullBox(mins, maxs, frustum);
		}

		Assert(sourceRenderable != null);
		ComputeBoundingSphere(renderable, out Vector3 origin, out float radius);

		ref ClientShadow_t shadow = ref Shadows[handle].Shadow;
		ref readonly Source.Common.Engine.ShadowInfo_t info = ref g_ShadowMgr.GetInfo(shadow.ShadowHandle);
		MathLib.Vector3DMultiplyPosition(shadow.WorldToShadow, origin, out Vector3 localOrigin);

		Vector3 shadowMin = new(-shadow.WorldSize.X * 0.5f, -shadow.WorldSize.Y * 0.5f, 0);
		Vector3 shadowMax = new(shadow.WorldSize.X * 0.5f, shadow.WorldSize.Y * 0.5f, info.MaxDist);

		if (!CollisionUtils.IsBoxIntersectingSphere(shadowMin, shadowMax, localOrigin, radius))
			return true;

		ComputeBoundingSphere(sourceRenderable, out Vector3 originSource, out float radiusSource);

		bool foundSeparatingPlane;
		CollisionPlane plane;
		if (!CollisionUtils.IsSphereIntersectingSphere(originSource, radiusSource, origin, radius)) {
			foundSeparatingPlane = true;
			plane = default;

			MathLib.VectorSubtract(origin, originSource, out plane.Normal);
		}
		else
			foundSeparatingPlane = ComputeSeparatingPlane(renderable, sourceRenderable, out plane);

		if (foundSeparatingPlane) {
			Vector3 shadowDir = GetShadowDirection(sourceRenderable);
			float shadowDot = MathLib.DotProduct(shadowDir, plane.Normal);
			float receiverDot = MathLib.DotProduct(plane.Normal, origin);
			float sourceDot = MathLib.DotProduct(plane.Normal, originSource);

			if (shadowDot > 0.0f) {
				if (receiverDot <= sourceDot)
					return true;
			}
			else {
				if (receiverDot >= sourceDot)
					return true;
			}
		}

		return false;
	}

	bool ComputeSeparatingPlane(IClientRenderable? rend1, IClientRenderable? rend2, out CollisionPlane plane) {
		rend1!.GetShadowRenderBounds(out Vector3 min1, out Vector3 max1, GetActualShadowCastType(rend1));
		rend2!.GetShadowRenderBounds(out Vector3 min2, out Vector3 max2, GetActualShadowCastType(rend2));
		return CollisionUtils.ComputeSeparatingPlane(rend1.GetRenderOrigin(), rend1.GetRenderAngles(), min1, max1, rend2.GetRenderOrigin(), rend2.GetRenderAngles(), min2, max2, 3.0f, out plane);
	}

	void UpdateAllShadows() {
		foreach (ClientShadowHandle_t i in ValidShadowHandles) {
			ref ClientShadow_t shadow = ref Shadows[i].Shadow;

			if ((shadow.Flags & (int)ShadowFlags.Flashlight) != 0)
				continue;

			IClientRenderable? renderable = cl_entitylist.GetClientRenderableFromHandle(shadow.Entity);
			if (renderable == null)
				continue;

			Assert(renderable.GetShadowHandle() == i);
			UpdateProjectedTextureInternal(i, false);
		}
	}

	bool DrawRenderToTextureShadow(ushort clientShadowHandle, float area) => throw new NotImplementedException();
	void DrawRenderToTextureShadowLOD(ushort clientShadowHandle) => throw new NotImplementedException();

	bool DrawShadowHierarchy(IClientRenderable? renderable, in ClientShadow_t shadow, bool child = false) => throw new NotImplementedException();

	bool BuildSetupListForRenderToTextureShadow(ushort clientShadowHandle, float area) => throw new NotImplementedException();
	bool BuildSetupShadowHierarchy(IClientRenderable? renderable, in ClientShadow_t shadow, bool child = false) => throw new NotImplementedException();

	void SetRenderToTextureShadowTexCoords(ShadowHandle_t handle, int x, int y, int w, int h) => throw new NotImplementedException();

	void DrawRenderToTextureDebugInfo(IClientRenderable? renderable, in Vector3 mins, in Vector3 maxs) => throw new NotImplementedException();

	public void AdvanceFrame() => throw new NotImplementedException();

	float GetShadowDistance(IClientRenderable? renderable) {
		float dist = ShadowCastDist;

		renderable!.GetShadowCastDistance(ref dist, GetActualShadowCastType(renderable));

		return dist;
	}

	Vector3 GetShadowDirection(IClientRenderable? renderable) {
		Vector3 result = GetShadowDirection();

		renderable!.GetShadowCastDirection(ref result, GetActualShadowCastType(renderable));

		return result;
	}

	void InitDepthTextureShadows() {
		if (!DepthTextureActive) {
			DepthTextureActive = true;

			ImageFormat dstFormat = materials.GetShadowDepthTextureFormat();
			ImageFormat nullFormat = materials.GetNullTextureFormat();

			materials.BeginRenderTargetAllocation();

			DummyColorTexture = InitRenderTarget(r_flashlightdepthres.GetInt(), r_flashlightdepthres.GetInt(), RenderTargetSizeMode.Offscreen, nullFormat, MaterialRenderTargetDepth.None, false, "_rt_ShadowDummy");

			DepthTextureCache.Clear();
			DepthTextureCacheLocks.Clear();
			for (int i = 0; i < MaxDepthTextureShadows; i++) {
				bool @false = false;

				Span<char> strRTName = stackalloc char[64];
				sprintf(strRTName, "_rt_ShadowDepthTexture_%d").D(i);

				ITexture? depthTex = InitRenderTarget(DepthTextureResolution, DepthTextureResolution, RenderTargetSizeMode.Offscreen, dstFormat, MaterialRenderTargetDepth.None, false, strRTName.SliceNullTerminatedString());

				if (i == 0) {
					DepthTextureResolution = depthTex!.GetActualWidth();
					r_flashlightdepthres.SetValue(DepthTextureResolution);
				}

				DepthTextureCache.Add(depthTex);
				DepthTextureCacheLocks.Add(@false);
			}

			materials.EndRenderTargetAllocation();
		}
	}

	static ITexture? InitRenderTarget(int w, int h, RenderTargetSizeMode sizeMode, ImageFormat fmt, MaterialRenderTargetDepth depth, bool hdr, ReadOnlySpan<char> strOptionalName) {
		TextureFlags textureFlags = TextureFlags.ClampS | TextureFlags.ClampT;
		if (depth == MaterialRenderTargetDepth.Only)
			textureFlags |= TextureFlags.PointSample;

		CreateRenderTargetFlags renderTargetFlags = hdr ? CreateRenderTargetFlags.HDR : 0;

		ITexture? texture = materials.CreateNamedRenderTargetTextureEx(strOptionalName, w, h, sizeMode, fmt, depth, textureFlags, renderTargetFlags);

		Assert(texture != null);
		return texture;
	}

	void ShutdownDepthTextureShadows() {
		if (DepthTextureActive) {
			DummyColorTexture = null;

			while (DepthTextureCache.Count != 0) {
				DepthTextureCacheLocks.RemoveAt(DepthTextureCache.Count - 1);
				DepthTextureCache.RemoveAt(DepthTextureCache.Count - 1);
			}

			DepthTextureActive = false;
		}
	}

	void InitRenderToTextureShadows() {
		if (!RenderToTextureActive) {
			RenderToTextureActive = true;
			RenderShadow = materials.FindMaterial("decals/rendershadow", MaterialDefines.TEXTURE_GROUP_DECAL);
			RenderModelShadow = materials.FindMaterial("decals/rendermodelshadow", MaterialDefines.TEXTURE_GROUP_DECAL);
			ShadowAllocator.Init();

			ShadowAllocator.Reset();
			RenderTargetNeedsClear = true;

			float fr = AmbientLightColor.R / 255.0f;
			float fg = AmbientLightColor.G / 255.0f;
			float fb = AmbientLightColor.B / 255.0f;
			RenderShadow!.ColorModulate(fr, fg, fb);
			RenderModelShadow!.ColorModulate(fr, fg, fb);

			foreach (ClientShadowHandle_t i in ValidShadowHandles) {
				ref ClientShadow_t shadow = ref Shadows[i].Shadow;
				if ((shadow.Flags & (int)ClientShadowFlags.UseRenderToTexture) != 0) {
					SetupRenderToTextureShadow(i);
					MarkRenderToTextureShadowDirty(i);

					g_ShadowMgr.SetShadowMaterial(shadow.ShadowHandle, RenderShadow, RenderModelShadow, i);
				}
			}
		}
	}

	void ShutdownRenderToTextureShadows() {
		if (RenderToTextureActive) {
			foreach (ClientShadowHandle_t i in ValidShadowHandles) {
				CleanUpRenderToTextureShadow(i);

				ref ClientShadow_t shadow = ref Shadows[i].Shadow;
				g_ShadowMgr.SetShadowMaterial(shadow.ShadowHandle, SimpleShadow, SimpleShadow, CLIENTSHADOW_INVALID_HANDLE);
				g_ShadowMgr.SetShadowTexCoord(shadow.ShadowHandle, 0, 0, 1, 1);
				ClearExtraClipPlanes(i);
			}

			RenderShadow = null;
			RenderModelShadow = null;

			ShadowAllocator.DeallocateAllTextures();
			ShadowAllocator.Shutdown();

			// materials.UncacheUnusedMaterials();

			RenderToTextureActive = false;
		}
	}

	static bool ShadowHandleCompareFunc(ClientShadowHandle_t lhs, ClientShadowHandle_t rhs) => lhs < rhs;

	ClientShadowHandle_t CreateProjectedTexture(ClientEntityHandle entity, int flags) {
		if ((flags & (int)ShadowFlags.Flashlight) == 0) {
			IClientRenderable? renderable = cl_entitylist.GetClientRenderableFromHandle(entity);
			if (renderable == null)
				return CLIENTSHADOW_INVALID_HANDLE;

			ModelType modelType = modelinfo.GetModelType(renderable.GetModel());
			if (modelType == ModelType.Brush)
				flags |= (int)ShadowFlags_t.BrushModel;
		}

		while (curShadowHandleIdx == CLIENTSHADOW_INVALID_HANDLE || Shadows.ContainsKey(curShadowHandleIdx))
			curShadowHandleIdx++;

		ClientShadowHandle_t h = curShadowHandleIdx++;
		Shadows[h] = new();
		ValidShadowHandles.Add(h);
		ref ClientShadow_t shadow = ref Shadows[h].Shadow;
		shadow.Entity = entity;
		shadow.ClientLeafShadowHandle = clientLeafSystem.AddShadow(h, (ushort)flags);
		shadow.Flags = (ushort)flags;
		shadow.RenderFrame = -1;
		shadow.LastOrigin = new(float.MaxValue, float.MaxValue, float.MaxValue);
		shadow.LastAngles = new(float.MaxValue, float.MaxValue, float.MaxValue);
		Assert((shadow.Flags & (int)ShadowFlags.Flashlight) == 0 != ((shadow.Flags & (int)ShadowFlags.Shadow) == 0));

		IMaterial? shadowMaterial = SimpleShadow;
		IMaterial? shadowModelMaterial = SimpleShadow;
		object? shadowProxyData = CLIENTSHADOW_INVALID_HANDLE;

		if (RenderToTextureActive && (flags & (int)ClientShadowFlags.UseRenderToTexture) != 0) {
			SetupRenderToTextureShadow(h);

			shadowMaterial = RenderShadow;
			shadowModelMaterial = RenderModelShadow;
			shadowProxyData = h;
		}

		if ((flags & (int)ClientShadowFlags.UseDepthTexture) != 0) {
			shadowMaterial = RenderShadow;
			shadowModelMaterial = RenderModelShadow;
			shadowProxyData = h;
		}

		ShadowCreateFlags createShadowFlags;
		if ((flags & (int)ShadowFlags.Flashlight) != 0)
			createShadowFlags = ShadowCreateFlags.Flashlight;
		else
			createShadowFlags = ShadowCreateFlags.CacheVerts;

		shadow.ShadowHandle = g_ShadowMgr.CreateShadowEx(shadowMaterial, shadowModelMaterial, shadowProxyData, (int)createShadowFlags);
		return h;
	}

	bool LockShadowDepthTexture(ref ITexture? shadowDepthTexture) => throw new NotImplementedException();
	public void UnlockAllShadowDepthTextures() => throw new NotImplementedException();

	public void SetFlashlightTarget(ClientShadowHandle_t shadowHandle, EHANDLE targetEntity) => throw new NotImplementedException();

	public void SetFlashlightLightWorld(ClientShadowHandle_t shadowHandle, bool lightWorld) => throw new NotImplementedException();

	bool IsFlashlightTarget(ClientShadowHandle_t shadowHandle, IClientRenderable? renderable) => throw new NotImplementedException();

	int BuildActiveShadowDepthList(in ViewSetup viewSetup, int maxDepthShadows, Span<ClientShadowHandle_t> activeDepthShadows) => throw new NotImplementedException();

	void SetViewFlashlightState(int activeFlashlightCount, ReadOnlySpan<ClientShadowHandle_t> activeFlashlights) => throw new NotImplementedException();
}
