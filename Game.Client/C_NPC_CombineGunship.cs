using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_NPC_CombineGunship>;
public class C_NPC_CombineGunship : C_BaseHelicopter
{
	public static readonly RecvTable DT_NPC_CombineGunship = new(DT_BaseHelicopter, [
		RecvPropVector(FIELD.OF(nameof(HitPos))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("NPC_CombineGunship", DT_NPC_CombineGunship).WithManualClassID(StaticClassIndices.CNPC_CombineGunship);

	public Vector3 HitPos;
}
