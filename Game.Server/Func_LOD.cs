using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;

using FIELD = FIELD<Func_LOD>;

[NetworkName("CFunc_LOD")]
public partial class Func_LOD : BaseEntity
{
	public static readonly SendTable DT_Func_LOD = new(DT_BaseEntity, [
		SendPropFloat(NetworkVarFields.DisappearMinDist, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.DisappearMaxDist, 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_Func_LOD);

	[NetworkName("m_fDisappearMinDist")]
	[NetworkVar] public partial float DisappearMinDist { get; set; }
	[NetworkName("m_fDisappearMaxDist")]
	[NetworkVar] public partial float DisappearMaxDist { get; set; }
}
