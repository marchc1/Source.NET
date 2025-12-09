using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<FlexManipulate>;
public class FlexManipulate : BaseEntity
{
	public static readonly SendTable DT_FlexManipulate = new(DT_BaseEntity, [
		SendPropFloat(FIELD.OF(nameof(ExScale)), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(EyesLocalTarget)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(FlexWeights)), 0, PropFlags.ProxyAlwaysYes | PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("FlexManipulate", DT_FlexManipulate).WithManualClassID(StaticClassIndices.CFlexManipulate);

	public float ExScale;
	public Vector3 EyesLocalTarget;
	public float FlexWeights;
}
