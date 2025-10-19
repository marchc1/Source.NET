using Source.Common;
using Source;

using Game.Shared;

namespace Game.Client;

using FIELD = FIELD<C_FuncAreaPortalWindow>;

public class C_FuncAreaPortalWindow : C_BaseEntity
{
	public static readonly RecvTable DT_FuncAreaPortalWindow = new(DT_BaseEntity, [
		RecvPropFloat(FIELD.OF(nameof(FadeDist))),
		RecvPropFloat(FIELD.OF(nameof(FadeStartDist))),
		RecvPropFloat(FIELD.OF(nameof(TranslucencyLimit))),
		RecvPropInt(FIELD.OF(nameof(BackgroundModelIndex)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("FuncAreaPortalWindow", DT_FuncAreaPortalWindow).WithManualClassID(StaticClassIndices.CFuncAreaPortalWindow);

	public float FadeDist;
	public float FadeStartDist;
	public float TranslucencyLimit;
	public int BackgroundModelIndex;
}

