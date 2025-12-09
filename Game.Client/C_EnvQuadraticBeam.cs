using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_EnvQuadraticBeam>;
public class C_EnvQuadraticBeam : C_BaseEntity
{
	public static readonly RecvTable DT_EnvQuadraticBeam = new(DT_BaseEntity, [
		RecvPropVector(FIELD.OF(nameof(TargetPosition))),
		RecvPropVector(FIELD.OF(nameof(ControlPosition))),
		RecvPropFloat(FIELD.OF(nameof(ScrollRate))),
		RecvPropFloat(FIELD.OF(nameof(Width))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("EnvQuadraticBeam", DT_EnvQuadraticBeam).WithManualClassID(StaticClassIndices.CEnvQuadraticBeam);

	public Vector3 TargetPosition;
	public Vector3 ControlPosition;
	public float ScrollRate;
	public float Width;
}
