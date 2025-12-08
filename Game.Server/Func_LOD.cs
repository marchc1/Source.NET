using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;

using FIELD = FIELD<Func_LOD>;

public class Func_LOD : BaseEntity
{
	public static readonly SendTable DT_Func_LOD = new(DT_BaseEntity, [
		SendPropFloat(FIELD.OF(nameof(DisappearMinDist)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(DisappearMaxDist)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("Func_LOD", DT_Func_LOD).WithManualClassID(StaticClassIndices.CFunc_LOD);

	public float DisappearMinDist;
	public float DisappearMaxDist;
}
