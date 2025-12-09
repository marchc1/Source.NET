using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<EnvQuadraticBeam>;
public class EnvQuadraticBeam : BaseEntity
{
	public static readonly SendTable DT_EnvQuadraticBeam = new(DT_BaseEntity, [
		SendPropVector(FIELD.OF(nameof(TargetPosition)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(ControlPosition)), 0, PropFlags.Coord),
		SendPropFloat(FIELD.OF(nameof(ScrollRate)), 8, 0),
		SendPropFloat(FIELD.OF(nameof(Width)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("EnvQuadraticBeam", DT_EnvQuadraticBeam).WithManualClassID(StaticClassIndices.CEnvQuadraticBeam);

	public Vector3 TargetPosition;
	public Vector3 ControlPosition;
	public float ScrollRate;
	public float Width;
}
