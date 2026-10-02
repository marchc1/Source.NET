using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<BeamSpotlight>;
[LinkEntityToClass("beam_spotlight")]
[NetworkName("CBeamSpotlight")]
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
	public static readonly new ServerClass ServerClass = new ServerClass(DT_BeamSpotlight);

	[NetworkName("m_nHaloIndex")]
	public int HaloIndex;
	[NetworkName("m_bSpotlightOn")]
	public int SpotlightOn;
	[NetworkName("m_bHasDynamicLight")]
	public int HasDynamicLight;
	[NetworkName("m_flSpotlightMaxLength")]
	public float SpotlightMaxLength;
	[NetworkName("m_flSpotlightGoalWidth")]
	public float SpotlightGoalWidth;
	[NetworkName("m_flHDRColorScale")]
	public float HDRColorScale;
	[NetworkName("m_flRotationSpeed")]
	public float RotationSpeed;
	[NetworkName("m_nRotationAxis")]
	public int RotationAxis;
}
