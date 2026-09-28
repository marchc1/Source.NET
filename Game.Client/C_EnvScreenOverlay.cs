using Source.Common;
using Source;

namespace Game.Client;

using FIELD = FIELD<C_EnvScreenOverlay>;

public class C_EnvScreenOverlay : C_BaseEntity
{
	public const int MAX_SCREEN_OVERLAYS = 10;

	public InlineArray10<InlineArray512<char>> OverlayNames;
	public InlineArray10<float> OverlayTimes;
	public float StartTime;
	public int DesiredOverlay;
	public bool IsActive;

	public static readonly RecvTable DT_EnvScreenOverlay = new(DT_BaseEntity, [
		RecvPropString(FIELD.OF_ARRAYINDEX(nameof(OverlayNames), 0)),
		RecvPropArray(FIELD.OF_ARRAY(nameof(OverlayNames))),
		RecvPropFloat(FIELD.OF_ARRAYINDEX(nameof(OverlayTimes), 0)),
		RecvPropArray(FIELD.OF_ARRAY(nameof(OverlayTimes))),
		RecvPropFloat(FIELD.OF(nameof(StartTime))),
		RecvPropInt(FIELD.OF(nameof(DesiredOverlay))),
		RecvPropBool(FIELD.OF(nameof(IsActive))),
	]);
	public static new readonly ClientClass ClientClass = new ClientClass("EnvScreenOverlay", DT_EnvScreenOverlay).WithManualClassID(Shared.StaticClassIndices.CEnvScreenOverlay);
}
