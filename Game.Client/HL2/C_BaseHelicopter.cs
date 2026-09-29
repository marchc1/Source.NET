using Source.Common;
using Source;

using Game.Shared;

namespace Game.Client;

using FIELD = FIELD<C_BaseHelicopter>;

[NetworkName("CBaseHelicopter")]
public class C_BaseHelicopter : C_AI_TrackPather
{
	public static readonly RecvTable DT_BaseHelicopter = new(DT_AI_BaseNPC, [
		RecvPropFloat(FIELD.OF(nameof(StartupTime)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_BaseHelicopter);

	[NetworkName("m_flStartupTime")]
	public TimeUnit_t StartupTime;
}
