using Microsoft.Extensions.DependencyInjection;

using Source.Common;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Networking;
using Source.Common.Server;
using Source.Engine.Client;
using Source.Engine.Server;


using static Source.Constants;

namespace Source.Engine;

public class CommonHostState
{
	public double IntervalPerTick;
}

public class Host(
	EngineParms host_parms, CommonHostState host_state, GameServer sv,
	IServiceProvider services
	)
{
	// RaphaelIT7: Kill me later, though we really need access to things like ClientGlobalVariables without having to pass around everything!
	public static Host CurrentHost;
	public int TimeToTicks(double dt) => (int)(0.5f + (double)dt / (double)host_state.IntervalPerTick);
	public double TicksToTime(int dt) => (double)host_state.IntervalPerTick * (double)dt;

	public string GetCurrentMod() => host_parms.Mod;
	public string GetCurrentGame() => host_parms.Game;
	public string GetBaseDirectory() => host_parms.BaseDir;

	public ConVar host_name = new("hostname", "", 0, "Hostname for server.");
	public ConVar host_map = new("host_map", "", 0, "Current map name.");

	public ClientGlobalVariables clientGlobalVariables;
	public CL CL;
	public SV SV;
	public ServerGlobalVariables serverGlobalVariables;
	public Cbuf Cbuf;
	public ClientState cl;
	public Cmd Cmd;
	public Con Con;
	public EngineVGui EngineVGui;
	public Cvar Cvar;
	public View View;
	public Common Common;
	public IEngine Engine;
	public Scr Scr;
	public Net Net;
	public Sys Sys;
	public ClientDLL ClientDLL;
	public IHostState HostState;
	public IBaseClientDLL? clientDLL;
	public IServerGameDLL? serverDLL;

	public bool Initialized;
	public double FrameTime;
	public double FrameTimeUnbounded;
	public double FrameTimeStandardDeviation;
	public double RealTime;
	public double IdealTime;
	public double NextTick;
	public double[] JitterHistory = new double[128];
	public int JitterHistoryPos;
	public long FrameCount;
	public int HunkLevel;
	public int FrameTicks;
	public int TickCount;
	public int CurrentFrameTick;

	public int NumTicksLastFrame;
	public double RemainderLastFrame;
	public double PrevRemainderLastFrame;
	public double LastFrameTime;

	public bool IsSinglePlayerGame() {
		if (sv.IsActive())
			return !sv.IsMultiplayer();
		else
			return cl.MaxClients == 1;
	}

	void AccumulateTime(double dt) {
		RealTime += dt;
		FrameTime = dt;
		FrameTimeUnbounded = FrameTime;
		double fullscale = 1; // TODO: host_timescale
		FrameTime *= fullscale;
		FrameTimeUnbounded = FrameTime;
	}

	int gHostSpawnCount;

	public int GetServerCount() {
		if (cl.SignOnState >= SignOnState.New)
			return cl.ServerCount;
		else if (sv.State >= ServerState.Loading)
			return sv.GetSpawnCount();

		return gHostSpawnCount;
	}

	void _SetGlobalTime() {
		serverGlobalVariables.RealTime = RealTime;
		serverGlobalVariables.FrameCount = FrameCount;
		serverGlobalVariables.AbsoluteFrameTime = FrameTime;
		serverGlobalVariables.IntervalPerTick = host_state.IntervalPerTick;
		serverGlobalVariables.ServerCount = GetServerCount();
#if !SWDS
		clientGlobalVariables.RealTime = RealTime;
		clientGlobalVariables.FrameCount = FrameCount;
		clientGlobalVariables.AbsoluteFrameTime = FrameTime;
		clientGlobalVariables.IntervalPerTick = host_state.IntervalPerTick;
#endif
	}

	double Remainder;
	public double FramesPerSecond;

	void _RunFrame(double time) {
		double prevRemainder;
		bool shouldRender;
		int numTicks;

		AccumulateTime(time);
		_SetGlobalTime();

		shouldRender = !sv.IsDedicated();

		prevRemainder = Remainder;
		if (prevRemainder < 0)
			prevRemainder = 0;

		Remainder += FrameTime;
		numTicks = 0;
		if (Remainder >= host_state.IntervalPerTick) {
			numTicks = (int)Math.Floor(Remainder / host_state.IntervalPerTick);
			if (IsSinglePlayerGame() && false) { // alternateTicks!

			}

			Remainder -= (numTicks * host_state.IntervalPerTick);
		}

		NextTick = host_state.IntervalPerTick - Remainder;

		Cbuf.Execute();
		if (Net.Dedicated && !Net.IsMultiplayer())
			Net.SetMultiplayer(true);

		serverGlobalVariables.InterpolationAmount = 0;
#if !SWDS
		clientGlobalVariables.InterpolationAmount = 0;
		cl.InSimulation = true;
#endif

		FrameTicks = numTicks;
		CurrentFrameTick = 0;

#if !SWDS
		// engine tools?
#endif

#if !SWDS
		if (!EngineThreads.IsThreadedEngine())
#endif
		{
#if !SWDS
			if (clientDLL != null)
				clientDLL.IN_SetSampleTime(FrameTime);
			clientGlobalVariables.SimTicksThisFrame = 1;
#endif
			cl.TickRemainder = Remainder;
			serverGlobalVariables.SimTicksThisFrame = 1;
			cl.SetFrameTime(FrameTime);
			for (int tick = 0; tick < numTicks; tick++) {
				double now = Sys.Time;
				double jitter = now - IdealTime;
				JitterHistory[JitterHistoryPos] = jitter;
				JitterHistoryPos = (JitterHistoryPos + 1) % JitterHistory.Length;

				if (Math.Abs(jitter) > 1.0)
					IdealTime = now;
				else
					IdealTime = 0.99 * IdealTime + 0.01 * now;

				Net.RunFrame(now);
				bool finalTick = (tick == (numTicks - 1));
				if (Net.Dedicated && !Net.IsMultiplayer())
					Net.SetMultiplayer(true);

				serverGlobalVariables.TickCount = sv.TickCount;
				++TickCount;
				++CurrentFrameTick;
#if !SWDS
				clientGlobalVariables.TickCount = cl.GetClientTickCount();
				CL.CheckClientState();
#endif
				_RunFrame_Input(prevRemainder, finalTick);
				prevRemainder = 0;
				_RunFrame_Server(finalTick);
				if (!sv.IsDedicated())
					_RunFrame_Client(finalTick);
				IdealTime += host_state.IntervalPerTick;
				Net.SendQueuedPackets(); // ?
			}

			if (!sv.IsDedicated()) {
				SetClientInSimulation(false);
				clientGlobalVariables.InterpolationAmount = (cl.TickRemainder / host_state.IntervalPerTick);

				CL.RunPrediction(PredictionReason.Normal);
				CL.ApplyAddAngle();
				CL.ExtraMouseUpdate(clientGlobalVariables.FrameTime);
			}
		}
		else {
			int clientTicks, serverTicks;
			clientTicks = NumTicksLastFrame;
			cl.TickRemainder = RemainderLastFrame;
			cl.SetFrameTime(LastFrameTime);
			if (clientDLL != null)
				clientDLL.IN_SetSampleTime(LastFrameTime);

			LastFrameTime = FrameTime;

			serverTicks = numTicks;

			clientGlobalVariables.SimTicksThisFrame = clientTicks;
			serverGlobalVariables.SimTicksThisFrame = serverTicks;
			serverGlobalVariables.TickCount = sv.TickCount;

			for (int tick = 0; tick < clientTicks; tick++) {
				Net.RunFrame(Sys.Time);
				bool finalTick = (tick == (clientTicks - 1));

				if (Net.Dedicated && !Net.IsMultiplayer())
					Net.SetMultiplayer(true);

				clientGlobalVariables.TickCount = cl.GetClientTickCount();

				CL.CheckClientState();
				Net.SendQueuedPackets();
				if (!sv.IsDedicated())
					_RunFrame_Client(finalTick);
			}

			SetClientInSimulation(false);
			clientGlobalVariables.InterpolationAmount = (cl.TickRemainder / host_state.IntervalPerTick);

			CL.RunPrediction(PredictionReason.Normal);
			CL.ApplyAddAngle();
			SetClientInSimulation(true);

			long saveTick = clientGlobalVariables.TickCount;
			for (int tick = 0; tick < serverTicks; tick++) {
				++TickCount;
				++CurrentFrameTick;
				clientGlobalVariables.TickCount = TickCount;
				bool finalTick = tick == (serverTicks - 1);
				_RunFrame_Input(prevRemainder, finalTick);
				prevRemainder = 0;
				Net.RunFrame(Sys.Time);
			}

			SetClientInSimulation(false);

			CL.ExtraMouseUpdate(clientGlobalVariables.FrameTime);

			clientGlobalVariables.TickCount = saveTick;
			NumTicksLastFrame = numTicks;
			RemainderLastFrame = Remainder;

			Net.SetTime(Sys.Time);
			throw new Exception("We haven't done threaded engine yet...");
		}

		if (shouldRender) {
			_RunFrame_Render();
			_RunFrame_Sound();
		}

		if (!sv.IsDedicated()) {
			ClientDLL.Update();
		}

		Speeds();
		UpdateMapList();
		FrameCount++;

		// It may be a bad idea to put this here... whatever for now - but later figure out how it's *actually* done
		if (Sys.TextMode)
			_RunFrame_TextMode();

		PostFrameRate(FrameTime);
	}

	public char[] consoleText = new char[2048];
	public int consoleTextLen;
	public int cursorPosition;

	private void _RunFrame_TextMode() {
		while (Console.KeyAvailable) {
			var key = Console.ReadKey(true);
			switch (key.Key) {
				case ConsoleKey.UpArrow:
					ReceiveUpArrow();
					break;
				case ConsoleKey.DownArrow:
					ReceiveDownArrow();
					break;
				case ConsoleKey.LeftArrow:
					ReceiveLeftArrow();
					break;
				case ConsoleKey.RightArrow:
					ReceiveRightArrow();
					break;
				case ConsoleKey.Enter:
					ReadOnlySpan<char> line = ReceiveNewLine();
					if (line.Length > 0) {
						Cbuf.InsertText(line);
					}
					break;
				case ConsoleKey.Backspace:
					ReceiveBackspace();
					break;
				case ConsoleKey.Tab:
					ReceiveTab();
					break;
				default:
					char ch = key.KeyChar;
					if (ch >= ' ' && ch <= '~')
						ReceiveStandardChar(ch);
					break;
			}
		}
	}

	private void ReceiveUpArrow() {

	}

	private void ReceiveDownArrow() {

	}

	private void ReceiveLeftArrow() {
		if (cursorPosition <= 0)
			return;
		Console.Write('\b');
		cursorPosition--;
	}

	private void ReceiveRightArrow() {
		if (cursorPosition >= consoleTextLen)
			return;
		Console.Write(consoleText[cursorPosition]);
		cursorPosition++;
	}

	private void ReceiveTab() {

	}

	private void ReceiveBackspace() {
		int count;
		if (cursorPosition <= 0)
			return;
		consoleTextLen--;
		cursorPosition--;

		Console.Write('\b');
		for (count = cursorPosition; count < consoleTextLen; count++) {
			consoleText[count] = consoleText[count + 1];
			Console.Write(consoleText[count]);
		}

		Console.Write(' ');
		count = consoleTextLen;
		while (count >= cursorPosition) {
			Console.Write('\b');
			count--;
		}
	}

	private ReadOnlySpan<char> ReceiveNewLine() {
		Console.WriteLine();
		int len = 0;
		if (consoleTextLen > 0) {
			len = consoleTextLen;
			consoleTextLen = 0;
			cursorPosition = 0;
			return consoleText.AsSpan()[..len];
		}
		else
			return null;
	}

	private void ReceiveStandardChar(char ch) {
		int count;
		if (consoleTextLen >= (consoleText.Length - 2))
			return;

		count = consoleTextLen;
		while (count > cursorPosition) {
			consoleText[count] = consoleText[count - 1];
			count--;
		}

		consoleText[cursorPosition] = ch;

		Console.Write(new string(new ReadOnlySpan<char>(consoleText))[cursorPosition..(cursorPosition + (consoleTextLen - cursorPosition + 1))]);
		consoleTextLen++;
		cursorPosition++;
		count = consoleTextLen;
		while (count > cursorPosition) {
			Console.Write('\b');
			count--;
		}
	}

	const double FPS_AVG_FRAC = 0.9;

	private void PostFrameRate(double frameTime) {
		frameTime = Math.Clamp(frameTime, 0.0001, 1.0);
		double fps = 1.0 / frameTime;
		FramesPerSecond = fps * FPS_AVG_FRAC + (1.0 - FPS_AVG_FRAC) * fps;
	}

	private void UpdateMapList() {

	}

	private void Speeds() {

	}

	public void UpdateScreen() {
		Scr.UpdateScreen();
	}

	private void _RunFrame_Render() {
		UpdateScreen();
	}

	private void _RunFrame_Sound() {

	}

	public void SetClientInSimulation(bool v) {

	}

	private void _RunFrame_Client(bool finalTick) {
		CL.ReadPackets(finalTick);
		CL.ProcessVoiceData();

		cl.CheckUpdatingSteamResources();
		cl.CheckFileCRCsWithServer();

		cl.RunFrame();
	}

	private void _RunFrame_Server(bool finalTick) {

	}

	private bool input_firstFrame = true;

	public bool LowViolence { get; set; } = false;

	private void _RunFrame_Input(double accumulatedExtraSamples, bool finalTick) {
		if (input_firstFrame) {
			input_firstFrame = false;
			// test script?
		}

#if !SWDS
		ClientDLL.ProcessInput();
		Cbuf.Execute();
		CL.Move(accumulatedExtraSamples, finalTick);
#endif
	}

	public void RunFrame(double frameTime) {
		_RunFrame(frameTime);
	}

	public void PostInit() {
		var serverGameDLL = services.GetService<IServerGameDLL>();
		if (serverGameDLL != null)
			serverGameDLL.PostInit();
		serverDLL = serverGameDLL;

		var clientDLL = services.GetService<IBaseClientDLL>();
		if (clientDLL != null)
			clientDLL.PostInit();
		this.clientDLL = clientDLL;
		CurrentHost = this;
	}

	public void ReadConfiguration() {

	}

	public bool IsSecureServerAllowed() => true;

	public void Init(bool dedicated) {
		RealTime = 0;
		IdealTime = 0;


		host_state.IntervalPerTick = DEFAULT_TICK_INTERVAL;

		Engine = services.GetRequiredService<IEngine>();
		var engineAPI = services.GetRequiredService<IEngineAPI>();
		var hostState = services.GetRequiredService<IHostState>();
		Sys = services.GetRequiredService<Sys>();
		cl = services.GetRequiredService<ClientState>();

		clientGlobalVariables = services.GetRequiredService<ClientGlobalVariables>();
		serverGlobalVariables = services.GetRequiredService<ServerGlobalVariables>();

		Con = engineAPI.InitSubsystem<Con>()!;
		Cbuf = engineAPI.InitSubsystem<Cbuf>()!;
		Cmd = engineAPI.InitSubsystem<Cmd>()!;
		Cvar = engineAPI.InitSubsystem<Cvar>()!;
#if !SWDS
		View = engineAPI.InitSubsystem<View>()!;
#endif
		Common = engineAPI.InitSubsystem<Common>()!;
		//engineAPI.InitSubsystem<Filter>();
#if !SWDS
		//engineAPI.InitSubsystem<Key>();
#endif
		Net = engineAPI.InitSubsystem<Net>(dedicated)!;
		sv.Init(dedicated);
		SV = services.GetRequiredService<SV>();
		SV.InitGameDLL();
#if !SWDS
		if (!dedicated) {
			CL = engineAPI.InitSubsystem<CL>()!;
			EngineVGui = engineAPI.InitSubsystem<EngineVGui>()!;
			ClientDLL = engineAPI.InitSubsystem<ClientDLL>()!;
			HostState = engineAPI.GetRequiredService<IHostState>();
			Scr = engineAPI.InitSubsystem<Scr>()!;
			// engineAPI.InitSubsystem<Render>();
			// engineAPI.InitSubsystem<Decal>();
			CL.Init();
		}
		else {
			cl.SignOnState = SignOnState.None;
		}
#endif

#if !SWDS
		ReadConfiguration();
		// engineAPI.InitSubsystem<Sound>();
#endif
		Cbuf.AddText("exec valve.rc");

		Initialized = true;
		hostState.Init();

		PostInit();
	}

	public void Disconnect(bool showMainMenu, string? reason = null) {
#if !SWDS
		if (!sv.IsDedicated()) {
			cl.Disconnect(reason, showMainMenu);
		}
#endif
	}

	public void Disconnect() {
		Disconnect(true);
	}

	[ConCommand]
	void disconnect(in TokenizedCommand args) {
		if (clientDLL == null || !clientDLL.DisconnectAttempt()) {
			string test = args.ArgS(1).ToString();
			if (string.IsNullOrEmpty(test))
				Disconnect();
			else
				Disconnect(false, test.ToString());
		}
	}

	public bool CanCheat() {
		return SV.sv_cheats.GetBool();
	}

	internal void CheckGore() {
		// todo
	}

	public bool ChangeLevel(bool loadFromSavedGame, ReadOnlySpan<char> levelName, ReadOnlySpan<char> landmarkName) {
		if (!sv.IsActive()) {
			Dbg.ConMsg("Only the server may changelevel\n");
			return false;
		}

#if !SWDS
		Scr.BeginLoadingPlaque();
		// stop sounds
#endif

		sv.InactivateClients();
		// do the rest later
		return true;
	}

	public bool NewGame(ReadOnlySpan<char> mapName, bool loadGame, bool backgroundLevel, ReadOnlySpan<char> oldMap, ReadOnlySpan<char> landmark, bool oldSave) {
#if !SWDS
		Scr.BeginLoadingPlaque();
#endif

		// do the rest later
		return true;
	}



	delegate void printer(ReadOnlySpan<char> text);

	[ConCommand(helpText: "Display map and connection status.")]
	void status(in TokenizedCommand args) {
		printer print;
		if (Cmd.Source == CommandSource.Command) {
			if (!sv.IsActive()) {
				Cmd.ForwardToServer(in args);
				return;
			}

			print = (txt) => Dbg.ConMsg(txt);
		}
		else {
			print = Client_Print;
		}

		print($"hostname: {host_name.GetString()}\n");
	}

	public void Client_Print(ReadOnlySpan<char> text) {

	}

	public void BuildConVarUpdateMessage(NET_SetConVar convars, FCvar flags, bool nonDefault) {
		int count = CountVariablesWithFlags(flags, nonDefault);
		if (count <= 0)
			return;

		if (count > 255) {
			Sys.Error($"Engine only supported 255 ConVars marked {flags}\n");
		}

		foreach (var var in Cvar.GetCommands()) {
			if (var.IsCommand())
				continue;

			ConVar convar = (ConVar)var;
			if (!convar.IsFlagSet(flags))
				continue;

			if (nonDefault && convar.GetDefault() != convar.GetString())
				continue;

			cvar_s acvar = new();
			acvar.Name = convar.GetName();
			acvar.Value = CleanupConVarStringValue(convar.GetString());
			convars.ConVars.Add(acvar);
		}
	}

	[ConCommand(helpText: "Exits the engine")]
	void quit(in TokenizedCommand args) {
#if !SWDS
		if (args.FindArg("prompt") != null) {
			// EngineVGui.ConfirmQuit();
			return;
		}

		// TODO: game events.
		HostState.Shutdown();
#endif
	}

	public string CleanupConVarStringValue(string v) {
		// todo.
		return v;
	}

	public int CountVariablesWithFlags(FCvar flags, bool nonDefault) {
		int count = 0;
		foreach (var var in Cvar.GetCommands()) {
			if (var.IsCommand())
				continue;

			ConVar convar = (ConVar)var;
			if (!convar.IsFlagSet(flags))
				continue;

			if (nonDefault && convar.GetDefault() != convar.GetString())
				continue;

			count++;
		}

		return count;
	}

	internal void AllowQueuedMaterialSystem(bool v) {
		// todo
	}

	[ConCommand(helpText: "Reload the most recent saved game (add setpos to jump to current view position on reload).")]
	void reload(in TokenizedCommand args, CommandSource source, int clientSlot = -1) {
		if (
#if !SWDS
#endif
			!sv.IsActive())
			return;

		if (sv.IsMultiplayer())
			return;

		if (source != CommandSource.Command)
			return;

		bool rememberLocation = args.ArgC() == 2 && args[1].Equals("setpos", StringComparison.OrdinalIgnoreCase);
		Scr.BeginLoadingPlaque();
		Disconnect(false);
		Dbg.Msg("reload incomplete!\n");
	}

	public void ShutdownServer() {
		if (!sv.IsActive())
			return;

		AllowQueuedMaterialSystem(false);
#if !SWDS

#endif
		// static prop manager
		// free state and world
		sv.Shutdown();
		GC.WaitForPendingFinalizers();
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive);
	}
}