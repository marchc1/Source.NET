using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;

using FIELD = FIELD<FuncAreaPortalWindow>;

public class FuncAreaPortalWindow : BaseEntity
{
	public static readonly SendTable DT_FuncAreaPortalWindow = new(DT_BaseEntity, [
		SendPropFloat(FIELD.OF(nameof(FadeDist)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(FadeStartDist)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(TranslucencyLimit)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(BackgroundModelIndex)), Constants.SP_MODEL_INDEX_BITS, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("FuncAreaPortalWindow", DT_FuncAreaPortalWindow).WithManualClassID(StaticClassIndices.CFuncAreaPortalWindow);

	public float FadeDist;
	public float FadeStartDist;
	public float TranslucencyLimit;
	public int BackgroundModelIndex;
}
