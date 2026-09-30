using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_AntlionGuard>;
[NetworkName("CNPC_AntlionGuard")]
public partial class NPC_AntlionGuard : AI_BaseNPC
{
	public static readonly SendTable DT_NPC_AntlionGuard = new(DT_AI_BaseNPC, [
		SendPropBool(NetworkVarFields.CavernBreed),
		SendPropBool(NetworkVarFields.InCavern),
		SendPropInt(NetworkVarFields.BleedingLevel, 2, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_NPC_AntlionGuard);

	[NetworkName("m_bCavernBreed")]
	[NetworkVar] public partial bool CavernBreed { get; set; }
	[NetworkName("m_bInCavern")]
	[NetworkVar] public partial bool InCavern { get; set; }
	[NetworkName("m_iBleedingLevel")]
	[NetworkVar] public partial int BleedingLevel { get; set; }
}
