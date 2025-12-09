using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_AntlionGuard>;
public class NPC_AntlionGuard : AI_BaseNPC
{
	public static readonly SendTable DT_NPC_AntlionGuard = new(DT_AI_BaseNPC, [
		SendPropBool(FIELD.OF(nameof(CavernBreed))),
		SendPropBool(FIELD.OF(nameof(InCavern))),
		SendPropInt(FIELD.OF(nameof(BleedingLevel)), 2, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("NPC_AntlionGuard", DT_NPC_AntlionGuard).WithManualClassID(StaticClassIndices.CNPC_AntlionGuard);

	public bool CavernBreed;
	public bool InCavern;
	public int BleedingLevel;
}
