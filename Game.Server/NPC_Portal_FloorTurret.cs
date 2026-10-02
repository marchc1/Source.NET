using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_Portal_FloorTurret>;
[LinkEntityToClass("npc_portal_turret_floor")]
[NetworkName("CNPC_Portal_FloorTurret")]
public class NPC_Portal_FloorTurret : AI_BaseNPC
{
	public static readonly SendTable DT_NPC_Portal_FloorTurret = new(DT_AI_BaseNPC, [
		SendPropBool(FIELD.OF(nameof(OutOfAmmo))),
		SendPropBool(FIELD.OF(nameof(LaserOn))),
		SendPropInt(FIELD.OF(nameof(LaserHaloSprite)), 16, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_NPC_Portal_FloorTurret);

	[NetworkName("m_bOutOfAmmo")]
	public bool OutOfAmmo;
	[NetworkName("m_bLaserOn")]
	public bool LaserOn;
	[NetworkName("m_sLaserHaloSprite")]
	public int LaserHaloSprite;
}
