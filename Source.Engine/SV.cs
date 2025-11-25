
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
public class SV(IServiceProvider services, Cbuf Cbuf, GameServer sv, ED ED)
{
	public ConVar sv_cheats = new(nameof(sv_cheats), "0", FCvar.Notify | FCvar.Replicated, "Allow cheats on server", callback: SV_CheatsChanged);

	private static void SV_CheatsChanged(IConVar var, in ConVarChangeContext ctx) {

	}

	internal void DumpStringTables() {

	}

	internal void InitGameDLL() {
		Cbuf.Execute();
		IServerGameDLL serverGameDLL = services.GetRequiredService<IServerGameDLL>();
		InitSendTables(serverGameDLL.GetAllServerClasses());
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
}
