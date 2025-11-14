using Source.Common;
using Source.Common.Engine;

using System.Numerics;

namespace Game.Client;

public enum RenderFlags {
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
	public byte Flags;
	public RenderGroup RenderGroup;
	public uint  FirstShadow;
	public short Area;
	public sbyte TranslucencyCalculatedView;
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



	public void AddRenderable(IClientRenderable renderable, RenderGroup group) {
		RenderFlags flags = RenderFlags.HasChanged;
	}

	public void AddRenderableToLeaves(uint renderable, Span<ushort> pLeaves) {
		throw new NotImplementedException();
	}

	public void BuildRenderablesList(in SetupRenderInfo info) {
		// todo later
	}

	public void ChangeRenderableRenderGroup(uint handle, RenderGroup group) {
		throw new NotImplementedException();
	}

	public void CreateRenderableHandle(IClientRenderable? renderable, bool bIsStaticProp = false) {
		throw new NotImplementedException();
	}

	public void EnableAlternateSorting(uint renderHandle, bool alternateSorting) {

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

	public void RemoveRenderable(uint handle) {
		throw new NotImplementedException();
	}

	public void RenderableChanged(uint handle) {
		throw new NotImplementedException();
	}

	public void SafeRemoveIfDesired() {
		throw new NotImplementedException();
	}

	public void SetRenderGroup(uint handle, RenderGroup group) {
		throw new NotImplementedException();
	}

	public void Shutdown() {
		throw new NotImplementedException();
	}
}
