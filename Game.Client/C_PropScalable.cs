using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_PropScalable>;
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
	public static readonly new ClientClass ClientClass = new ClientClass("PropScalable", DT_PropScalable).WithManualClassID(StaticClassIndices.CPropScalable);

	public float ScaleX;
	public float ScaleY;
	public float ScaleZ;
	public float LerpTimeX;
	public float LerpTimeY;
	public float LerpTimeZ;
	public float GoalTimeX;
	public float GoalTimeY;
	public float GoalTimeZ;
}
