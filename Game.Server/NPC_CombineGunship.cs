using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_CombineGunship>;
[LinkEntityToClass("npc_combinegunship")]
[NetworkName("CNPC_CombineGunship")]
public class NPC_CombineGunship : BaseHelicopter
{
	public static readonly SendTable DT_CombineGunship = new(DT_BaseHelicopter, [
		SendPropVector(FIELD.OF(nameof(HitPos)), 0, PropFlags.Coord),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_CombineGunship);

	[NetworkName("m_vecHitPos")]
	public Vector3 HitPos;
}
