using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_Portal_FloorTurret>;
[NetworkName("CNPC_Portal_FloorTurret")]
public partial class NPC_Portal_FloorTurret : AI_BaseNPC
{
	public static readonly SendTable DT_NPC_Portal_FloorTurret = new(DT_AI_BaseNPC, [
		SendPropBool(NetworkVarFields.OutOfAmmo),
		SendPropBool(NetworkVarFields.LaserOn),
		SendPropInt(NetworkVarFields.LaserHaloSprite, 16, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_NPC_Portal_FloorTurret);

	[NetworkName("m_bOutOfAmmo")]
	[NetworkVar] public partial bool OutOfAmmo { get; set; }
	[NetworkName("m_bLaserOn")]
	[NetworkVar] public partial bool LaserOn { get; set; }
	[NetworkName("m_sLaserHaloSprite")]
	[NetworkVar] public partial int LaserHaloSprite { get; set; }
}
