using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
using Source.Common.Mathematics;
namespace Game.Client;
using FIELD = FIELD<C_TEBreakModel>;
public class C_TEBreakModel : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEBreakModel = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropFloat(FIELD.OF_VECTORELEM(nameof(Rotation), 0)),
		RecvPropFloat(FIELD.OF_VECTORELEM(nameof(Rotation), 1)),
		RecvPropFloat(FIELD.OF_VECTORELEM(nameof(Rotation), 2)),
		RecvPropVector(FIELD.OF(nameof(Size))),
		RecvPropVector(FIELD.OF(nameof(Velocity))),
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropInt(FIELD.OF(nameof(Randomization))),
		RecvPropInt(FIELD.OF(nameof(Count))),
		RecvPropFloat(FIELD.OF(nameof(Time))),
		RecvPropInt(FIELD.OF(nameof(Flags))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEBreakModel", DT_TEBreakModel).WithManualClassID(StaticClassIndices.CTEBreakModel);

	public Vector3 Origin;
	public QAngle Rotation;
	public Vector3 Size;
	public Vector3 Velocity;
	public int ModelIndex;
	public int Randomization;
	public int Count;
	public float Time;
	public int Flags;
}
