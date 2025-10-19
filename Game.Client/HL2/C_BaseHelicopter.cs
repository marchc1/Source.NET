using Source.Common;
using Source;

using Game.Shared;

namespace Game.Client;

using FIELD = FIELD<C_BaseHelicopter>;

public class C_BaseHelicopter : C_AI_TrackPather
{
	public static readonly RecvTable DT_BaseHelicopter = new(DT_AI_BaseNPC, [
		RecvPropFloat(FIELD.OF(nameof(StartupTime)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("BaseHelicopter", DT_BaseHelicopter).WithManualClassID(StaticClassIndices.CBaseHelicopter);

	public TimeUnit_t StartupTime;
}
