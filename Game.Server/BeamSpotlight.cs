using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<BeamSpotlight>;
public class BeamSpotlight : BaseEntity
{
	public static readonly SendTable DT_BeamSpotlight = new(DT_BaseEntity, [
		SendPropInt(FIELD.OF(nameof(HaloIndex)), 16, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(SpotlightOn)), 1, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(HasDynamicLight)), 1, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(SpotlightMaxLength)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(SpotlightGoalWidth)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(HDRColorScale)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(RotationSpeed)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(RotationAxis)), 2, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("BeamSpotlight", DT_BeamSpotlight).WithManualClassID(StaticClassIndices.CBeamSpotlight);

	public int HaloIndex;
	public int SpotlightOn;
	public int HasDynamicLight;
	public float SpotlightMaxLength;
	public float SpotlightGoalWidth;
	public float HDRColorScale;
	public float RotationSpeed;
	public int RotationAxis;
}
