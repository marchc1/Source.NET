using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_NPC_AntlionGuard>;
[NetworkName("CNPC_AntlionGuard")]
public class C_NPC_AntlionGuard : C_AI_BaseNPC
{
	public static readonly RecvTable DT_NPC_AntlionGuard = new(DT_AI_BaseNPC, [
		RecvPropBool(FIELD.OF(nameof(CavernBreed))),
		RecvPropBool(FIELD.OF(nameof(InCavern))),
		RecvPropInt(FIELD.OF(nameof(BleedingLevel))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_NPC_AntlionGuard);

	[NetworkName("m_bCavernBreed")]
	public bool CavernBreed;
	[NetworkName("m_bInCavern")]
	public bool InCavern;
	[NetworkName("m_iBleedingLevel")]
	public int BleedingLevel;
}
