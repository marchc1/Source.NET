using Game.Shared;

using Source;
using Source.Common;

namespace Game.Client.HL2;
using FIELD = Source.FIELD<C_InfoTeleporterCountdown>;

[NetworkName("CInfoTeleporterCountdown")]
public partial class C_InfoTeleporterCountdown : C_BaseEntity
{
	public static readonly RecvTable DT_InfoTeleporterCountdown = new(DT_BaseEntity, [
		RecvPropBool(FIELD.OF(nameof(CountdownStarted))),
		RecvPropBool(FIELD.OF(nameof(Disabled))),
		RecvPropFloat(FIELD.OF(nameof(StartTime))),
		RecvPropFloat(FIELD.OF(nameof(TimeRemaining)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_InfoTeleporterCountdown);

	[NetworkName("m_bCountdownStarted")]
	public bool CountdownStarted;
	[NetworkName("m_bDisabled")]
	public bool Disabled;
	[NetworkName("m_flStartTime")]
	public TimeUnit_t StartTime;
	[NetworkName("m_flTimeRemaining")]
	public TimeUnit_t TimeRemaining;
}
