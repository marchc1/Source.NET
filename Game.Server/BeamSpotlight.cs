using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<BeamSpotlight>;
[NetworkName("CBeamSpotlight")]
public partial class BeamSpotlight : BaseEntity
{
	public static readonly SendTable DT_BeamSpotlight = new(DT_BaseEntity, [
		SendPropInt(NetworkVarFields.HaloIndex, 16, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.SpotlightOn, 1, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.HasDynamicLight, 1, PropFlags.Unsigned),
		SendPropFloat(NetworkVarFields.SpotlightMaxLength, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.SpotlightGoalWidth, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.HDRColorScale, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.RotationSpeed, 0, PropFlags.NoScale),
		SendPropInt(NetworkVarFields.RotationAxis, 2, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_BeamSpotlight);

	[NetworkName("m_nHaloIndex")]
	[NetworkVar] public partial int HaloIndex { get; set; }
	[NetworkName("m_bSpotlightOn")]
	[NetworkVar] public partial int SpotlightOn { get; set; }
	[NetworkName("m_bHasDynamicLight")]
	[NetworkVar] public partial int HasDynamicLight { get; set; }
	[NetworkName("m_flSpotlightMaxLength")]
	[NetworkVar] public partial float SpotlightMaxLength { get; set; }
	[NetworkName("m_flSpotlightGoalWidth")]
	[NetworkVar] public partial float SpotlightGoalWidth { get; set; }
	[NetworkName("m_flHDRColorScale")]
	[NetworkVar] public partial float HDRColorScale { get; set; }
	[NetworkName("m_flRotationSpeed")]
	[NetworkVar] public partial float RotationSpeed { get; set; }
	[NetworkName("m_nRotationAxis")]
	[NetworkVar] public partial int RotationAxis { get; set; }
}
