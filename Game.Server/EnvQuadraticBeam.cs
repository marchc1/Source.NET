using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<EnvQuadraticBeam>;
[LinkEntityToClass("env_quadraticbeam")]
[NetworkName("CEnvQuadraticBeam")]
public class EnvQuadraticBeam : BaseEntity
{
	public static readonly SendTable DT_QuadraticBeam = new(DT_BaseEntity, [
		SendPropVector(FIELD.OF(nameof(TargetPosition)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(ControlPosition)), 0, PropFlags.Coord),
		SendPropFloat(FIELD.OF(nameof(ScrollRate)), 8, 0, -4, 4),
		SendPropFloat(FIELD.OF(nameof(Width)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_QuadraticBeam);

	[NetworkName("m_targetPosition")]
	public Vector3 TargetPosition;
	[NetworkName("m_controlPosition")]
	public Vector3 ControlPosition;
	[NetworkName("m_scrollRate")]
	public float ScrollRate;
	[NetworkName("m_flWidth")]
	public float Width;
}
