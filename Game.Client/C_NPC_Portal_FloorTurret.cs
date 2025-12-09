using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_NPC_Portal_FloorTurret>;
public class C_NPC_Portal_FloorTurret : C_AI_BaseNPC
{
	public static readonly RecvTable DT_NPC_Portal_FloorTurret = new(DT_AI_BaseNPC, [
		RecvPropBool(FIELD.OF(nameof(OutOfAmmo))),
		RecvPropBool(FIELD.OF(nameof(LaserOn))),
		RecvPropInt(FIELD.OF(nameof(LaserHaloSprite))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("NPC_Portal_FloorTurret", DT_NPC_Portal_FloorTurret).WithManualClassID(StaticClassIndices.CNPC_Portal_FloorTurret);

	public bool OutOfAmmo;
	public bool LaserOn;
	public int LaserHaloSprite;
}
