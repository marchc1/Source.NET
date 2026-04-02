using Game.Shared;

using Source.Common;

namespace Game.Server;
using FIELD = Source.FIELD<BaseAnimatingOverlay>;

public class BaseAnimatingOverlay : BaseAnimating
{
	public const int MAX_OVERLAYS = 15;

	public static readonly SendTable DT_OverlayVars = new([
		SendPropList(FIELD.OF(nameof(AnimOverlay)), MAX_OVERLAYS, SendPropDataTable(null, AnimationLayerRef.DT_AnimationLayer))
	]); public static readonly ServerClass SC_OverlayVars = new ServerClass("OverlayVars", DT_OverlayVars);

	public static readonly SendTable DT_BaseAnimatingOverlay = new(DT_BaseAnimating, [
		SendPropDataTable("overlay_vars", DT_OverlayVars)
	]); public static readonly new ServerClass ServerClass = new ServerClass("BaseAnimatingOverlay", DT_BaseAnimatingOverlay).WithManualClassID(StaticClassIndices.CBaseAnimatingOverlay);

	readonly List<AnimationLayerRef> AnimOverlay = [];
}
