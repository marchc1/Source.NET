using Game.Shared;

using Source;
using Source.Common;

namespace Game.Server.HL2;
using FIELD = Source.FIELD<InfoTeleporterCountdown>;
public partial class InfoTeleporterCountdown : BaseEntity
{
	public static readonly SendTable DT_InfoTeleporterCountdown = new(DT_BaseEntity, [
		SendPropBool(FIELD.OF(nameof(CountdownStarted))),
		SendPropBool(FIELD.OF(nameof(Disabled))),
		SendPropFloat(FIELD.OF(nameof(StartTime)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(TimeRemaining)), 0, PropFlags.NoScale)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("InfoTeleporterCountdown", DT_InfoTeleporterCountdown).WithManualClassID(StaticClassIndices.CInfoTeleporterCountdown);

	public bool CountdownStarted;
	public bool Disabled;
	public TimeUnit_t StartTime;
	public TimeUnit_t TimeRemaining;
}
