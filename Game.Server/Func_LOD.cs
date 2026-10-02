using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;

using FIELD = FIELD<Func_LOD>;

[LinkEntityToClass("func_lod")]
[NetworkName("CFunc_LOD")]
public class Func_LOD : BaseEntity
{
	public static readonly SendTable DT_Func_LOD = new(DT_BaseEntity, [
		SendPropFloat(FIELD.OF(nameof(DisappearMinDist)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(DisappearMaxDist)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_Func_LOD);

	[NetworkName("m_fDisappearMinDist")]
	public float DisappearMinDist;
	[NetworkName("m_fDisappearMaxDist")]
	public float DisappearMaxDist;
}
