using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;

using FIELD = FIELD<FuncAreaPortalWindow>;

[NetworkName("CFuncAreaPortalWindow")]
public partial class FuncAreaPortalWindow : BaseEntity
{
	public static readonly SendTable DT_FuncAreaPortalWindow = new(DT_BaseEntity, [
		SendPropFloat(NetworkVarFields.FadeDist, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.FadeStartDist, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.TranslucencyLimit, 0, PropFlags.NoScale),
		SendPropModelIndex(NetworkVarFields.BackgroundModelIndex),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_FuncAreaPortalWindow);

	[NetworkName("m_flFadeDist")]
	[NetworkVar] public partial float FadeDist { get; set; }
	[NetworkName("m_flFadeStartDist")]
	[NetworkVar] public partial float FadeStartDist { get; set; }
	[NetworkName("m_flTranslucencyLimit")]
	[NetworkVar] public partial float TranslucencyLimit { get; set; }
	[NetworkName("m_iBackgroundModelIndex")]
	[NetworkVar] public partial int BackgroundModelIndex { get; set; }
}
