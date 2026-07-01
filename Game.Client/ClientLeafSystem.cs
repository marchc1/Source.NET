using CommunityToolkit.HighPerformance;

using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;

using System.Numerics;
using System.Runtime.InteropServices;

namespace Game.Client;

public enum RenderFlags : byte
{
	TwoPass = 0x01,
	StaticProp = 0x02,
	BrushModel = 0x04,
	StudioModel = 0x08,
	HasChanged = 0x10,
	AlternateSorting = 0x20
}

public class ClientLeafSubSystemData
{
}

public struct RenderableInfo
{
	public IClientRenderable? Renderable;
	public long RenderFrame;
	public long RenderFrame2;
	public long EnumCount;
	public long TranslucencyCalculated;
	public uint LeafList;
	public uint RenderLeaf;
	public RenderFlags Flags;
	public RenderGroup RenderGroup;
	public uint FirstShadow;
	public short Area;
	public ViewID TranslucencyCalculatedView;
}

public struct ClientLeaf
{
	public uint FirstElement;
	public uint FirstShadow;
	public ushort FirstDetailProp;
	public ushort DetailPropCount;
	public int DetailPropRenderFrame;
	public InlineArray1<ClientLeafSubSystemData?> SubSystemData;
}

public struct ShadowInfo_t
{
	public uint FirstLeaf;
	public uint FirstRenderable;
	public int EnumCount;
	// public ClientShadowHandle_t Shadow;
	public ushort Flags;
}
public class EnumResult
{
	public int Leaf;
	public EnumResult? Next;
}

public class EnumResultList
{
	public EnumResult? Head;
	public ClientRenderHandle_t Handle;
}

class RenderableInfoBox
{
	public RenderableInfo Info = new();
}

public class ClientLeafSystem : IClientLeafSystem
{
	public const int CLSUBSYSTEM_DETAILOBJECTS = 0;
	public const int N_CLSUBSYSTEMS = 1;

	readonly List<ClientLeaf> Leaf = [];
	readonly List<ClientRenderHandle_t> DirtyRenderables = [];
	readonly List<ClientRenderHandle_t> ViewModels = [];
	readonly Dictionary<ClientRenderHandle_t, RenderableInfoBox> Renderables = [];
	readonly HashSet<ClientRenderHandle_t> ValidHandles = [];
	ClientRenderHandle_t curHandleIdx;
	ClientRenderHandle_t AllocHandle() {
		ClientRenderHandle_t handle = Interlocked.Increment(ref curHandleIdx);
		ValidHandles.Add(handle);
		return handle;
	}

	public void SetSubSystemDataInLeaf(int leaf, int subSystemIdx, ClientLeafSubSystemData? data) {
		Assert(subSystemIdx < N_CLSUBSYSTEMS);
		if (!Leaf.IsValidIndex(leaf)) {
			Assert(false);
			return;
		}
		ref ClientLeaf l = ref Leaf.AsSpan()[leaf];
		l.SubSystemData[subSystemIdx] = data;
	}

	public ClientLeafSubSystemData? GetSubSystemDataInLeaf(int leaf, int subSystemIdx) {
		Assert(subSystemIdx < N_CLSUBSYSTEMS);
		if (!Leaf.IsValidIndex(leaf)) {
			Assert(false);
			return null;
		}
		return Leaf[leaf].SubSystemData[subSystemIdx];
	}

	public void SetDetailObjectsInLeaf(int leaf, int firstDetailObject, int detailObjectCount) {
		if (!Leaf.IsValidIndex(leaf)) {
			Assert(false);
			return;
		}
		ref ClientLeaf l = ref Leaf.AsSpan()[leaf];
		l.FirstDetailProp = (ushort)firstDetailObject;
		l.DetailPropCount = (ushort)detailObjectCount;
	}

	public void GetDetailObjectsInLeaf(int leaf, out int firstDetailObject, out int detailObjectCount) {
		firstDetailObject = 0;
		detailObjectCount = 0;
		if (!Leaf.IsValidIndex(leaf)) {
			Assert(false);
			return;
		}
		firstDetailObject = Leaf[leaf].FirstDetailProp;
		detailObjectCount = Leaf[leaf].DetailPropCount;
	}

	public void DrawDetailObjectsInLeaf(int leaf, int frameNumber, out int firstDetailObject, out int detailObjectCount) {
		ref ClientLeaf leafInfo = ref Leaf.AsSpan()[leaf];
		leafInfo.DetailPropRenderFrame = frameNumber;
		firstDetailObject = leafInfo.FirstDetailProp;
		detailObjectCount = leafInfo.DetailPropCount;
	}



	public void AddRenderable(IClientRenderable renderable, RenderGroup group) {
		RenderFlags flags = RenderFlags.HasChanged;
		if (group == RenderGroup.TwoPass) {
			group = RenderGroup.TranslucentEntity;
			flags |= RenderFlags.TwoPass;
		}

		NewRenderable(renderable, group, flags);
		ClientRenderHandle_t handle = renderable.RenderHandle();
		DirtyRenderables.Add(handle);
	}

