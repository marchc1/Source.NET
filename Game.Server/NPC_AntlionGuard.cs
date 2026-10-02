using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_AntlionGuard>;
[LinkEntityToClass("npc_antlionguard")]
[NetworkName("CNPC_AntlionGuard")]
public class NPC_AntlionGuard : AI_BaseNPC
{
	public static readonly SendTable DT_NPC_AntlionGuard = new(DT_AI_BaseNPC, [
		SendPropBool(FIELD.OF(nameof(CavernBreed))),
		SendPropBool(FIELD.OF(nameof(InCavern))),
		SendPropInt(FIELD.OF(nameof(BleedingLevel)), 2, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_NPC_AntlionGuard);

	[NetworkName("m_bCavernBreed")]
	public bool CavernBreed;
	[NetworkName("m_bInCavern")]
	public bool InCavern;
	[NetworkName("m_iBleedingLevel")]
	public int BleedingLevel;
}
