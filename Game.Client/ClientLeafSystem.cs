using Source;
using Source.Common;
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
		throw new NotImplementedException();
	}

	public void BuildRenderablesList(in SetupRenderInfo info) {
		// TODO: full implementation. Every prop physics for now (I am lazy and testing model rendering).
		foreach (var renderable in Renderables) {
			ref RenderableInfo i = ref renderable.Value.Info;
			if (i.Renderable is C_PhysicsProp || i.Renderable is Beam)
				AddRenderableToRenderList(info.RenderList!, i.Renderable, 0, i.RenderGroup);
		}
	}

	private void AddRenderableToRenderList(ClientRenderablesList renderList, IClientRenderable? renderable, ushort leaf, RenderGroup renderGroup, ClientRenderHandle_t renderHandle = 0, bool twoPass = false) {
		ref int curCount = ref renderList.RenderGroupCounts[(int)renderGroup];

		ref ClientRenderablesList.Entry entry = ref renderList[renderGroup, curCount];
		entry.Renderable = renderable;
		entry.WorldListInfoLeaf = leaf;
		entry.TwoPass = twoPass;
		entry.RenderHandle = renderHandle;
		curCount++;
	}

	public void ChangeRenderableRenderGroup(ClientRenderHandle_t handle, RenderGroup group) {
		throw new NotImplementedException();
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

	public bool Init() {
		throw new NotImplementedException();
	}

	public bool IsPerFrame() {
		throw new NotImplementedException();
	}

	public bool IsRenderableInPVS(IClientRenderable renderable) {
		throw new NotImplementedException();
	}

	public void LevelInitPostEntity() {
		throw new NotImplementedException();
	}

	public void LevelInitPreEntity() {
		throw new NotImplementedException();
	}

	public void LevelShutdownPostEntity() {
		throw new NotImplementedException();
	}

	public void LevelShutdownPreClearSteamAPIContext() {
		throw new NotImplementedException();
	}

	public void LevelShutdownPreEntity() {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> Name() {
		throw new NotImplementedException();
	}

	public void OnRestore() {
		throw new NotImplementedException();
	}

	public void OnSave() {
		throw new NotImplementedException();
	}

	public void PostInit() {
		throw new NotImplementedException();
	}

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

		IClientRenderable renderable = Renderables[handle].Info.Renderable!;
		renderable.RenderHandle() = INVALID_CLIENT_RENDER_HANDLE;

		if ((Renderables[handle].Info.Flags & RenderFlags.HasChanged) != 0) {
			var i = DirtyRenderables.IndexOf(handle);
			Assert(i != -1);
			DirtyRenderables.RemoveAt(i);
		}
	}

	private void RemoveFromViewModelList(long handle) {
		int i = ViewModels.IndexOf(handle);
		Assert(i != -1);
		ViewModels.RemoveAt(i);
	}

	public void SafeRemoveIfDesired() {
		throw new NotImplementedException();
	}

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
