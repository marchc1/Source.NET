using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEBubbleTrail>;
public class C_TEBubbleTrail : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEBubbleTrail = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Mins))),
		RecvPropVector(FIELD.OF(nameof(Maxs))),
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropFloat(FIELD.OF(nameof(LWaterZ))),
		RecvPropInt(FIELD.OF(nameof(Count))),
		RecvPropFloat(FIELD.OF(nameof(Speed))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEBubbleTrail", DT_TEBubbleTrail).WithManualClassID(StaticClassIndices.CTEBubbleTrail);

	public Vector3 Mins;
	public Vector3 Maxs;
	public int ModelIndex;
	public float LWaterZ;
	public int Count;
	public float Speed;
}
