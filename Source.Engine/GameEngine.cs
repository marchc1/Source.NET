using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.Engine;

using static Source.Constants;

namespace Source.Engine;

public class GameEngine : IEngine
{
	const string DEFAULT_FPS_MAX_S = "300";

	static readonly ConVar fps_max = new("fps_max", DEFAULT_FPS_MAX_S, FCvar.Archive, "Frame rate limiter. 0 = unlimited", callback: FpsMaxChanged);
	static readonly ConVar fps_max_menu = new("fps_max_menu", "60", FCvar.Archive, "Frame rate limiter while in the main menu. 0 = use fps_max.");
	static readonly ConVar fps_max_nofocus = new("fps_max_nofocus", "20", FCvar.Archive, "Frame rate limiter when the game is not focused. 0 = use fps_max(_menu).");

	static void FpsMaxChanged(IConVar var, in ConVarChangeContext ctx) {
		if (Host.CanCheat())
			return;

		double fps = double.TryParse(ctx.New, out double d) ? d : 0;
		if (fps != 0 && fps < 30) {
			Warning("sv_cheats is 0 and fps_max is being limited to a minimum of 30 (or set to 0).\n");
			var.SetValue(30.0f);
		}
	}

	readonly Sys Sys;
	readonly IHostState HostState;
	readonly Host Host;
	IGame Game => field ??= Singleton<IGame>();
	private bool FilterTime(double dt) {
		if (sv.IsDedicated()) {
			MinFrameTime = Host.NextTick;
			return dt >= Host.NextTick;
		}

		MinFrameTime = 0;

		double fps = fps_max.GetDouble();

		if (!cl.IsConnected()) {
			double menu = fps_max_menu.GetDouble();
			if (menu > 0)
				fps = fps > 0 ? Math.Min(fps, menu) : menu;
		}

		if (!Game.IsActiveApp()) {
			double nofocus = fps_max_nofocus.GetDouble();
			if (nofocus > 0)
				fps = fps > 0 ? Math.Min(fps, nofocus) : nofocus;
		}

		if (fps > 0) {
			fps = Math.Clamp(fps, MIN_FPS, MAX_FPS);
			double minFrametime = 1 / fps;
			MinFrameTime = minFrametime;

			if (dt < minFrametime)
				return false;
		}

		return true;
	}

	IEngine.Quit Quitting;

	IEngine.State State;
	IEngine.State NextState;

	double CurrentTime;
	double FrameTime;
	double PreviousTime;
	double FilteredTime;
	double MinFrameTime;
	double LastRemainder;
	bool CatchupTime;

	public GameEngine(Sys Sys, IHostState HostState, Host Host) {
		this.Sys = Sys;
		this.HostState = HostState;
		this.Host = Host;

		State = IEngine.State.Inactive;
		NextState = IEngine.State.Inactive;
		CurrentTime = 0;
		FrameTime = 0;
		PreviousTime = 0;
		FilteredTime = 0;
		MinFrameTime = 0;
		LastRemainder = 0;
		CatchupTime = false;
		Quitting = IEngine.Quit.NotQuitting;
	}

	public bool Load(bool dedicated, string rootDirectory) {
		bool success = false;

		State = NextState = IEngine.State.Active;
		if (Sys.InitGame(dedicated, rootDirectory)) {
			success = true;
		}

		return success;
	}

	public void Unload() {
		Sys.ShutdownGame();
		State = IEngine.State.Inactive;
		NextState = IEngine.State.Inactive;
	}

	public void SetNextState(IEngine.State nextState) => NextState = nextState;
	public IEngine.State GetState() => State;

	public void Frame() {
		if (PreviousTime == 0) {
			FilterTime(0.0);
			PreviousTime = Sys.Time - MinFrameTime;
		}

		for (; ; ) {
			CurrentTime = Sys.Time;
			FrameTime = CurrentTime - PreviousTime;
			Assert(FrameTime >= 0);
			// TODO: handle ^^^

			if (FilterTime(FrameTime))
				break;

			double busyWaitMS = 2.25; // windows exclusive change later?

			int sleepMS = (int)((MinFrameTime - FrameTime) * 1000 - busyWaitMS);
			if (sleepMS > 0)
				Thread.Sleep(sleepMS);
			else {
				for (int i = 2000; i >= 0; i--) ;
			}
		}
		FilteredTime = 0;
		if (!sv.IsDedicated())
			g_ClientDLL?.FrameStageNotify(ClientFrameStage.Start);

		switch (State) {
			case IEngine.State.Paused:
			case IEngine.State.Inactive:
				break;
			case IEngine.State.Active:
			case IEngine.State.Close:
			case IEngine.State.Restart:
				HostState.Frame(FrameTime);
				break;
		}

		if (NextState != State) {
			State = NextState;
			switch (State) {
				case IEngine.State.Close: SetQuitting(IEngine.Quit.ToDesktop); break;
				case IEngine.State.Restart: SetQuitting(IEngine.Quit.Restart); break;
			}
		}

		PreviousTime = CurrentTime;
	}

	public double GetFrameTime() => FrameTime;
	public double GetCurTime() => CurrentTime;
	public IEngine.Quit GetQuitting() => Quitting;
	public void SetQuitting(IEngine.Quit quitType) => Quitting = quitType;
}
