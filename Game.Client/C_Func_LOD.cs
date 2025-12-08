using Source.Common;
using Source;

using Game.Shared;

namespace Game.Client;

using FIELD = FIELD<C_Func_LOD>;

public class C_Func_LOD : C_BaseEntity
{
	public static readonly RecvTable DT_Func_LOD = new(DT_BaseEntity, [
		RecvPropFloat(FIELD.OF(nameof(DisappearMinDist))),
		RecvPropFloat(FIELD.OF(nameof(DisappearMaxDist))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("Func_LOD", DT_Func_LOD).WithManualClassID(StaticClassIndices.CFunc_LOD);

	public float DisappearMinDist;
	public float DisappearMaxDist;
}
