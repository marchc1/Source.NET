using Source.Common;
using Source;

namespace Game.Server;

using FIELD = FIELD<EnvScreenOverlay>;

[NetworkName("CEnvScreenOverlay")]
public partial class EnvScreenOverlay : BaseEntity
{
	public const int MAX_SCREEN_OVERLAYS = 10;

	[NetworkName("m_iszOverlayNames")]
	[NetworkVar] public partial NetworkArray<InlineArray10<string?>, string?> OverlayNames { get; }
	[NetworkName("m_flOverlayTimes")]
	[NetworkVar] public partial NetworkArray<InlineArray10<float>, float> OverlayTimes { get; }
	[NetworkName("m_flStartTime")]
	[NetworkVar] public partial float StartTime { get; set; }
	[NetworkName("m_iDesiredOverlay")]
	[NetworkVar] public partial int DesiredOverlay { get; set; }
	[NetworkName("m_bIsActive")]
	[NetworkVar] public partial bool IsActive { get; set; }

	public static readonly SendTable DT_EnvScreenOverlay = new(DT_BaseEntity, [
		SendPropStringT(FIELD.OF_SENDINFO_ARRAY(NetworkVarFields.OverlayNames)),
		SendPropArray(NetworkVarFields.OverlayNames),
		SendPropFloat(FIELD.OF_SENDINFO_ARRAY(NetworkVarFields.OverlayTimes), 11, PropFlags.RoundDown, -1.0f, 63.0f),
		SendPropArray(NetworkVarFields.OverlayTimes),
		SendPropFloat(NetworkVarFields.StartTime, 0, PropFlags.NoScale),
		SendPropInt(NetworkVarFields.DesiredOverlay, 5),
		SendPropBool(NetworkVarFields.IsActive),
	]);
	public static new readonly ServerClass ServerClass = new ServerClass(DT_EnvScreenOverlay);
}
