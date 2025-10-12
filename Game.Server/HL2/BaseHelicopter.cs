using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;

using FIELD = FIELD<BaseHelicopter>;

public class BaseHelicopter : AI_TrackPather
{
	public static readonly SendTable DT_BaseHelicopter = new(DT_AI_BaseNPC, [
		SendPropFloat(FIELD.OF(nameof(StartupTime)), 0, PropFlags.NoScale)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("BaseHelicopter", DT_BaseHelicopter).WithManualClassID(StaticClassIndices.CBaseHelicopter);

	public TimeUnit_t StartupTime;
}
