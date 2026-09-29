using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_EnvQuadraticBeam>;
[NetworkName("CEnvQuadraticBeam")]
public class C_EnvQuadraticBeam : C_BaseEntity
{
	public static readonly RecvTable DT_QuadraticBeam = new(DT_BaseEntity, [
		RecvPropVector(FIELD.OF(nameof(TargetPosition))),
		RecvPropVector(FIELD.OF(nameof(ControlPosition))),
		RecvPropFloat(FIELD.OF(nameof(ScrollRate))),
		RecvPropFloat(FIELD.OF(nameof(Width))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_QuadraticBeam);

	[NetworkName("m_targetPosition")]
	public Vector3 TargetPosition;
	[NetworkName("m_controlPosition")]
	public Vector3 ControlPosition;
	[NetworkName("m_scrollRate")]
	public float ScrollRate;
	[NetworkName("m_flWidth")]
	public float Width;
}
