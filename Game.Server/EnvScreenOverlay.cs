using Game.Shared;
using Source.Common;
using Source;

namespace Game.Server;

using FIELD = FIELD<EnvScreenOverlay>;

[LinkEntityToClass("env_screenoverlay")]
[NetworkName("CEnvScreenOverlay")]
public class EnvScreenOverlay : BaseEntity
{
	public const int MAX_SCREEN_OVERLAYS = 10;

	[NetworkName("m_iszOverlayNames")]
	public InlineArray10<InlineArray512<char>> OverlayNames;
	[NetworkName("m_flOverlayTimes")]
	public InlineArray10<float> OverlayTimes;
	[NetworkName("m_flStartTime")]
	public float StartTime;
	[NetworkName("m_iDesiredOverlay")]
	public int DesiredOverlay;
	[NetworkName("m_bIsActive")]
	public bool IsActive;

	public static readonly SendTable DT_EnvScreenOverlay = new(DT_BaseEntity, [
		SendPropString(FIELD.OF_SENDINFO_ARRAY(nameof(OverlayNames))),
		SendPropArray(FIELD.OF_ARRAY(nameof(OverlayNames))),
		SendPropFloat(FIELD.OF_SENDINFO_ARRAY(nameof(OverlayTimes)), 11, PropFlags.RoundDown, -1.0f, 63.0f),
		SendPropArray(FIELD.OF_ARRAY(nameof(OverlayTimes))),
		SendPropFloat(FIELD.OF(nameof(StartTime)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(DesiredOverlay)), 5),
		SendPropBool(FIELD.OF(nameof(IsActive))),
	]);
	public static new readonly ServerClass ServerClass = new ServerClass(DT_EnvScreenOverlay);
}
