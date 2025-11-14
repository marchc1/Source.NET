using Source.Common;
using Source.Common.Engine;

namespace Game.Client;

public class ClientLeafSystem : IClientLeafSystem
{
	public void AddRenderable(IClientRenderable renderable, RenderGroup group) {
		throw new NotImplementedException();
	}

	public void AddRenderableToLeaves(uint renderable, Span<ushort> pLeaves) {
		throw new NotImplementedException();
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
