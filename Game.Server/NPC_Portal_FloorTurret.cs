using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_Portal_FloorTurret>;
public class NPC_Portal_FloorTurret : AI_BaseNPC
{
	public static readonly SendTable DT_NPC_Portal_FloorTurret = new(DT_AI_BaseNPC, [
		SendPropBool(FIELD.OF(nameof(OutOfAmmo))),
		SendPropBool(FIELD.OF(nameof(LaserOn))),
		SendPropInt(FIELD.OF(nameof(LaserHaloSprite)), 32, 0),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("NPC_Portal_FloorTurret", DT_NPC_Portal_FloorTurret).WithManualClassID(StaticClassIndices.CNPC_Portal_FloorTurret);

	public bool OutOfAmmo;
	public bool LaserOn;
	public int LaserHaloSprite;
}
