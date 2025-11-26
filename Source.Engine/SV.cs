
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
public class SV(IServiceProvider services, Cbuf Cbuf, GameServer sv, ED ED, Host Host, CommonHostState host_state, IEngineVGuiInternal EngineVGui, ICvar cvar, IModelLoader modelloader)
{
	public IServerGameDLL? ServerGameDLL;
	public IServerGameEnts? ServerGameEnts;
	public IServerGameClients? ServerGameClients;
	public ConVar sv_cheats = new(nameof(sv_cheats), "0", FCvar.Notify | FCvar.Replicated, "Allow cheats on server", callback: SV_CheatsChanged);

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
			// TODO
			throw new NotImplementedException();
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
}
