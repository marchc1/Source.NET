using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEBubbles>;
public class C_TEBubbles : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEBubbles = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Mins))),
		RecvPropVector(FIELD.OF(nameof(Maxs))),
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropFloat(FIELD.OF(nameof(Height))),
		RecvPropInt(FIELD.OF(nameof(Count))),
		RecvPropFloat(FIELD.OF(nameof(Speed))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEBubbles", DT_TEBubbles).WithManualClassID(StaticClassIndices.CTEBubbles);

	public Vector3 Mins;
	public Vector3 Maxs;
	public int ModelIndex;
	public float Height;
	public int Count;
	public float Speed;
}
