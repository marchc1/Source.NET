using Game.Shared;

using Source;
using Source.Common;

namespace Game.Client.HL2;
using FIELD = Source.FIELD<C_InfoTeleporterCountdown>;

public partial class C_InfoTeleporterCountdown : C_BaseEntity
{
	public static readonly RecvTable DT_InfoTeleporterCountdown = new(DT_BaseEntity, [
		RecvPropBool(FIELD.OF(nameof(CountdownStarted))),
		RecvPropBool(FIELD.OF(nameof(Disabled))),
		RecvPropFloat(FIELD.OF(nameof(StartTime))),
		RecvPropFloat(FIELD.OF(nameof(TimeRemaining)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("InfoTeleporterCountdown", DT_InfoTeleporterCountdown).WithManualClassID(StaticClassIndices.CInfoTeleporterCountdown);

	public bool CountdownStarted;
	public bool Disabled;
	public TimeUnit_t StartTime;
	public TimeUnit_t TimeRemaining;
}
