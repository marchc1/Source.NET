using Source.Common.Commands;
using Source.Common.Engine;

namespace Source.Engine;

public class ED
{

	static readonly ConVar sv_useexplicitdelete = new("1", FCvar.DevelopmentOnly, "Explicitly delete dormant client entities caused by AllowImmediateReuse().");
	static readonly ConVar sv_lowEdicthreshold = new("8", FCvar.None, "When only this many edicts are free, take the action specified by sv_lowedict_action.", 0, Constants.MAX_EDICTS);
	static readonly ConVar sv_lowedict_action = new("0", FCvar.None, "0 - no action, 1 - warn to log file, 2 - attempt to restart the game, if applicable, 3 - restart the map, 4 - go to the next map in the map cycle, 5 - spew all edicts.", 0, 5);
	static MaxEdictsBitVec FreeEdicts;

	public static Edict? Alloc(int forceEdictIndex) {
		if (forceEdictIndex >= 0) {
			if (forceEdictIndex >= sv.NumEdicts) {
				Warning("ED_Alloc( %d ) - invalid edict index specified.", forceEdictIndex);
				return null;
			}

			Edict e = sv.Edicts![forceEdictIndex];
			if (e.IsFree()) {
				Assert(forceEdictIndex == e.EdictIndex);
				--sv.FreeEdicts;
				Assert(FreeEdicts.IsBitSet(forceEdictIndex));
				FreeEdicts.Clear(forceEdictIndex);
				ClearEdict(e);
				return e;
			}
			else
				return null;
		}

		// Check the free list first.
		int bit = -1;
		Edict edict;
		for (; ; )
		{
			bit = FreeEdicts.FindNextSetBit(bit + 1) - 1; // FIXME: This is returning 8192, so we must -1 otherwise we are 1 over the limit?
			if (bit < 0)
				break;

			edict = sv.Edicts![bit];

			// If this assert goes off, someone most likely called pedict.ClearFree() and not ED_ClearFreeFlag()?
			Assert(edict.IsFree());
			Assert(bit == edict.EdictIndex);
			if ((edict.FreeTime < 2) || (sv.GetTime() - edict.FreeTime >= 1.0 /*EDICT_FREETIME*/)) {
				// If we have no freetime, we've had AllowImmediateReuse() called. We need
				// to explicitly delete this old entity.
				if (edict.FreeTime == 0 && sv_useexplicitdelete.GetBool()) {
					//Warning("ADDING SLOT to snapshot: %d\n", i );
					// framesnapshotmanager.AddExplicitDelete(bit); TODO
				}

				--sv.FreeEdicts;
				FreeEdicts.Clear(edict.EdictIndex);
				ClearEdict(edict);
				return edict;
			}
		}

		// Allocate a new edict.
		if (sv.NumEdicts >= sv.MaxEdicts) {
			AssertMsg(false, "Can't allocate edict");

			//todo  SpewEdicts(); // Log the entities we have before we die

			if (sv.MaxEdicts == 0)
				Sys.Error("ED_Alloc: No edicts yet");
			Sys.Error("ED_Alloc: no free edicts");
		}

		// Do this before clearing since clear now needs to call back into the edict to deduce the index so can get the changeinfo data in the parallel structure
		edict = sv.Edicts![sv.NumEdicts++];

		// We should not be in the free list...
		Assert(!FreeEdicts.IsBitSet(edict.EdictIndex));
		ClearEdict(edict);

		if (sv_lowedict_action.GetInt() > 0 && sv.NumEdicts >= sv.MaxEdicts - sv_lowEdicthreshold.GetInt()) {
			int edictsRemaining = sv.MaxEdicts - sv.NumEdicts;
			// Log.Printf("Warning: free edicts below threshold. %i free edict%s remaining.\n", edictsRemaining, edictsRemaining == 1 ? "" : "s"); todo

			switch (sv_lowedict_action.GetInt()) {
				case 2:
					// restart the game
					{
						ConVarRef mp_restartgame_immediate = new("mp_restartgame_immediate");
						if (mp_restartgame_immediate.IsValid()) {
							mp_restartgame_immediate.SetValue(1);
						}
						else {
							ConVarRef mp_restartgame = new("mp_restartgame");
							if (mp_restartgame.IsValid())
								mp_restartgame.SetValue(1);
						}
					}
					break;
				case 3:
					// restart the map
					engine.ChangeLevel(sv.GetMapName(), null);
					break;
				case 4:
					// go to the next map
					engine.ServerCommand("changelevel_next\n");
					break;
				case 5:
					// spew all edicts
					// SpewEdicts(); todo
					break;
			}
		}

		return edict;
	}


	public static void ClearEdict(Edict e) {
		e.ClearFree();
		e.ClearStateChanged();
		e.SetChangeInfoSerialNumber(0);

		// serverGameEnts.FreeContainingEntity(e);
		e.InitializeEntityDLLFields();
		e.NetworkSerialNumber = -1;  // must be filled by game.dll
	}

	public void ClearFreeEdictList() {
		FreeEdicts.ClearAll();
	}

	public void ClearFreeFlag(Edict e) {
		e.ClearFree();
		FreeEdicts.Clear(e.EdictIndex);
	}
}
