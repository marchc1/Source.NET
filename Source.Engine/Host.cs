using Microsoft.Extensions.DependencyInjection;

using Source.Common;
using Source.Common.Audio;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Common.Input;
using Source.Common.Networking;
using Source.Common.Server;
using Source.Engine.Client;
using Source.Engine.Server;

using Steamworks;

using System.Runtime.CompilerServices;

using static Source.Constants;

namespace Source.Engine;

public class CommonHostState
{
	public Model? WorldModel;
	public WorldBrushData? WorldBrush;
	public TimeUnit_t IntervalPerTick;
	public void SetWorldModel(Model? model) {
		WorldModel = model;
		if (model != null)
			WorldBrush = model.Brush.Shared;
		else
			WorldBrush = null;
	}
}

public class Host(
	EngineParms host_parms, CommonHostState host_state,
	IServiceProvider services, ICommandLine CommandLine, IFileSystem fileSystem
	)
{
	public int TimeToTicks(TimeUnit_t dt) => (int)(0.5 + dt / host_state.IntervalPerTick);
	public TimeUnit_t TicksToTime(int dt) => host_state.IntervalPerTick * dt;

	public string GetCurrentMod() => host_parms.Mod;
	public string GetCurrentGame() => host_parms.Game;
	public string GetBaseDirectory() => host_parms.BaseDir;

	public ConVar host_name = new("hostname", "", 0, "Hostname for server.");
	public ConVar host_map = new("host_map", "", 0, "Current map name.");
	public ConVar developer = new("developer", "0", 0, "Set developer message level");

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	public ClientGlobalVariables clientGlobalVariables;
	public CL CL;
	public MatSysInterface MatSysInterface;
	public IModelLoader modelloader;
	public SV SV;
	public ServerGlobalVariables serverGlobalVariables;
	public Cbuf Cbuf;
	public Cmd Cmd;
	public Con Con;
	public Key Key;
	public EngineVGui EngineVGui;
	public Cvar Cvar;
	public View View;
	public Render Render;
	public Common Common;
	public IEngine Engine;
	public Scr Scr;
	public Net Net;
	public Sys Sys;
	public ISoundServices soundServices;
	public ClientDLL ClientDLL;
	public Sound Sound;
	public IHostState HostState;
	public IBaseClientDLL? clientDLL;
	public IGameEventManager2 GameEventManager;
	public IServerGameDLL? serverDLL;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

	public bool Initialized;
	public TimeUnit_t Time;
	public TimeUnit_t FrameTime;
	public TimeUnit_t FrameTimeUnbounded;
	public TimeUnit_t FrameTimeStandardDeviation;
	public TimeUnit_t RealTime;
	public TimeUnit_t IdealTime;
	public TimeUnit_t NextTick;
	public TimeUnit_t[] JitterHistory = new TimeUnit_t[128];
	public int JitterHistoryPos;
	public long FrameCount;
	public int HunkLevel;
	public int FrameTicks;
	public int TickCount;
	public int CurrentFrameTick;

	public int NumTicksLastFrame;
	public TimeUnit_t RemainderLastFrame;
	public TimeUnit_t PrevRemainderLastFrame;
	public TimeUnit_t LastFrameTime;

	public bool IsSinglePlayerGame() {
		if (sv.IsActive())
			return !sv.IsMultiplayer();
		else
			return cl.MaxClients == 1;
	}

	void AccumulateTime(TimeUnit_t dt) {
		RealTime += dt;
		FrameTime = dt;
		FrameTimeUnbounded = FrameTime;
		TimeUnit_t fullscale = 1; // TODO: host_timescale
		FrameTime *= fullscale;
		FrameTimeUnbounded = FrameTime;

		soundServices.SetSoundFrametime(dt, FrameTime);
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

	TimeUnit_t Remainder;
	public TimeUnit_t FramesPerSecond;

	void _RunFrame(TimeUnit_t time) {
		TimeUnit_t prevRemainder;
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
		if (!EngineThreads.IsEngineThreaded())
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
				TimeUnit_t now = Sys.Time;
				TimeUnit_t jitter = now - IdealTime;
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
#if !SWDS
		else
#endif
		{
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
		Time = TickCount * host_state.IntervalPerTick + cl.TickRemainder;

		// It may be a bad idea to put this here... whatever for now - but later figure out how it's *actually* done
		if (Sys.TextMode)
			_RunFrame_TextMode();

		PostFrameRate(FrameTime);
	}

	public char[] consoleText = new char[2048];
	public int consoleTextLen;
	public int cursorPosition;

	private void _RunFrame_TextMode() {
		while (!Console.IsInputRedirected && Console.KeyAvailable) {
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
	public static void DefaultMapFileName(ReadOnlySpan<char> fullMapName, Span<char> diskName) {
		sprintf(diskName, "maps/%s.bsp").S(fullMapName);
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

	const TimeUnit_t FPS_AVG_FRAC = 0.9;

	private void PostFrameRate(TimeUnit_t frameTime) {
		frameTime = Math.Clamp(frameTime, 0.0001, 1.0);
		TimeUnit_t fps = 1.0 / frameTime;
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
		UpdateSounds();
	}

	AudioState audioState;

	public void UpdateSounds() {
		if (cl.IsActive()) {
			Sound.Update(in audioState);
		}
		else {
			Sound.Update();
		}
	}

	public void SetClientInSimulation(bool inSimulation) {
		cl.InSimulation = inSimulation || cl.IsPaused();
		clientGlobalVariables.CurTime = cl.GetTime();
		clientGlobalVariables.FrameTime = cl.GetFrameTime();
	}

	private void _RunFrame_Client(bool finalTick) {
		CL.ReadPackets(finalTick);
		CL.ProcessVoiceData();

		cl.CheckUpdatingSteamResources();
		cl.CheckFileCRCsWithServer();

		cl.RunFrame();
	}

	private void _RunFrame_Server(bool finalTick) {
		SV.Frame(finalTick);
	}

	private bool input_firstFrame = true;

	public bool LowViolence { get; set; } = false;

	private void _RunFrame_Input(TimeUnit_t accumulatedExtraSamples, bool finalTick) {
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

	public void RunFrame(TimeUnit_t frameTime) {
		_RunFrame(frameTime);
	}

	public void PostInit() {
		if (SV.ServerGameDLL != null)
			SV.ServerGameDLL.PostInit();
		serverDLL = SV.ServerGameDLL;

		var clientDLL = services.GetService<IBaseClientDLL>();
		if (clientDLL != null)
			clientDLL.PostInit();
		this.clientDLL = clientDLL;
	}

	bool ConfigCfgExecuted = false;
	public void ReadConfiguration() {
		if (sv.IsDedicated())
			return;

		if (fileSystem == null)
			Sys.Error("Host_ReadConfiguration:  g_pFileSystem == NULL\n");

		bool saveconfig = false;

		if (fileSystem!.FileExists("cfg/config.cfg", "MOD"))
			Cbuf.AddText("exec config.cfg\n");
		else {
			Cbuf.AddText("exec config_default.cfg\n");
			saveconfig = true;
		}

		Cbuf.Execute();

		int numBinds = Key.CountBindings();
		if (numBinds == 0)
			UseDefaultBindings();
		else
			SetupNewBindings();

		Key.SetBinding(ButtonCode.KeyEscape, "cancelselect");

		if (Key.NameForBinding("toggleconsole").IsEmpty)
			Key.SetBinding(ButtonCode.KeyBackquote, "toggleconsole");

		ConfigCfgExecuted = true;

		if (saveconfig) {
			bool saveinit = Initialized;
			Initialized = true;
			WriteConfiguration();
			Initialized = saveinit;
		}
	}

	private void SetupNewBindings() {
		// todo
	}

	private void UseDefaultBindings() {
		// todo
	}

	public void WriteConfiguration(ReadOnlySpan<char> filename = default, bool allVars = false) {
		bool isUserRequested = !filename.IsEmpty;
		if (filename.IsEmpty)
			filename = "config.cfg";

		if (!Initialized)
			return;

		if (!isUserRequested && CommandLine.CheckParm("-default"))
			return;

		if (sv.IsDedicated())
			return;

		using MemoryStream str = new();
		using StreamWriter configBuff = new(str);

		configBuff.Write($"cfg/{filename}");
		fileSystem.CreateDirHierarchy("cfg", "MOD");
	}

	public bool IsSecureServerAllowed() => true;

	public void Init(bool dedicated) {
		RealTime = 0;
		IdealTime = 0;

		OverlayText.IsDeadFn += OverlayText_IsDeadFn;
		OverlayText.SetEndTimeFn += OverlayText_SetEndTimeFn;

		host_state.IntervalPerTick = DEFAULT_TICK_INTERVAL;

		Engine = services.GetRequiredService<IEngine>();
		var engineAPI = services.GetRequiredService<IEngineAPI>();
		var hostState = services.GetRequiredService<IHostState>();
		Sys = services.GetRequiredService<Sys>();

		clientGlobalVariables = services.GetRequiredService<ClientGlobalVariables>();
		serverGlobalVariables = services.GetRequiredService<ServerGlobalVariables>();

		Con = engineAPI.InitSubsystem<Con>()!;
		Cbuf = engineAPI.InitSubsystem<Cbuf>()!;
		Cmd = engineAPI.InitSubsystem<Cmd>()!;
		Cvar = engineAPI.InitSubsystem<Cvar>()!;
		soundServices = engineAPI.GetRequiredService<ISoundServices>();
#if !SWDS
		View = engineAPI.InitSubsystem<View>()!;
#endif
		Common = engineAPI.InitSubsystem<Common>()!;
		Key = engineAPI.InitSubsystem<Key>()!;
		//engineAPI.InitSubsystem<Filter>();
#if !SWDS
		//engineAPI.InitSubsystem<Key>();
#endif
		Net = engineAPI.InitSubsystem<Net>(dedicated)!;
		GameEventManager = engineAPI.InitSubsystem<IGameEventManager2>()!;
		sv.Init(dedicated);
		SV = services.GetRequiredService<SV>();
		SV.InitGameDLL();
#if !SWDS
		if (!dedicated) {
			CL = engineAPI.InitSubsystem<CL>()!;
			MatSysInterface = engineAPI.InitSubsystem<MatSysInterface>()!;
			modelloader = engineAPI.InitSubsystem<IModelLoader>()!;
			EngineVGui = engineAPI.InitSubsystem<EngineVGui>()!;
			ClientDLL = engineAPI.InitSubsystem<ClientDLL>()!;
			HostState = engineAPI.GetRequiredService<IHostState>();
			Scr = engineAPI.InitSubsystem<Scr>()!;
			Render = engineAPI.InitSubsystem<Render>()!;
			// engineAPI.InitSubsystem<Decal>();
		}
		else {
			cl.SignOnState = SignOnState.None;
		}
#endif

#if !SWDS
		ReadConfiguration();
		Sound = engineAPI.InitSubsystem<Sound>()!;
#endif
		Cbuf.AddText("exec valve.rc");

		Initialized = true;
		hostState.Init();

		PostInit();
	}

	public void Shutdown() {
		OverlayText.IsDeadFn -= OverlayText_IsDeadFn;
		OverlayText.SetEndTimeFn -= OverlayText_SetEndTimeFn;

		Disconnect(true);
		Scr.DisabledForLoading = true;

#if !SWDS
		if (!sv.IsDedicated()) {
			// Decal.Shutdown();
			Render.Shutdown();
			Scr.Shutdown();
			Sound.Shutdown();
			ClientDLL.Shutdown();
			// TextMessageShutdown();
			EngineVGui.Shutdown();
			//StaticPropMgr.Shutdown();
			modelloader.Shutdown();
			// ShutdownStudioRender();
			// ShutdownMaterialSystem();
			CL.Shutdown();
		}
		else
#endif
		{
			// Decal.Shutdown();
			modelloader.Shutdown();
			// ShutdownStudioRender();
			// StaticPropMgr.Shutdown();
			// ShutdownMaterialSystem();
		}


		// HLTV.Shutdown();
		// Log.Shutdown();
		// GameEventManager.Shutdown();

		sv.Shutdown();
		Net.Shutdown();

#if !SWDS
		Key.Shutdown();
#endif


		Common.Shutdown();

#if !SWDS
		View.Shutdown();
#endif

		Cvar.Shutdown();
		Cmd.Shutdown();
		Cbuf.Shutdown();
		Con.Shutdown();
	}

	private void OverlayText_SetEndTimeFn(OverlayText text, TimeUnit_t duration) {

	}

	private bool OverlayText_IsDeadFn(OverlayText text) {
		if (cl.IsPaused())
			return false;

		if (text.ServerCount != cl.ServerCount)
			return true;

		if (text.CreationTick != -1)
			return GetOverlayTick() > text.CreationTick;

		if (text.EndTime == 0)
			return false;

		return cl.GetTime() >= text.EndTime;
	}

	private long GetOverlayTick() {
		if (sv.IsActive())
			return sv.TickCount;
		return cl.GetClientTickCount();
	}

	public void Disconnect(bool showMainMenu, ReadOnlySpan<char> reason = default) {
#if !SWDS
		if (!sv.IsDedicated()) {
			cl.Disconnect(reason, showMainMenu);
		}
		HostState.GameShutdown();
#endif
	}

	public void Disconnect() {
		Disconnect(true);
	}

	[ConCommand(helpText: "Print version info string.")]
	void version() {
		// todo
		// We should probably just use the steam inf information. Right now build_number is implemented to use a custom solution in the meantime.
		// Can we use that + add some custom stuff for Source.NET?
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

	[ConCommand("map", "Start playing on specified map.", FCvar.DontRecord)]
	public void Map_f(in TokenizedCommand args, CommandSource source, int clientSlot = -1) {
		Map_Helper(in args, source, false, false, false);
	}

	private void Map_Helper(in TokenizedCommand args, CommandSource source, bool editmode, bool background, bool commentary) {
		if (source != CommandSource.Command)
			return;

		if (args.ArgC() < 2) {
			Warning("No map specified\n");
			return;
		}

		Span<char> mapName = stackalloc char[128];
		strcpy(mapName, args[1]);

		ReadOnlySpan<char> reason = null;
		// lots to do here still
		Disconnect(false);
		HostState.NewGame(mapName, false, background);
		// TODO: accept setpos, setang.
	}


	const int STATUS_COLUMN_LENGTH_LINEPREFIX = 1;
	const int STATUS_COLUMN_LENGTH_USERID = 6;
	const string STATUS_COLUMN_LENGTH_USERID_STR = "6";
	const int STATUS_COLUMN_LENGTH_NAME = 19;
	const int STATUS_COLUMN_LENGTH_STEAMID = 19;
	const int STATUS_COLUMN_LENGTH_TIME = 9;
	const int STATUS_COLUMN_LENGTH_PING = 4;
	const string STATUS_COLUMN_LENGTH_PING_STR = "4";
	const int STATUS_COLUMN_LENGTH_LOSS = 4;
	const string STATUS_COLUMN_LENGTH_LOSS_STR = "4";
	const int STATUS_COLUMN_LENGTH_STATE = 6;
	const int STATUS_COLUMN_LENGTH_ADDR = 21;

	ref struct StatusLineBuilder
	{
		int CurPosition;
		InlineArray512<char> Line;
		internal void AddColumnText(ReadOnlySpan<char> text, int columnWidth) {
			int len = (int)strlen(Line);

			if (CurPosition > len) {
				for (int i = len; i < CurPosition; i++) 
					Line[i] = ' ';

				Line[CurPosition] = '\0';
			}
			else if (len != 0) {
				Line[len] = ' ';
				Line[len + 1] = '\0';
			}
			StrTools.StrConcat(Line, text);
			CurPosition += columnWidth + 1;
		}
		internal void Reset() { CurPosition = 0; Line[0] = '\0'; }
		internal void InsertEmptyColumn(int columnWidth) => CurPosition += columnWidth + 1;
		internal unsafe ReadOnlySpan<char> GetLine() {
			return Line;
		}
	}

	public bool NewGame(ReadOnlySpan<char> mapName, bool loadGame, bool backgroundLevel, ReadOnlySpan<char> oldMap = default, ReadOnlySpan<char> landmark = default, bool oldSave = false) {
		Common.TimestampedLog("Host_NewGame");
		Span<char> previousMapName = stackalloc char[MAX_PATH];
		host_map.GetString().CopyTo(previousMapName);
#if !SWDS
		Scr.BeginLoadingPlaque();
#endif
		Span<char> _mapName = stackalloc char[MAX_PATH];
		Span<char> _mapFile = stackalloc char[MAX_PATH];
		mapName.CopyTo(_mapName);
		DefaultMapFileName(_mapName, _mapFile);

		SV.InitGameServerSteam();

		if (!modelloader.Map_IsValid(_mapFile)) {
			Scr.EndLoadingPlaque();
			return false;
		}

		DevMsg("---- Host_NewGame ----\n");
		host_map.SetValue(_mapName);
		if (!loadGame) {
			HostState.RunGameInit();
		}

		Net.SetMultiplayer(sv.IsMultiplayer());
		Net.ListenSocket(sv.Socket, true);

		if (host_name.GetString().Length == 0)
			host_name.SetValue(serverDLL!.GetGameDescription());

		sv.LevelMainMenuBackground = backgroundLevel;
		serverGlobalVariables.CurTime = sv.GetTime();

		Common.TimestampedLog("serverGameDLL.LevelInit");
#if !SWDS
		EngineVGui.UpdateProgressBar(LevelLoadingProgress.LevelInit);
#endif

		if (loadGame && !oldSave) {
			sv.SetPaused(true);
			sv.LoadGame = true;
			serverGlobalVariables.CurTime = sv.GetTime();
		}

		if (!SV.ActivateServer())
			return false;

		if (!sv.IsDedicated()) {
			Common.TimestampedLog("Stuff 'connect localhost' to console");

			Span<char> str = stackalloc char[512];
			sprintf(str, "connect localhost:%d listenserver").D(sv.GetUDPPort());
			Cbuf.AddText(str);
		}


		return true;
	}


	public EUniverse GetSteamUniverse() {
#if !SWDS
		return SteamUtils.GetConnectedUniverse();
#else
		return SteamGameServerUtils.GetConnectedUniverse();
#endif
	}


	delegate void PrinterFn(ReadOnlySpan<char> text);

	static ConVarRef sv_tags;
	[ConCommand(helpText: "Display map and connection status.")]
	void status(in TokenizedCommand args, CommandSource source, int clientslot) {
		PrinterFn print;
		if (source == CommandSource.Command) {
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
		ReadOnlySpan<char> pchSecureReasonString = "";
		ReadOnlySpan<char> pchUniverse = "";
		bool bGSSecure = Steam3Server().BSecure();
		if (!bGSSecure && Steam3Server().BWantsSecure())
			if (Steam3Server().BLoggedOn())
				pchSecureReasonString = " (secure mode enabled, connected to Steam3)";
			else
				pchSecureReasonString = " (secure mode enabled, disconnected from Steam3)";


		switch (GetSteamUniverse()) {
			case EUniverse.k_EUniversePublic:
				pchUniverse = "";
				break;
			case EUniverse.k_EUniverseBeta:
				pchUniverse = " (beta)";
				break;
			case EUniverse.k_EUniverseInternal:
				pchUniverse = " (internal)";
				break;
			case EUniverse.k_EUniverseDev:
				pchUniverse = " (dev)";
				break;
			default:
				pchUniverse = " (unknown)";
				break;
		}

		print($"version : {GetSteamInfIDVersionInfo().PatchVersion}/{Protocol.VERSION} {build_number()} {(bGSSecure ? "secure" : "insecure")}{pchSecureReasonString}{pchUniverse}\n");

		if (Net.IsMultiplayer()) {
			// todo
		}

		ConVarRef sv_registration_successful = new("sv_registration_successful", true );
		if (sv_registration_successful.IsValid()) {
			Span<char> sExtraInfo = stackalloc char[256];
			ConVarRef sv_registration_message = new( "sv_registration_message", true );
			if (sv_registration_message.IsValid()) {
				ReadOnlySpan<char> msg = sv_registration_message.GetString();
				if (!msg.IsEmpty)
					sprintf(sExtraInfo, "  (%s)").S(msg);
			}

			if (sv_registration_successful.GetBool()) 
				print($"account : logged in{sExtraInfo}\n");
			else 
				print($"account : not logged in{sExtraInfo}\n");
		}

		print($"map     : {sv.GetMapName()} at: {(int)MainViewOrigin()[0]} x, {(int)MainViewOrigin()[1]} y, {(int)MainViewOrigin()[2]} z\n");
		if (sv_tags.IsEmpty) sv_tags = new("sv_tags");
		print($"tags    : {sv_tags.GetString()}\n");

		int players = sv.GetNumClients();
		int nBots = sv.GetNumFakeClients();
		int nHumans = players - nBots;

		print($"players : {nHumans} humans, {nBots} bots ({sv.GetMaxClients()} max)\n");
		print($"edicts  : {sv.NumEdicts - sv.FreeEdicts} used of {sv.MaxEdicts} max\n");

		// the header for the status rows
		// print( "# userid %-19s %-19s connected ping loss state%s\n", "name", "uniqueid", cmd_source == src_command ? "  adr" : "" );
		StatusLineBuilder header = new();
		header.AddColumnText("#", STATUS_COLUMN_LENGTH_LINEPREFIX);
		header.AddColumnText("userid", STATUS_COLUMN_LENGTH_USERID);
		header.AddColumnText("name", STATUS_COLUMN_LENGTH_NAME);
		header.AddColumnText("uniqueid", STATUS_COLUMN_LENGTH_STEAMID);
		header.AddColumnText("connected", STATUS_COLUMN_LENGTH_TIME);
		header.AddColumnText("ping", STATUS_COLUMN_LENGTH_PING);
		header.AddColumnText("loss", STATUS_COLUMN_LENGTH_LOSS);
		header.AddColumnText("state", STATUS_COLUMN_LENGTH_STATE);
		if (source == CommandSource.Command) {
			header.AddColumnText("adr", STATUS_COLUMN_LENGTH_ADDR);
		}

		print($"{header.GetLine()}\n");

		for (int j = 0; j < sv.GetClientCount(); j++) {
			IClient? client = sv.GetClient(j);

			if (!client!.IsConnected())
				continue;
			Status_PrintClient(client, (source == CommandSource.Command), print);
		}
	}

	private void Status_PrintClient(IClient client, bool showAddress, PrinterFn print) {
		INetChannelInfo? nci = client.GetNetChannel();

		ReadOnlySpan<char> state = "challenging";
		if (client.IsActive())
			state = "active";
		else if (client.IsSpawned())
			state = "spawning";
		else if (client.IsConnected())
			state = "connecting";

		StatusLineBuilder builder = new();
		builder.AddColumnText("#", STATUS_COLUMN_LENGTH_LINEPREFIX);
		builder.AddColumnText($"{client.GetUserID()}", STATUS_COLUMN_LENGTH_USERID);
		builder.AddColumnText(client.GetClientName(), STATUS_COLUMN_LENGTH_NAME);
		builder.AddColumnText(client.GetNetworkIDString(), STATUS_COLUMN_LENGTH_STEAMID);

		if (nci != null) {
			builder.AddColumnText(Common.FormatSeconds(nci.GetTimeConnected()), STATUS_COLUMN_LENGTH_TIME);
			builder.AddColumnText($"{(int)(1000.0f * nci.GetAverageLatency(NetFlow.FLOW_OUTGOING))}", STATUS_COLUMN_LENGTH_PING);
			builder.AddColumnText($"{(int)(100.0f * nci.GetAverageLoss(NetFlow.FLOW_INCOMING))}", STATUS_COLUMN_LENGTH_LOSS);
			builder.AddColumnText(state, STATUS_COLUMN_LENGTH_STATE);
			if (showAddress)
				builder.AddColumnText(nci.GetAddress(), STATUS_COLUMN_LENGTH_ADDR);
		}
		else {
			builder.InsertEmptyColumn(STATUS_COLUMN_LENGTH_TIME);
			builder.InsertEmptyColumn(STATUS_COLUMN_LENGTH_PING);
			builder.InsertEmptyColumn(STATUS_COLUMN_LENGTH_LOSS);
			builder.AddColumnText(state, STATUS_COLUMN_LENGTH_STATE);
		}

		print($"{builder.GetLine()}\n");
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
		if (!args.FindArg("prompt").IsEmpty) {
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

	[ConCommand]
	void dumpstringtables() {
		SV.DumpStringTables();
#if !SWDS
		CL.DumpStringTables();
#endif
	}

	bool inerror;

	public void Error(ReadOnlySpan<char> error) {
		if (inerror)
			Sys.Error("Host_Error: recursively entered");
		inerror = true;

		if (sv.IsDedicated()) {
			Sys.Error($"Host_Error: {error}\n");
			return;
		}
#if !SWDS
		Scr.EndLoadingPlaque();
#endif
		ConMsg($"\nHost_Error: {error}\n\n");
		Disconnect(true, error);
		inerror = false;
	}

	static readonly ConVar singlestep = new("singlestep", "0", FCvar.Cheat, "Run engine in single step mode ( set next to 1 to advance a frame )");
	static readonly ConVar cvarNext = new("next", "0", FCvar.Cheat, "Set to 1 to advance to next frame ( when singlestep == 1 )");

	int ShouldRun_CurrentTick;
	public bool ShouldRun() {
		if (singlestep.GetInt() == 0)
			return true;

		if (cvarNext.GetInt() != 0) {
			if (ShouldRun_CurrentTick != (TickCount - 1)) {
				cvarNext.SetValue(0);
				return false;
			}

			return true;
		}
		else {
			ShouldRun_CurrentTick = TickCount;
			return false;
		}
	}

	public void EndGame(bool showMainMenu, ReadOnlySpan<char> message) {
		ConMsg($"Host_EndGame: {message}");
		Disconnect(showMainMenu);
		if (sv.IsDedicated()) {
			Sys.Error($"Host_EndGame: {message}\n");
			return;
		}
	}

	public readonly ConVar skill = new("skill", "1", FCvar.Archive, "Game skill level (1-3).", 1, 3);
	public readonly ConVar deathmatch = new("deathmatch", "0", FCvar.Notify | FCvar.InternalUse, "Running a deathmatch server.");
	public readonly ConVar coop = new("coop", "0", FCvar.Notify, "Cooperative play.");

	internal bool ValidGame() {
		if (sv.IsMultiplayer()) {
			if (deathmatch.GetInt() != 0)
				return true;
		}
		else
			return true;

		ConDMsg("Unable to launch game\n");
		return false;
	}
}
