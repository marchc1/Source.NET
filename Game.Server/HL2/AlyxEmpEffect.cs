using Game.Shared;

using Source.Common;

namespace Game.Server.HL2;
using FIELD = Source.FIELD<AlyxEmpEffect>;
[NetworkName("CAlyxEmpEffect")]
public partial class AlyxEmpEffect : BaseEntity
{
	public static readonly SendTable DT_AlyxEmpEffect = new(DT_BaseEntity, [
		SendPropInt(NetworkVarFields.State, 8, PropFlags.Unsigned),
		SendPropFloat(NetworkVarFields.Duration, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.StartTime, 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_AlyxEmpEffect);

	[NetworkName("m_nState")]
	[NetworkVar] public partial int State { get; set; }
	[NetworkName("m_flDuration")]
	[NetworkVar] public partial TimeUnit_t Duration { get; set; }
	[NetworkName("m_flStartTime")]
	[NetworkVar] public partial TimeUnit_t StartTime { get; set; }
}
