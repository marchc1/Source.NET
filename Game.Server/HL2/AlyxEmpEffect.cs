using Game.Shared;

using Source.Common;

namespace Game.Server.HL2;
using FIELD = Source.FIELD<AlyxEmpEffect>;
[LinkEntityToClass("env_alyxemp")]
[NetworkName("CAlyxEmpEffect")]
public partial class AlyxEmpEffect : BaseEntity
{
	public static readonly SendTable DT_AlyxEmpEffect = new(DT_BaseEntity, [
		SendPropInt(FIELD.OF(nameof(State)), 8, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(Duration)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(StartTime)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_AlyxEmpEffect);

	[NetworkName("m_nState")]
	public int State;
	[NetworkName("m_flDuration")]
	public TimeUnit_t Duration;
	[NetworkName("m_flStartTime")]
	public TimeUnit_t StartTime;
}
