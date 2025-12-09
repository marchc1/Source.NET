using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<PropScalable>;
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
	public static readonly new ServerClass ServerClass = new ServerClass("PropScalable", DT_PropScalable).WithManualClassID(StaticClassIndices.CPropScalable);

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
