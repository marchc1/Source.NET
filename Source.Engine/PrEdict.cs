using Source.Common.Engine;
using Source.Common.Server;
using Source.Engine.Server;

namespace Source.Engine;

public class ED {
	public static void ClearEdict(Edict e) {
		e.ClearFree();
		e.ClearStateChanged();
		e.SetChangeInfoSerialNumber(0);

		// serverGameEnts.FreeContainingEntity(e);
		e.InitializeEntityDLLFields();
		e.NetworkSerialNumber = -1;  // must be filled by game.dll
	}

	MaxEdictsBitVec FreeEdicts;

	public void ClearFreeEdictList() {
		FreeEdicts.ClearAll();
	}

	public void ClearFreeFlag(Edict e) {
		e.ClearFree();
		FreeEdicts.Clear(e.EdictIndex);
	}
}
