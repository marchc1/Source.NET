using Game.Shared;

using Source;
using Source.Common;

namespace Game.Server.HL2;
using FIELD = Source.FIELD<InfoTeleporterCountdown>;
[NetworkName("CInfoTeleporterCountdown")]
public partial class InfoTeleporterCountdown : BaseEntity
{
	public static readonly SendTable DT_InfoTeleporterCountdown = new(DT_BaseEntity, [
		SendPropBool(NetworkVarFields.CountdownStarted),
		SendPropBool(NetworkVarFields.Disabled),
		SendPropFloat(NetworkVarFields.StartTime, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.TimeRemaining, 0, PropFlags.NoScale)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_InfoTeleporterCountdown);

	[NetworkName("m_bCountdownStarted")]
	[NetworkVar] public partial bool CountdownStarted { get; set; }
	[NetworkName("m_bDisabled")]
	[NetworkVar] public partial bool Disabled { get; set; }
	[NetworkName("m_flStartTime")]
	[NetworkVar] public partial TimeUnit_t StartTime { get; set; }
	[NetworkName("m_flTimeRemaining")]
	[NetworkVar] public partial TimeUnit_t TimeRemaining { get; set; }
}
