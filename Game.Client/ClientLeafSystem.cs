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

public class ClientLeafSystem : IClientLeafSystem
{
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
