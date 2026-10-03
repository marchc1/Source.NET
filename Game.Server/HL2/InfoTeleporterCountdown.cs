using Game.Shared;

using Source;
using Source.Common;

namespace Game.Server.HL2;
using FIELD = Source.FIELD<InfoTeleporterCountdown>;
[LinkEntityToClass("info_teleporter_countdown")]
[NetworkName("CInfoTeleporterCountdown")]
public partial class InfoTeleporterCountdown : BaseEntity
{
	public static readonly SendTable DT_InfoTeleporterCountdown = new(DT_BaseEntity, [
		SendPropBool(FIELD.OF(nameof(CountdownStarted))),
		SendPropBool(FIELD.OF(nameof(Disabled))),
		SendPropFloat(FIELD.OF(nameof(StartTime)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(TimeRemaining)), 0, PropFlags.NoScale)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_InfoTeleporterCountdown);

	[NetworkName("m_bCountdownStarted")]
	public bool CountdownStarted;
	[NetworkName("m_bDisabled")]
	public bool Disabled;
	[NetworkName("m_flStartTime")]
	public TimeUnit_t StartTime;
	[NetworkName("m_flTimeRemaining")]
	public TimeUnit_t TimeRemaining;
}
