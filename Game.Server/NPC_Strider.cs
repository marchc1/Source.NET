using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_Strider>;
public class NPC_Strider : AI_BaseNPC
{
	public static readonly SendTable DT_NPC_Strider = new(DT_AI_BaseNPC, [
		SendPropVector(FIELD.OF(nameof(HitPos)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF_ARRAYINDEX(nameof(IKTarget), 0), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF_ARRAYINDEX(nameof(IKTarget), 1), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF_ARRAYINDEX(nameof(IKTarget), 2), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF_ARRAYINDEX(nameof(IKTarget), 3), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF_ARRAYINDEX(nameof(IKTarget), 4), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF_ARRAYINDEX(nameof(IKTarget), 5), 0, PropFlags.Coord),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("NPC_Strider", DT_NPC_Strider).WithManualClassID(StaticClassIndices.CNPC_Strider);

	public Vector3 HitPos;
	public InlineArray6<Vector3> IKTarget;
}
