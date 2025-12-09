using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_FlexManipulate>;
public class C_FlexManipulate : C_BaseEntity
{
	public static readonly RecvTable DT_FlexManipulate = new(DT_BaseEntity, [
		RecvPropFloat(FIELD.OF(nameof(ExScale))),
		RecvPropVector(FIELD.OF(nameof(EyesLocalTarget))),
		RecvPropFloat(FIELD.OF(nameof(FlexWeights))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("FlexManipulate", DT_FlexManipulate).WithManualClassID(StaticClassIndices.CFlexManipulate);

	public float ExScale;
	public Vector3 EyesLocalTarget;
	public float FlexWeights;
}
