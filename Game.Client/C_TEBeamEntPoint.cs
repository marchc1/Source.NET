using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEBeamEntPoint>;
public class C_TEBeamEntPoint : C_BaseBeam
{
	public static readonly RecvTable DT_TEBeamEntPoint = new(DT_BaseBeam, [
		RecvPropInt(FIELD.OF(nameof(StartEntity))),
		RecvPropInt(FIELD.OF(nameof(EndEntity))),
		RecvPropVector(FIELD.OF(nameof(StartPoint))),
		RecvPropVector(FIELD.OF(nameof(EndPoint))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEBeamEntPoint", DT_TEBeamEntPoint).WithManualClassID(StaticClassIndices.CTEBeamEntPoint);

	public int StartEntity;
	public int EndEntity;
	public Vector3 StartPoint;
	public Vector3 EndPoint;
}
