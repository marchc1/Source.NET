using Game.Shared;

namespace Game.Client;

public class ViewportClientSystem : IGameSystem
{
	public bool Init() {
		clientMode!.Layout();
		return true;
	}

	public bool IsPerFrame() {
		return true;
	}

	public void LevelInitPostEntity() {

	}

	public void LevelInitPreEntity() {

	}

	public void LevelShutdownPostEntity() {

	}

	public void LevelShutdownPreClearSteamAPIContext() {

	}

	public void LevelShutdownPreEntity() {

	}

	public ReadOnlySpan<char> Name() => "ViewportClientSystem";

	public void OnRestore() {

	}

	public void OnSave() {

	}

	public void PostInit() {

	}

	public void PostRender() {

	}

	public void PreRender() {

	}

	public void SafeRemoveIfDesired() {

	}

	public void Shutdown() {

	}

	public void Update(double frametime) {

	}
}
