using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_BeamSpotlight>;
[LinkEntityToClass("beam_spotlight")]
[NetworkName("CBeamSpotlight")]
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
	public static readonly new ClientClass ClientClass = new ClientClass(DT_BeamSpotlight);

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
