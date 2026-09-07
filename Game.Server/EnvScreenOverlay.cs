using Source.Common;
using Source;

namespace Game.Server;

using FIELD = FIELD<EnvScreenOverlay>;

public class EnvScreenOverlay : BaseEntity
{
	public const int MAX_SCREEN_OVERLAYS = 10;

	public InlineArray10<InlineArray512<char>> OverlayNames;
	public InlineArray10<float> OverlayTimes;
	public float StartTime;
	public int DesiredOverlay;
	public bool IsActive;

	public static readonly SendTable DT_EnvScreenOverlay = new(DT_BaseEntity, [
		SendPropString(FIELD.OF_ARRAYINDEX(nameof(OverlayNames), 0)),
		SendPropArray(FIELD.OF_ARRAY(nameof(OverlayNames))),
		SendPropFloat(FIELD.OF_ARRAYINDEX(nameof(OverlayTimes), 0), 11, PropFlags.RoundDown, -1.0f, 63.0f),
		SendPropArray(FIELD.OF_ARRAY(nameof(OverlayTimes))),
		SendPropFloat(FIELD.OF(nameof(StartTime)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(DesiredOverlay)), 5),
		SendPropBool(FIELD.OF(nameof(IsActive))),
	]);
	public static new readonly ServerClass ServerClass = new ServerClass("EnvScreenOverlay", DT_EnvScreenOverlay).WithManualClassID(Shared.StaticClassIndices.CEnvScreenOverlay);
}
