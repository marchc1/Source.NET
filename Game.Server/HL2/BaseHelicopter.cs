using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;

using FIELD = FIELD<BaseHelicopter>;

[NetworkName("CBaseHelicopter")]
public partial class BaseHelicopter : AI_TrackPather
{
	public static readonly SendTable DT_BaseHelicopter = new(DT_AI_BaseNPC, [
		SendPropFloat(NetworkVarFields.StartupTime, 0, PropFlags.NoScale)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_BaseHelicopter);

	[NetworkName("m_flStartupTime")]
	[NetworkVar] public partial TimeUnit_t StartupTime { get; set; }
}
