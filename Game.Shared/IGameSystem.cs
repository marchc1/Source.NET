using Source;

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


	static string? s_MapName;
	static bool s_bSystemsInitted;

	static ReadOnlySpan<char> MapName() => s_MapName;


	delegate void GameSystemFunc(IGameSystem self);
	delegate void PerFrameGameSystemFunc(IGameSystemPerFrame self);
	delegate void PerFrameGameSystemFunc<T>(IGameSystemPerFrame self, T arg);

	static readonly List<IGameSystem> s_GameSystems = [];
	static readonly List<IGameSystemPerFrame> s_GameSystemsPerFrame = [];
	public static AutoGameSystem? s_pSystemList = null;
	public static AutoGameSystemPerFrame? s_pPerFrameSystemList = null;

	static void InvokeMethod(GameSystemFunc f) {
		int i;
		int c = s_GameSystems.Count;
		for (i = 0; i < c; ++i) {
			IGameSystem sys = s_GameSystems[i];
			f(sys);
		}
	}

	static void InvokeMethodReverseOrder(GameSystemFunc f) {
		int i;
		int c = s_GameSystems.Count();
		for (i = c; --i >= 0;) {
			IGameSystem sys = s_GameSystems[i];
			f(sys);
		}
	}

	static void InvokePerFrameMethod(PerFrameGameSystemFunc f) {
		int i;
		int c = s_GameSystemsPerFrame.Count;
		for (i = 0; i < c; ++i) {
			IGameSystemPerFrame sys = s_GameSystemsPerFrame[i];
			f(sys);
		}
	}
	static void InvokePerFrameMethod<T>(PerFrameGameSystemFunc<T> f, T arg1) {
		int i;
		int c = s_GameSystemsPerFrame.Count;
		for (i = 0; i < c; ++i) {
			IGameSystemPerFrame sys = s_GameSystemsPerFrame[i];
			f(sys, arg1);
		}
	}

	public static void Add(IGameSystem sys) {
		s_GameSystems.Add(sys);
		if (sys is IGameSystemPerFrame perframe)
			s_GameSystemsPerFrame.Add(perframe);
	}
	public static void Remove(IGameSystem sys) {
		s_GameSystems.Remove(sys);
		if (sys is IGameSystemPerFrame perframe)
			s_GameSystemsPerFrame.Remove(perframe);
	}
	public static void RemoveAll() {
		s_GameSystems.Clear();
		s_GameSystemsPerFrame.Clear();
	}

	public static bool InitAllSystems() {
		int i;

		{
			// first add any auto systems to the end
			AutoGameSystem? system = s_pSystemList;
			while (system != null) {
				if (s_GameSystems.Find(system) == -1)
					Add(system);
				else
					DevWarning(1, "AutoGameSystem already added to game system list!!!\n");
				system = system.Next;
			}
			s_pSystemList = null;
		}

		{
			AutoGameSystemPerFrame? system = s_pPerFrameSystemList;
			while (system != null) {
				if (s_GameSystems.Find(system) == -1)
					Add(system);
				else
					DevWarning(1, "AutoGameSystem already added to game system list!!!\n");

				system = system.Next;
			}
			s_pSystemList = null;
		}
		// Now remember that we are initted so new AutoGameSystems will add themselves automatically.
		s_bSystemsInitted = true;

		for (i = 0; i < s_GameSystems.Count; ++i) {
			IGameSystem sys = s_GameSystems[i];

			bool valid = sys.Init();
			if (!valid)
				return false;
		}

		return true;
	}

	public static void PostInitAllSystems() => InvokeMethod(static sys => sys.PostInit());
	public static void ShutdownAllSystems() => InvokeMethodReverseOrder(static sys => sys.Shutdown());
	public static void LevelInitPreEntityAllSystems(ReadOnlySpan<char> mapName) {
		s_MapName = new(mapName.SliceNullTerminatedString());
		InvokeMethod(static sys => sys.LevelInitPreEntity());
	}
	public static void LevelInitPostEntityAllSystems() => InvokeMethod(static sys => sys.LevelInitPostEntity());
	public static void LevelShutdownPreClearSteamAPIContextAllSystems() => InvokeMethodReverseOrder(static sys => sys.LevelShutdownPreClearSteamAPIContext());
	public static void LevelShutdownPreEntityAllSystems() => InvokeMethodReverseOrder(static sys => sys.LevelShutdownPreEntity());
	public static void LevelShutdownPostEntityAllSystems() {
		InvokeMethodReverseOrder(static sys => sys.LevelShutdownPostEntity());
		s_MapName = null;
	}
	public static void OnSaveAllSystems() => InvokeMethod(static sys => sys.OnSave());
	public static void OnRestoreAllSystems() => InvokeMethod(static sys => sys.OnRestore());
	public static void SafeRemoveIfDesiredAllSystems() => InvokeMethodReverseOrder(static sys => sys.SafeRemoveIfDesired());

#if CLIENT_DLL
	public static void PreRenderAllSystems() => InvokePerFrameMethod(static sys => sys.PreRender());
	public static void UpdateAllSystems(TimeUnit_t frametime) {
		SafeRemoveIfDesiredAllSystems();
		InvokePerFrameMethod(static (sys, frametime) => sys.Update(frametime), frametime);
	}
	public static void PostRenderAllSystems() => InvokePerFrameMethod(static sys => sys.PostRender());
#else
	public static void FrameUpdatePreEntityThinkAllSystems() => InvokePerFrameMethod(static sys => sys.FrameUpdatePreEntityThink());
	public static void FrameUpdatePostEntityThinkAllSystems() => InvokePerFrameMethod(static sys => sys.FrameUpdatePostEntityThink());
	public static void PreClientUpdateAllSystems() => InvokePerFrameMethod(static sys => sys.PreClientUpdate());
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
	public virtual void PreRender() { }
	public virtual void Update(TimeUnit_t frametime) { }
	public virtual void PostRender() { }
#else
	public virtual void FrameUpdatePreEntityThink() { }
	public virtual void FrameUpdatePostEntityThink() { }
	public virtual void PreClientUpdate() { }
#endif
}
public class AutoGameSystem : BaseGameSystem
{
	string name;
	public AutoGameSystem? Next;
	public AutoGameSystem(ReadOnlySpan<char> name = default) {
		this.name = new(name);

		if (IGameSystem.s_bSystemsInitted)
			IGameSystem.Add(this);
		else {
			Next = IGameSystem.s_pSystemList;
			IGameSystem.s_pSystemList = this;
		}
	}
	public override ReadOnlySpan<char> Name() => name;
}

public class AutoGameSystemPerFrame : BaseGameSystemPerFrame
{
	string name;
	public AutoGameSystemPerFrame? Next;

	public AutoGameSystemPerFrame(ReadOnlySpan<char> name = default) {
		this.name = new(name);

		if (IGameSystem.s_bSystemsInitted)
			IGameSystem.Add(this);
		else {
			Next = IGameSystem.s_pPerFrameSystemList;
			IGameSystem.s_pPerFrameSystemList = this;
		}
	}
	public override ReadOnlySpan<char> Name() => name;
}
