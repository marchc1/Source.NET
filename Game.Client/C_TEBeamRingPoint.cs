using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEBeamRingPoint>;
public class C_TEBeamRingPoint : C_BaseBeam
{
	public static readonly RecvTable DT_TEBeamRingPoint = new(DT_BaseBeam, [
		RecvPropVector(FIELD.OF(nameof(Center))),
		RecvPropFloat(FIELD.OF(nameof(LStartRadius))),
		RecvPropFloat(FIELD.OF(nameof(LEndRadius))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEBeamRingPoint", DT_TEBeamRingPoint).WithManualClassID(StaticClassIndices.CTEBeamRingPoint);

	public Vector3 Center;
	public float LStartRadius;
	public float LEndRadius;
}