	private void NewRenderable(IClientRenderable renderable, RenderGroup type, RenderFlags flags) {
		Assert(renderable);
		Assert(renderable.RenderHandle() == INVALID_CLIENT_RENDER_HANDLE);

		ClientRenderHandle_t handle = AllocHandle();
		Renderables[handle] = new();
		ValidHandles.Add(handle);
		ref RenderableInfo info = ref Renderables[handle].Info;

		// We need to know if it's a brush model for shadows
		ModelType modelType = modelinfo.GetModelType(renderable.GetModel());
		if (modelType == ModelType.Brush)
			flags |= RenderFlags.BrushModel;
		else if (modelType == ModelType.Studio)
			flags |= RenderFlags.StudioModel;

		info.Renderable = renderable;
		info.RenderFrame = -1;
		info.RenderFrame2 = -1;
		info.TranslucencyCalculated = -1;
		info.TranslucencyCalculatedView = ViewID.Illegal;
		info.FirstShadow = unchecked((uint)-1);
		info.LeafList = unchecked((uint)-1);
		info.Flags = flags;
		info.RenderGroup = type;
		info.EnumCount = 0;
		info.RenderLeaf = unchecked((uint)-1);
		if (IsViewModelRenderGroup(info.RenderGroup))
			AddToViewModelList(handle);

		renderable.RenderHandle() = handle;
	}

	private void AddToViewModelList(ClientRenderHandle_t handle) {
		Assert(ViewModels.Find(handle) == -1);
		ViewModels.Add(handle);
	}

	private bool IsViewModelRenderGroup(RenderGroup renderGroup) {
		return renderGroup == RenderGroup.ViewModelTranslucent || renderGroup == RenderGroup.ViewModelOpaque;
	}

	public void AddRenderableToLeaves(ClientRenderHandle_t renderable, Span<ushort> pLeaves) {
		// throw new NotImplementedException();
		// TODO!!
	}

	public void BuildRenderablesList(in SetupRenderInfo info) {
		foreach (var renderable in Renderables) {
			ref RenderableInfo i = ref renderable.Value.Info;
			if (i.Renderable == null || !i.Renderable.ShouldDraw())
				continue;

			bool twoPass = (i.Flags & RenderFlags.TwoPass) != 0;
			AddRenderableToRenderList(info.RenderList!, i.Renderable, 0, i.RenderGroup, renderable.Key, twoPass);
		}
	}

	static readonly ConVar cl_drawleaf = new("cl_drawleaf", "-1", FCvar.Cheat);

	private void AddRenderableToRenderList(ClientRenderablesList renderList, IClientRenderable? renderable, ushort leaf, RenderGroup renderGroup, ClientRenderHandle_t renderHandle = 0, bool twoPass = false) {
#if DEBUG
		if (cl_drawleaf.GetInt() >= 0) {
			if (leaf != cl_drawleaf.GetInt())
				return;
		}
#endif

		Assert(renderGroup >= 0 && (int)renderGroup < ClientRenderablesList.RENDER_GROUP_COUNT);

		ref int curCount = ref renderList.RenderGroupCounts[(int)renderGroup];
		if (curCount < ClientRenderablesList.MAX_GROUP_ENTITIES) {
			Assert(leaf >= 0 && leaf <= 65535);

			ref ClientRenderablesList.Entry entry = ref renderList[renderGroup, curCount];
			entry.Renderable = renderable;
			entry.WorldListInfoLeaf = leaf;
			entry.TwoPass = twoPass;
			entry.RenderHandle = renderHandle;
			curCount++;
		}
		else {
			engine.Con_NPrintf(10, $"Warning: overflowed CClientRenderablesList group {(int)renderGroup}");
		}
	}

	public void ChangeRenderableRenderGroup(ClientRenderHandle_t handle, RenderGroup group) {
		ref RenderableInfo info = ref Renderables[handle].Info;
		info.RenderGroup = group;
	}

	public void CreateRenderableHandle(IClientRenderable? renderable, bool bIsStaticProp = false) {
		RenderGroup group = renderable!.IsTransparent() ? RenderGroup.TranslucentEntity : RenderGroup.OpaqueEntity;

		bool bTwoPass = false;
		if (group == RenderGroup.TranslucentEntity)
			bTwoPass = renderable.IsTwoPass();

		RenderFlags flags = 0;
		if (bIsStaticProp) {
			flags = RenderFlags.StaticProp;
			if (group == RenderGroup.OpaqueEntity)
				group = RenderGroup.OpaqueStatic;
		}

		if (bTwoPass)
			flags |= RenderFlags.TwoPass;

		NewRenderable(renderable, group, flags);
	}

