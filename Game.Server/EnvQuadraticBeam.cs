using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<EnvQuadraticBeam>;
[NetworkName("CEnvQuadraticBeam")]
public partial class EnvQuadraticBeam : BaseEntity
{
	public static readonly SendTable DT_QuadraticBeam = new(DT_BaseEntity, [
		SendPropVector(NetworkVarFields.TargetPosition, 0, PropFlags.Coord),
		SendPropVector(NetworkVarFields.ControlPosition, 0, PropFlags.Coord),
		SendPropFloat(NetworkVarFields.ScrollRate, 8, 0, -4, 4),
		SendPropFloat(NetworkVarFields.Width, 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_QuadraticBeam);

	[NetworkName("m_targetPosition")]
	[NetworkVar] public partial Vector3 TargetPosition { get; set; }
	[NetworkName("m_controlPosition")]
	[NetworkVar] public partial Vector3 ControlPosition { get; set; }
	[NetworkName("m_scrollRate")]
	[NetworkVar] public partial float ScrollRate { get; set; }
	[NetworkName("m_flWidth")]
	[NetworkVar] public partial float Width { get; set; }
}
