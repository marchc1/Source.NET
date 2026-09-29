using Game.Shared;

using Source.Common;

namespace Game.Client.HL2;
using FIELD = Source.FIELD<C_AlyxEmpEffect>;

[NetworkName("CAlyxEmpEffect")]
public partial class C_AlyxEmpEffect : C_BaseEntity
{
	public static readonly RecvTable DT_AlyxEmpEffect = new(DT_BaseEntity, [
		RecvPropInt(FIELD.OF(nameof(State))),
		RecvPropFloat(FIELD.OF(nameof(Duration))),
		RecvPropFloat(FIELD.OF(nameof(StartTime))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_AlyxEmpEffect);

	[NetworkName("m_nState")]
	public int State;
	[NetworkName("m_flDuration")]
	public TimeUnit_t Duration;
	[NetworkName("m_flStartTime")]
	public TimeUnit_t StartTime;
}
