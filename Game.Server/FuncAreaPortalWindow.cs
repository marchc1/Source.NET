using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;

using FIELD = FIELD<FuncAreaPortalWindow>;

[LinkEntityToClass("func_areaportalwindow")]
[NetworkName("CFuncAreaPortalWindow")]
public class FuncAreaPortalWindow : BaseEntity
{
	public static readonly SendTable DT_FuncAreaPortalWindow = new(DT_BaseEntity, [
		SendPropFloat(FIELD.OF(nameof(FadeDist)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(FadeStartDist)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(TranslucencyLimit)), 0, PropFlags.NoScale),
		SendPropModelIndex(FIELD.OF(nameof(BackgroundModelIndex))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_FuncAreaPortalWindow);

	[NetworkName("m_flFadeDist")]
	public float FadeDist;
	[NetworkName("m_flFadeStartDist")]
	public float FadeStartDist;
	[NetworkName("m_flTranslucencyLimit")]
	public float TranslucencyLimit;
	[NetworkName("m_iBackgroundModelIndex")]
	public int BackgroundModelIndex;
}
