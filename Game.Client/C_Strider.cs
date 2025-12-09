using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_Strider>;
public class C_Strider : C_AI_BaseNPC
{
	public static readonly RecvTable DT_NPC_Strider = new(DT_AI_BaseNPC, [
		RecvPropVector(FIELD.OF(nameof(HitPos))),
		RecvPropVector(FIELD.OF_ARRAYINDEX(nameof(IKTarget), 0)),
		RecvPropVector(FIELD.OF_ARRAYINDEX(nameof(IKTarget), 1)),
		RecvPropVector(FIELD.OF_ARRAYINDEX(nameof(IKTarget), 2)),
		RecvPropVector(FIELD.OF_ARRAYINDEX(nameof(IKTarget), 3)),
		RecvPropVector(FIELD.OF_ARRAYINDEX(nameof(IKTarget), 4)),
		RecvPropVector(FIELD.OF_ARRAYINDEX(nameof(IKTarget), 5)),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("NPC_Strider", DT_NPC_Strider).WithManualClassID(StaticClassIndices.CNPC_Strider);

	public Vector3 HitPos;
	public InlineArray6<Vector3> IKTarget;
}
