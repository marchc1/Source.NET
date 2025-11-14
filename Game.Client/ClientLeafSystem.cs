using Source;
using Source.Common;
using Source.Common.Engine;

using System.Numerics;
using System.Runtime.InteropServices;

namespace Game.Client;

public enum RenderFlags : byte {
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

public struct ClientLeaf {
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

public class ClientLeafSystem : IClientLeafSystem
{
	readonly List<ClientLeaf> Leaf = [];
	readonly List<ClientRenderHandle_t> DirtyRenderables = [];
	readonly List<ClientRenderHandle_t> ViewModels = [];
	readonly LinkedList<RenderableInfo> Renderables = [];



	public void AddRenderable(IClientRenderable renderable, RenderGroup group) {
		RenderFlags flags = RenderFlags.HasChanged;
		if(group == RenderGroup.TwoPass) {
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

		var listObj = Renderables.AddLast(new RenderableInfo());
		ClientRenderHandle_t handle = GCHandle.ToIntPtr(GCHandle.Alloc(listObj, GCHandleType.Weak));
		ref RenderableInfo info = ref listObj.ValueRef;

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
		// todo later
	}

	public void ChangeRenderableRenderGroup(ClientRenderHandle_t handle, RenderGroup group) {
		throw new NotImplementedException();
	}

	public void CreateRenderableHandle(IClientRenderable? renderable, bool bIsStaticProp = false) {
		throw new NotImplementedException();
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
		throw new NotImplementedException();
	}

	public void RenderableChanged(ClientRenderHandle_t handle) {
		throw new NotImplementedException();
	}

	public void SafeRemoveIfDesired() {
		throw new NotImplementedException();
	}

	public void SetRenderGroup(ClientRenderHandle_t handle, RenderGroup group) {
		throw new NotImplementedException();
	}

	public void Shutdown() {
		throw new NotImplementedException();
	}
}
