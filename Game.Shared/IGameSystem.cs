namespace Game.Shared;


public interface IGameSystemPerFrame : IGameSystem
{

#if CLIENT_DLL
	void PreRender();
	void Update(double frametime);
	void PostRender();
#else
	void FrameUpdatePreEntityThink();
	void FrameUpdatePostEntityThink();
	void PreClientUpdate();
#endif
}
public interface IGameSystem
{
	ReadOnlySpan<char> Name();

	bool Init();
	void PostInit();
	void Shutdown();

	void LevelInitPreEntity();
	void LevelInitPostEntity();

	void LevelShutdownPreClearSteamAPIContext();
	void LevelShutdownPreEntity();
	void LevelShutdownPostEntity();
	void OnSave();
	void OnRestore();
	void SafeRemoveIfDesired();

	bool IsPerFrame();


	static string? currentMapName;

	static ReadOnlySpan<char> MapName() => currentMapName;

	static readonly List<IGameSystem> SystemList = [];

	static void Add(IGameSystem sys) {
		SystemList.Add(sys);
	}
	static void Remove(IGameSystem sys) {
		SystemList.Remove(sys);
	}
	static void RemoveAll() {
		SystemList.Clear();
	}

	static bool InitAllSystems() {
		foreach (var sys in SystemList) {
			if (!sys.Init())
				return false;
		}
		return true;
	}

	static void PostInitAllSystems() {
		foreach (var sys in SystemList)
			sys.PostInit();
	}

	static void ShutdownAllSystems() {
		foreach (var sys in ((IEnumerable<IGameSystem>)SystemList).Reverse())
			sys.PostInit();
	}

	static void LevelInitPreEntityAllSystems(ReadOnlySpan<char> mapName) {
		currentMapName = new(mapName);
		foreach (var sys in SystemList)
			sys.LevelInitPreEntity();
	}

	static void LevelInitPostEntityAllSystems() {
		foreach (var sys in SystemList)
			sys.LevelInitPostEntity();
	}

	static void LevelShutdownPreClearSteamAPIContextAllSystems() {
		foreach (var sys in ((IEnumerable<IGameSystem>)SystemList).Reverse())
			sys.LevelShutdownPreClearSteamAPIContext();
	}

	static void LevelShutdownPreEntityAllSystems() {
		foreach (var sys in ((IEnumerable<IGameSystem>)SystemList).Reverse())
			sys.LevelShutdownPreEntity();
	}

	static void LevelShutdownPostEntityAllSystems() {
		foreach (var sys in ((IEnumerable<IGameSystem>)SystemList).Reverse())
			sys.LevelShutdownPostEntity();
	}

	static void OnSaveAllSystems() {
		foreach (var sys in ((IEnumerable<IGameSystem>)SystemList).Reverse())
			sys.OnSave();
	}

	static void OnRestoreAllSystems() {
		foreach (var sys in ((IEnumerable<IGameSystem>)SystemList).Reverse())
			sys.OnRestore();
	}

	static void SafeRemoveIfDesiredAllSystems() {
		foreach (var sys in ((IEnumerable<IGameSystem>)SystemList).Reverse())
			sys.SafeRemoveIfDesired();
	}

#if CLIENT_DLL
	static void PreRenderAllSystems() {
		// todo
	}
	static void UpdateAllSystems(double frametime) {
		// todo
	}
	static void PostRenderAllSystems() {
		// todo
	}
#else
	static void FrameUpdatePreEntityThinkAllSystems() {
		// todo
	}
	static void FrameUpdatePostEntityThinkAllSystems() {
		// todo
	}
	static void PreClientUpdateAllSystems() {
		// todo
	}
#endif
};

public class BaseGameSystem : IGameSystem
{
	public virtual bool Init() => true;
	public virtual bool IsPerFrame() => false;
	public virtual void LevelInitPostEntity() { }
	public virtual void LevelInitPreEntity() { }
	public virtual void LevelShutdownPostEntity() { }
	public virtual void LevelShutdownPreClearSteamAPIContext() { }
	public virtual void LevelShutdownPreEntity() { }
	public virtual ReadOnlySpan<char> Name() => "unnamed";
	public virtual void OnRestore() { }
	public virtual void OnSave() { }
	public virtual void PostInit() { }
	public virtual void SafeRemoveIfDesired() { }
	public virtual void Shutdown() { }
}
public class BaseGameSystemPerFrame : IGameSystemPerFrame
{
	public virtual bool Init() => true;
	public virtual bool IsPerFrame() => true;
	public virtual void LevelInitPostEntity() { }
	public virtual void LevelInitPreEntity() { }
	public virtual void LevelShutdownPostEntity() { }
	public virtual void LevelShutdownPreClearSteamAPIContext() { }
	public virtual void LevelShutdownPreEntity() { }
	public virtual ReadOnlySpan<char> Name() => "unnamed";
	public virtual void OnRestore() { }
	public virtual void OnSave() { }
	public virtual void PostInit() { }
	public virtual void SafeRemoveIfDesired() { }
	public virtual void Shutdown() { }

#if CLIENT_DLL
	public virtual void PreRender(){ }
	public virtual void Update(TimeUnit_t frametime){ }
	public virtual void PostRender(){ }
#else
	public virtual void FrameUpdatePreEntityThink() { }
	public virtual void FrameUpdatePostEntityThink() { }
	public virtual void PreClientUpdate() { }
#endif
}
public class AutoGameSystem : BaseGameSystem
{
	string name;
	public AutoGameSystem(ReadOnlySpan<char> name = default) {
		this.name = new(name);
	}
	public override ReadOnlySpan<char> Name() => name;
}

public class AutoGameSystemPerFrame : BaseGameSystemPerFrame
{
	string name;
	public AutoGameSystemPerFrame(ReadOnlySpan<char> name = default) {
		this.name = new(name);
	}
	public override ReadOnlySpan<char> Name() => name;
}
