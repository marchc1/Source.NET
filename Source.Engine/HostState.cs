namespace Source.Engine;

using Source.Common.DataCache;
using Source.Common.Engine;
using Source.Common.Mathematics;

using System.Numerics;

using static Source.Dbg;
public enum HostStates
{
	NewGame,
	LoadGame,
	ChangeLevelSP,
	ChangeLevelMP,
	Run,
	GameShutdown,
	Shutdown,
	Restart
}
public class HostState : IHostState
{
	private readonly Host Host;
	private readonly IMDLCache mdlCache;
	public HostStates CurrentState;
	public HostStates NextState;
	public Vector3 Location;
	public QAngle Angles;
	public InlineArray128<char> LevelName;
	public InlineArray128<char> LandmarkName;
	public InlineArray128<char> SaveName;
	public double ShortFrameTime;
	public bool ActiveGame;
	public bool RememberingLocation;
	public bool BackgroundLevel;
	public bool WaitingForConnection;

	IEngine eng = null!;

	public HostState(Host Host, IMDLCache mdlCache) {
		this.Host = Host;
		this.mdlCache = mdlCache;
		((IHostState)this).Init();
	}

	void IHostState.Init() {
		eng = Host.Engine;
		SetState(HostStates.Run, true);
		CurrentState = HostStates.Run;
		NextState = HostStates.Run;
		ActiveGame = false;
		LevelName[0] = '\0';
		SaveName[0] = '\0';
		LandmarkName[0] = '\0';
		RememberingLocation = false;
		BackgroundLevel = false;
		Location = new();
		Angles = new();
		WaitingForConnection = false;
		ShortFrameTime = 1.0;
	}

	void IHostState.Frame(double time) {
		while (true) {
			HostStates oldState = CurrentState;
			switch (CurrentState) {
				case HostStates.NewGame:
					mdlCache.BeginMapLoad();
					State_NewGame();
					break;
				case HostStates.LoadGame:
					mdlCache.BeginMapLoad();
					State_LoadGame();
					break;
				case HostStates.ChangeLevelMP:
					mdlCache.BeginMapLoad();
					ShortFrameTime = 0.5;
					State_ChangeLevelMP();
					break;
				case HostStates.ChangeLevelSP:
					mdlCache.BeginMapLoad();
					ShortFrameTime = 1.5;
					State_ChangeLevelSP();
					break;
				case HostStates.Run:
					State_Run(time);
					break;
				case HostStates.GameShutdown:
					State_GameShutdown();
					break;
				case HostStates.Shutdown:
					State_Shutdown();
					break;
				case HostStates.Restart:
					// mdl cache...
					State_Restart();
					break;
			}

			if (oldState == HostStates.Run) break;
			if (oldState == HostStates.Shutdown || oldState == HostStates.Restart) break;
		}
	}

	void IHostState.RunGameInit() {
		Assert(!ActiveGame);
		ActiveGame = true;
	}

	void IHostState.NewGame(ReadOnlySpan<char> mapName, bool rememberLocation, bool background) {
		strcpy(LevelName, mapName);
		LandmarkName[0] = '\0';
		WaitingForConnection = true;
		BackgroundLevel = background;
		//if (rememberLocation) 
		//RememberLocation();

		SetNextState(HostStates.NewGame);
	}

	void IHostState.LoadGame(ReadOnlySpan<char> mapName, bool rememberLocation) {
		throw new NotImplementedException();
	}

	void IHostState.ChangeLevelSP(ReadOnlySpan<char> newLevel, ReadOnlySpan<char> landmarkName) {
		strcpy(LevelName, newLevel);
		strcpy(LevelName, landmarkName);
		SetNextState(HostStates.ChangeLevelSP);
	}

	void IHostState.ChangeLevelMP(ReadOnlySpan<char> newLevel, ReadOnlySpan<char> landmarkName) {
		strcpy(LevelName, newLevel);
		strcpy(LevelName, landmarkName);
		SetNextState(HostStates.ChangeLevelMP);
	}

	void IHostState.GameShutdown() {
		if (CurrentState != HostStates.Shutdown && CurrentState != HostStates.Restart && CurrentState != HostStates.GameShutdown)
			SetNextState(HostStates.GameShutdown);
	}

	void IHostState.Shutdown() => SetNextState(HostStates.Shutdown);

	void IHostState.Restart() => SetNextState(HostStates.Restart);

	bool IHostState.IsShuttingDown() => CurrentState == HostStates.Shutdown || CurrentState == HostStates.Restart || CurrentState == HostStates.GameShutdown;

	void IHostState.OnClientConnected() {

	}

	void IHostState.OnClientDisconnected() {

	}

	void IHostState.SetSpawnPoint(in Vector3 position, in QAngle angle) {
		Angles = angle;
		Location = position;
		RememberingLocation = true;
	}

	public void SetState(HostStates newState, bool clearNext) {
		CurrentState = newState;
		if (clearNext)
			NextState = newState;
	}

	public void SetNextState(HostStates nextState) {
		Assert(CurrentState == HostStates.Run);
		NextState = nextState;
	}

	// The State_ functions execute that states code right away. The external API queues the state changes to happen during
	// the state machines processing loop.

	protected void State_NewGame() {

	}
	protected void State_LoadGame() {

	}
	protected void State_ChangeLevelMP() {

	}
	protected void State_ChangeLevelSP() {

	}

	// TODO: IsClientActive, IsClientConnected?

	static bool firstRunFrame = true;
	protected void State_Run(double frameTime) {
		Host.RunFrame(frameTime);

		switch (NextState) {
			case HostStates.Run: break;
			case HostStates.LoadGame:
			case HostStates.NewGame:
				Host.Scr.BeginLoadingPlaque();
				goto case HostStates.GameShutdown;

			case HostStates.Shutdown:
			case HostStates.Restart:
			case HostStates.GameShutdown:
				SetState(HostStates.GameShutdown, false);
				break;

			case HostStates.ChangeLevelMP:
			case HostStates.ChangeLevelSP:
				SetState(NextState, true);
				break;

			default: SetState(HostStates.Run, true); break;
		}
	}
	protected void State_GameShutdown() {
		if (Host.serverDLL != null) {
			// todo
		}

		GameShutdown();
		Host.ShutdownServer();
		switch (NextState) {
			case HostStates.LoadGame:
			case HostStates.NewGame:
			case HostStates.Shutdown:
			case HostStates.Restart:
				SetState(NextState, true);
				break;
			default:
				SetState(HostStates.Run, true);
				break;
		}
	}

	private void GameShutdown() {
		if (ActiveGame) {
			Host.serverDLL?.GameShutdown();
			ActiveGame = false;
		}
	}

	protected void State_Shutdown() {
		eng.SetNextState(IEngine.State.Close);
	}
	protected void State_Restart() {
		State_Shutdown();
		eng.SetQuitting(IEngine.Quit.Restart);
	}
}
