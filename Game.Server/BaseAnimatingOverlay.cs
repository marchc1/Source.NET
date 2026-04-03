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

	public AnimationLayerRef GetAnimOverlay(int i) => AnimOverlay[i];

	public int GetNumAnimOverlays() => AnimOverlay.Count;
	public void SetNumAnimOverlays(int num) {
		if (AnimOverlay.Count < num)
			for (int i = 0, diff = num - AnimOverlay.Count; i < diff; i++)
				AnimOverlay.Add(new());
		else if (AnimOverlay.Count > num)
			for (int i = 0, diff = AnimOverlay.Count - num; i < diff; i++)
				AnimOverlay.RemoveAt(AnimOverlay.Count - 1);
	}
}
