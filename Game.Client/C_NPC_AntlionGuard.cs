using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_NPC_AntlionGuard>;
public class C_NPC_AntlionGuard : C_AI_BaseNPC
{
	public static readonly RecvTable DT_NPC_AntlionGuard = new(DT_AI_BaseNPC, [
		RecvPropBool(FIELD.OF(nameof(CavernBreed))),
		RecvPropBool(FIELD.OF(nameof(InCavern))),
		RecvPropInt(FIELD.OF(nameof(BleedingLevel))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("NPC_AntlionGuard", DT_NPC_AntlionGuard).WithManualClassID(StaticClassIndices.CNPC_AntlionGuard);

	public bool CavernBreed;
	public bool InCavern;
	public int BleedingLevel;
}
