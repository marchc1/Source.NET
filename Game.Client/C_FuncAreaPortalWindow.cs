using Source.Common;
using Source;

using Game.Shared;

namespace Game.Client;

using FIELD = FIELD<C_FuncAreaPortalWindow>;

[NetworkName("CFuncAreaPortalWindow")]
public class C_FuncAreaPortalWindow : C_BaseEntity
{
	public static readonly RecvTable DT_FuncAreaPortalWindow = new(DT_BaseEntity, [
		RecvPropFloat(FIELD.OF(nameof(FadeDist))),
		RecvPropFloat(FIELD.OF(nameof(FadeStartDist))),
		RecvPropFloat(FIELD.OF(nameof(TranslucencyLimit))),
		RecvPropInt(FIELD.OF(nameof(BackgroundModelIndex)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_FuncAreaPortalWindow);

	[NetworkName("m_flFadeDist")]
	public float FadeDist;
	[NetworkName("m_flFadeStartDist")]
	public float FadeStartDist;
	[NetworkName("m_flTranslucencyLimit")]
	public float TranslucencyLimit;
	[NetworkName("m_iBackgroundModelIndex")]
	public int BackgroundModelIndex;
}