	public void EnableAlternateSorting(ClientRenderHandle_t renderHandle, bool alternateSorting) {

	}

	public bool Init() => true;

	public bool IsPerFrame() => true;

	public bool IsRenderableInPVS(IClientRenderable renderable) {
		throw new NotImplementedException();
	}

	public void LevelInitPostEntity() { }

	public void LevelInitPreEntity() {
		Renderables.EnsureCapacity(1024);
		DirtyRenderables.EnsureCapacity(256);

		int leafCount = engine.LevelLeafCount();
		Leaf.EnsureCapacity(leafCount);

		ClientLeaf newLeaf = new() {
			FirstElement = unchecked((uint)-1),
			FirstShadow = unchecked((uint)-1),
			FirstDetailProp = 0,
			DetailPropCount = 0,
			DetailPropRenderFrame = -1
		};
		while (--leafCount >= 0)
			Leaf.Add(newLeaf);

#if DEBUG
		DevMsg($"ClientLeafSystem.LevelInitPreEntity: {Leaf.Count} leaves\n");
#endif
	}

	public void LevelShutdownPostEntity() {
		ViewModels.Clear();
		Renderables.Clear();
		Leaf.Clear();
		DirtyRenderables.Clear();
		ValidHandles.Clear();
	}

	public void LevelShutdownPreClearSteamAPIContext() { }

	public void LevelShutdownPreEntity() { }

	public ReadOnlySpan<char> Name() => "CClientLeafSystem";

	public void OnRestore() { }

	public void OnSave() { }

	public void PostInit() { }

	public void RemoveRenderable(ClientRenderHandle_t handle) {
		if (!ValidHandles.Contains(handle))
			return;

		IClientRenderable renderable = Renderables[handle].Info.Renderable!;
		renderable.RenderHandle() = INVALID_CLIENT_RENDER_HANDLE;

		if ((Renderables[handle].Info.Flags & RenderFlags.HasChanged) != 0) {
			var i = DirtyRenderables.IndexOf(handle);
			Assert(i != -1);
			DirtyRenderables.RemoveAt(i);
		}

		if (IsViewModelRenderGroup(Renderables[handle].Info.RenderGroup))
			RemoveFromViewModelList(handle);

		ValidHandles.Remove(handle);
		Renderables.Remove(handle);
	}

	public void RenderableChanged(ClientRenderHandle_t handle) {
		if (!ValidHandles.Contains(handle))
			return;

		if ((Renderables[handle].Info.Flags & RenderFlags.HasChanged) == 0) {
			Renderables[handle].Info.Flags |= RenderFlags.HasChanged;
			DirtyRenderables.Add(handle);
		}
		else {
			Assert(DirtyRenderables.IndexOf(handle) != -1);
		}
	}

	private void RemoveFromViewModelList(ClientRenderHandle_t handle) {
		int i = ViewModels.IndexOf(handle);
		Assert(i != -1);
		ViewModels.RemoveAt(i);
	}

	public void SafeRemoveIfDesired() { }

	public void SetRenderGroup(ClientRenderHandle_t handle, RenderGroup group) {
		ref RenderableInfo pInfo = ref Renderables[handle].Info;

		bool twoPass = false;
		if (group == RenderGroup.TwoPass) {
			twoPass = true;
			group = RenderGroup.TranslucentEntity;
		}

		if (twoPass)
			pInfo.Flags |= RenderFlags.TwoPass;
		else
			pInfo.Flags &= ~RenderFlags.TwoPass;

		bool bOldViewModelRenderGroup = IsViewModelRenderGroup(pInfo.RenderGroup);
		bool bNewViewModelRenderGroup = IsViewModelRenderGroup(group);
		if (bOldViewModelRenderGroup != bNewViewModelRenderGroup) {
			if (bOldViewModelRenderGroup) {
				RemoveFromViewModelList(handle);
			}
			else {
				AddToViewModelList(handle);
			}
		}

		pInfo.RenderGroup = group;
	}

	public void Shutdown() {
		throw new NotImplementedException();
	}

	public void CollateViewModelRenderables(List<IClientRenderable> opaque, List<IClientRenderable> translucent) {
		for (int i = ViewModels.Count - 1; i >= 0; --i) {
			ClientRenderHandle_t handle = ViewModels[i];
			ref RenderableInfo renderable = ref Renderables[handle].Info;

			// NOTE: In some cases, this removes the entity from the view model list
			renderable.Renderable!.ComputeFxBlend();

			// That's why we need to test RENDER_GROUP_OPAQUE_ENTITY - it may have changed in ComputeFXBlend()
			if (renderable.RenderGroup == RenderGroup.ViewModelOpaque || renderable.RenderGroup == RenderGroup.OpaqueEntity) {
				opaque.Add(renderable.Renderable);
			}
			else {
				translucent.Add(renderable.Renderable);
			}
		}
	}
}
