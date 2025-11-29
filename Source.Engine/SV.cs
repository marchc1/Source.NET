
using Microsoft.Extensions.DependencyInjection;

using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Server;
using Source.Engine.Server;

using static Source.Common.OptimizedModel;

namespace Source.Engine;

/// <summary>
/// Various serverside methods. In Source, these would mostly be represented by
/// SV_MethodName's in the static global namespace
/// </summary>
public class SV(IServiceProvider services, Cbuf Cbuf, ED ED, Host Host, CommonHostState host_state, IEngineVGuiInternal EngineVGui, ICvar cvar, IModelLoader modelloader, ServerGlobalVariables serverGlobalVariables, Con Con, [FromKeyedServices(Realm.Server)] NetworkStringTableContainer networkStringTableContainerServer, IHostState HostState, ServerPlugin serverPluginHandler)
{
	public IServerGameDLL? ServerGameDLL;
	public IServerGameEnts? ServerGameEnts;
	public IServerGameClients? ServerGameClients;

	public static readonly ConVar sv_pure_kick_clients = new( "sv_pure_kick_clients", "1", 0, "If set to 1, the server will kick clients with mismatching files. Otherwise, it will issue a warning to the client." );
	public static readonly ConVar sv_pure_trace = new( "sv_pure_trace", "0", 0, "If set to 1, the server will print a message whenever a client is verifying a CRC for a file." );
	public static readonly ConVar sv_pure_consensus = new( "sv_pure_consensus", "5", 0, "Minimum number of file hashes to agree to form a consensus." );
	public static readonly ConVar sv_pure_retiretime = new( "sv_pure_retiretime", "900", 0, "Seconds of server idle time to flush the sv_pure file hash cache." );
	public static readonly ConVar sv_lan = new( "sv_lan", "0", 0, "Server is a lan server ( no heartbeat, no authentication, no non-class C addresses )" );

	public static ConVar sv_cheats = new(nameof(sv_cheats), "0", FCvar.Notify | FCvar.Replicated, "Allow cheats on server", callback: SV_CheatsChanged);

	private static void SV_CheatsChanged(IConVar var, in ConVarChangeContext ctx) {

	}

	internal void DumpStringTables() {

	}

	internal bool InitGameDLL() {
		Cbuf.Execute();
		if (sv.DLLInitialized)
			return true;

		ServerGameDLL = services.GetService<IServerGameDLL>();
		if (ServerGameDLL == null) {
			Warning("Failed to load server binary\n");
			goto IgnoreThisDLL;
		}

		ServerGameEnts = services.GetService<IServerGameEnts>();
		if (ServerGameEnts == null) {
			Warning("Could not get IServerGameEnts interface\n");
			goto IgnoreThisDLL;
		}

		ServerGameClients = services.GetService<IServerGameClients>();
		if (ServerGameClients == null) {
			Warning("Could not get IServerGameClients interface\n");
			goto IgnoreThisDLL;
		}

		sv.DLLInitialized = true;
		if (!ServerGameDLL.DLLInit(services))
			Host.Error("IDLLFunctions.DLLInit returned false.\n");

		if (Host.host_name.GetString().Length == 0)
			Host.host_name.SetValue(ServerGameDLL.GetGameDescription());

		InitSendTables(ServerGameDLL.GetAllServerClasses());
		host_state.IntervalPerTick = ServerGameDLL.GetTickInterval();
		sv.InitMaxClients();
		Cbuf.Execute();

		return true;
	IgnoreThisDLL:
		ServerGameDLL = null;
		ServerGameEnts = null;
		ServerGameClients = null;
		return false;
	}

	private void InitSendTables(ServerClass? classes) {
		SendTable[] tables = new SendTable[Constants.MAX_DATATABLES];
		int numTables = BuildSendTablesArray(classes, tables);
		services.GetRequiredService<EngineSendTable>().Init(tables.AsSpan()[..numTables]);
	}

	private int BuildSendTablesArray(ServerClass? classes, SendTable[] tables) {
		int i = 0;
		while (classes != null) {
			tables[i++] = classes.Table;
			classes = classes.Next;
		}
		return i;
	}

	internal void ShutdownGameDLL() {

	}

	public void AllocateEdicts() {
		sv.Edicts = new Edict[sv.MaxEdicts];
		for (int i = 0; i < sv.MaxEdicts; i++) {
			sv.Edicts[i] = new();
			sv.Edicts[i].EdictIndex = i;
			sv.Edicts[i].FreeTime = 0;
		}
		ED.ClearFreeEdictList();
		// TODO: EdictChangeInfo
	}

	internal void InitGameServerSteam() {
		if (sv.IsMultiplayer()) {
			Steam3Server().Activate(ServerType.Normal);
			sv.SetQueryPortFromSteamServer();
			ServerGameDLL!.GameServerSteamAPIActivated();
		}
	}

