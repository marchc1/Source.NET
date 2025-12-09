using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TESmoke>;
public class C_TESmoke : C_BaseTempEntity
{
	public static readonly RecvTable DT_TESmoke = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropFloat(FIELD.OF(nameof(Scale))),
		RecvPropInt(FIELD.OF(nameof(FrameRate))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TESmoke", DT_TESmoke).WithManualClassID(StaticClassIndices.CTESmoke);

	public Vector3 Origin;
	public int ModelIndex;
	public float Scale;
	public int FrameRate;
}
