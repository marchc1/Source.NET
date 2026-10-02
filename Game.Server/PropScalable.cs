using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<PropScalable>;
[LinkEntityToClass("prop_coreball")]
[LinkEntityToClass("prop_scalable")]
[NetworkName("CPropScalable")]
public class PropScalable : BaseAnimating
{
	public static readonly SendTable DT_PropScalable = new(DT_BaseAnimating, [
		SendPropFloat(FIELD.OF(nameof(ScaleX)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(ScaleY)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(ScaleZ)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(LerpTimeX)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(LerpTimeY)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(LerpTimeZ)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(GoalTimeX)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(GoalTimeY)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(GoalTimeZ)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PropScalable);

	[NetworkName("m_flScaleX")]
	public float ScaleX;
	[NetworkName("m_flScaleY")]
	public float ScaleY;
	[NetworkName("m_flScaleZ")]
	public float ScaleZ;
	[NetworkName("m_flLerpTimeX")]
	public float LerpTimeX;
	[NetworkName("m_flLerpTimeY")]
	public float LerpTimeY;
	[NetworkName("m_flLerpTimeZ")]
	public float LerpTimeZ;
	[NetworkName("m_flGoalTimeX")]
	public float GoalTimeX;
	[NetworkName("m_flGoalTimeY")]
	public float GoalTimeY;
	[NetworkName("m_flGoalTimeZ")]
	public float GoalTimeZ;
}