	internal bool ActivateServer() {
		Common.TimestampedLog("SV.ActivateServer");
#if !SWDS
		EngineVGui.UpdateProgressBar(LevelLoadingProgress.ActivateServer);
#endif

		Common.TimestampedLog("serverGameDLL.ServerActivate");

		host_state.IntervalPerTick = ServerGameDLL!.GetTickInterval();
		if (host_state.IntervalPerTick < Constants.MINIMUM_TICK_INTERVAL || host_state.IntervalPerTick > Constants.MAXIMUM_TICK_INTERVAL) {
			Sys.Error($"GetTickInterval returned bogus tick interval ({host_state.IntervalPerTick})[{Constants.MINIMUM_TICK_INTERVAL} to {Constants.MAXIMUM_TICK_INTERVAL} is valid range]");
		}

		Msg($"SV.ActivateServer: setting tickrate to {1.0 / host_state.IntervalPerTick}\n");
		// TODO
		sv.State = ServerState.Active;

		Common.TimestampedLog("SV.CreateBaseline");

		// create a baseline for more efficient communications
		CreateBaseline();

		sv.AllowSignOnWrites = false;

		// set skybox name
		ConVar? skyname = cvar.FindVar("sv_skyname");

		if (skyname != null)
			strcpy(sv.Skyname, skyname.GetString());
		else
			strcpy(sv.Skyname, "unknown");

		Common.TimestampedLog("Send Reconnects");

		// Tell connected clients to reconnect
		sv.ReconnectClients();

		if (sv.IsMultiplayer())
			ConDMsg("%i player server started\n", sv.GetMaxClients());
		else
			ConDMsg("Game started\n");

		if (sv.IsDedicated()) {
			// purge unused models and their data hierarchy (materials, shaders, etc)
			modelloader.PurgeUnusedModels();
		}

		InitGameServerSteam();

		// TODO: Steam3Server

		Common.TimestampedLog("SV_ActivateServer(finished)");

		return true;
	}

	private void CreateBaseline() {

	}
	public bool HasPlayers() => sv.GetClientCount() > 0;

	public bool IsSimulating() {
		if (sv.IsPaused())
			return false;

# if !SWDS
		if (!sv.IsMultiplayer()) {
			if (cl.IsActive() && (Con.IsVisible() || EngineVGui.ShouldPause()))
				return false;
		}
#endif 
		return true;
	}
	ConVar? sv_noclipduringpause;
	internal void Frame(bool finalTick) {
		if (ServerGameDLL!= null && finalTick) 
			ServerGameDLL.Think(finalTick);

		if (!sv.IsActive() || !Host.ShouldRun()) {
			return;
		}

		serverGlobalVariables.FrameTime = host_state.IntervalPerTick;

		bool isSimulating = IsSimulating();
		bool sendDuringPause = sv_noclipduringpause != null? sv_noclipduringpause.GetBool() : false;

		sv.RunFrame();

		bool simulated = false;
		if (HasPlayers()) {
			bool serverCanSimulate = true; // TODO: Restoring

			if (serverCanSimulate && (isSimulating || sendDuringPause)) {
				simulated = true;
				sv.TickCount++;
				networkStringTableContainerServer.SetTick(sv.TickCount);
			}

			Think(isSimulating);
		}
		else if (sv.IsMultiplayer()) {
			Think(false);  
		}

		sv.SimulatingTicks = simulated;

		if (finalTick) {
			if (!EngineThreads.IsEngineThreaded() || sv.IsMultiplayer())
				SendClientUpdates(isSimulating, sendDuringPause);
			// else
				// DeferredServerWork = CreateFunctor(SendClientUpdates, isSimulating, sendDuringPause);

		}

		if (IsPC() && sv.IsMultiplayer()) 
			Steam3Server().RunFrame();
	}

	public bool ForcedSend;

	private void SendClientUpdates(bool isSimulating, bool sendDuringPause) {
		bool forcedSend = ForcedSend;
		ForcedSend = false;

		PreClientUpdate(isSimulating);
		sv.SendClientMessages(isSimulating || forcedSend);
		networkStringTableContainerServer.SetTick(sv.TickCount + 1);
	}

	private void PreClientUpdate(bool isSimulating) {
		ServerGameDLL?.PreClientUpdate(isSimulating);
	}

	public double TimeForceShutdown;

	private void Think(bool isSimulating) {
		sv.UpdateHibernationState();

		if (TimeForceShutdown > 0.0) {
			if (TimeForceShutdown < Platform.Time) {
				Warning("Server shutting down because sv_shutdown was requested and timeout has expired.\n");
				HostState.Shutdown();
			}
		}

		serverGlobalVariables.TickCount = sv.TickCount;
		serverGlobalVariables.CurTime = sv.GetTime();
		serverGlobalVariables.FrameTime = isSimulating ? host_state.IntervalPerTick : 0;

		isSimulating = isSimulating && (sv.IsMultiplayer() || cl.IsActive());
		serverPluginHandler.GameFrame(isSimulating);
	}

	internal void CreateNetworkStringTables() {

	}

	internal void ClearWorld() {

	}
}
