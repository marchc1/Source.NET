using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<PropScalable>;
[NetworkName("CPropScalable")]
public partial class PropScalable : BaseAnimating
{
	public static readonly SendTable DT_PropScalable = new(DT_BaseAnimating, [
		SendPropFloat(NetworkVarFields.ScaleX, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.ScaleY, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.ScaleZ, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.LerpTimeX, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.LerpTimeY, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.LerpTimeZ, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.GoalTimeX, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.GoalTimeY, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.GoalTimeZ, 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PropScalable);

	[NetworkName("m_flScaleX")]
	[NetworkVar] public partial float ScaleX { get; set; }
	[NetworkName("m_flScaleY")]
	[NetworkVar] public partial float ScaleY { get; set; }
	[NetworkName("m_flScaleZ")]
	[NetworkVar] public partial float ScaleZ { get; set; }
	[NetworkName("m_flLerpTimeX")]
	[NetworkVar] public partial float LerpTimeX { get; set; }
	[NetworkName("m_flLerpTimeY")]
	[NetworkVar] public partial float LerpTimeY { get; set; }
	[NetworkName("m_flLerpTimeZ")]
	[NetworkVar] public partial float LerpTimeZ { get; set; }
	[NetworkName("m_flGoalTimeX")]
	[NetworkVar] public partial float GoalTimeX { get; set; }
	[NetworkName("m_flGoalTimeY")]
	[NetworkVar] public partial float GoalTimeY { get; set; }
	[NetworkName("m_flGoalTimeZ")]
	[NetworkVar] public partial float GoalTimeZ { get; set; }
}
