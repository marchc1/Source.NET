using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_PropScalable>;
[NetworkName("CPropScalable")]
public class C_PropScalable : C_BaseAnimating
{
	public static readonly RecvTable DT_PropScalable = new(DT_BaseAnimating, [
		// todo: RecvProxy_ScaleX
		RecvPropFloat(FIELD.OF(nameof(ScaleX))),
		// todo: RecvProxy_ScaleY
		RecvPropFloat(FIELD.OF(nameof(ScaleY))),
		// todo: RecvProxy_ScaleZ
		RecvPropFloat(FIELD.OF(nameof(ScaleZ))),
		RecvPropFloat(FIELD.OF(nameof(LerpTimeX))),
		RecvPropFloat(FIELD.OF(nameof(LerpTimeY))),
		RecvPropFloat(FIELD.OF(nameof(LerpTimeZ))),
		RecvPropFloat(FIELD.OF(nameof(GoalTimeX))),
		RecvPropFloat(FIELD.OF(nameof(GoalTimeY))),
		RecvPropFloat(FIELD.OF(nameof(GoalTimeZ))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_PropScalable);

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
