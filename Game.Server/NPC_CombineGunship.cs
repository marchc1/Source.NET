using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_CombineGunship>;
public class NPC_CombineGunship : BaseHelicopter
{
	public static readonly SendTable DT_NPC_CombineGunship = new(DT_BaseHelicopter, [
		SendPropVector(FIELD.OF(nameof(HitPos)), 0, PropFlags.Coord),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("NPC_CombineGunship", DT_NPC_CombineGunship).WithManualClassID(StaticClassIndices.CNPC_CombineGunship);

	public Vector3 HitPos;
}
