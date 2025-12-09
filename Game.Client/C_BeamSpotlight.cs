using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_BeamSpotlight>;
public class C_BeamSpotlight : C_BaseEntity
{
	public static readonly RecvTable DT_BeamSpotlight = new(DT_BaseEntity, [
		RecvPropInt(FIELD.OF(nameof(HaloIndex))),
		RecvPropInt(FIELD.OF(nameof(SpotlightOn))),
		RecvPropInt(FIELD.OF(nameof(HasDynamicLight))),
		RecvPropFloat(FIELD.OF(nameof(SpotlightMaxLength))),
		RecvPropFloat(FIELD.OF(nameof(SpotlightGoalWidth))),
		RecvPropFloat(FIELD.OF(nameof(HDRColorScale))),
		RecvPropFloat(FIELD.OF(nameof(RotationSpeed))),
		RecvPropInt(FIELD.OF(nameof(RotationAxis))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("BeamSpotlight", DT_BeamSpotlight).WithManualClassID(StaticClassIndices.CBeamSpotlight);

	public int HaloIndex;
	public int SpotlightOn;
	public int HasDynamicLight;
	public float SpotlightMaxLength;
	public float SpotlightGoalWidth;
	public float HDRColorScale;
	public float RotationSpeed;
	public int RotationAxis;
}
