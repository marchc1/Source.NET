
using Microsoft.Extensions.DependencyInjection;

using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Server;
using Source.Engine.Server;

namespace Source.Engine;

/// <summary>
/// Various serverside methods. In Source, these would mostly be represented by
/// SV_MethodName's in the static global namespace
/// </summary>
public class SV(IServiceProvider services, Cbuf Cbuf, GameServer sv, ED ED, Host Host, CommonHostState host_state)
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
		if(ServerGameDLL == null) {
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
		while(classes != null) {
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
		throw new NotImplementedException();
	}
}
