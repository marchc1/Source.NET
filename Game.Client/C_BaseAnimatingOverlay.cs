global using AnimationLayer = Game.Client.C_AnimationLayer;
using Game.Shared;

using Source.Common;

using FIELD_AL = Source.FIELD<Game.Client.C_AnimationLayer>;
using FIELD_BAO = Source.FIELD<Game.Client.C_BaseAnimatingOverlay>;

namespace Game.Client;

public class C_AnimationLayer
{
	public static readonly RecvTable DT_AnimationLayer = new([
		RecvPropInt(FIELD_AL.OF(nameof(Sequence))),
		RecvPropFloat(FIELD_AL.OF(nameof(Cycle))),
		RecvPropFloat(FIELD_AL.OF(nameof(PrevCycle))),
		RecvPropFloat(FIELD_AL.OF(nameof(Weight))),
		RecvPropInt(FIELD_AL.OF(nameof(Order))),
	]); public static readonly ClientClass ClientClass = new ClientClass("Client", null, null, DT_AnimationLayer).WithManualClassID(StaticClassIndices.CBaseAnimatingOverlay);

	public int Sequence;
	public TimeUnit_t Cycle;
	public float PrevCycle;
	public float Weight;
	public int Order;

	public TimeUnit_t PlaybackRate;
	public TimeUnit_t LayerAnimtime;
	public TimeUnit_t LayerFadeOuttime;
	public double BlendIn;
	public double BlendOut;
	public bool ClientBlend;

	public double GetFadeout(double curTime) {
		double s;

		if (LayerFadeOuttime <= 0.0f) {
			s = 0;
		}
		else {
			// blend in over 0.2 seconds
			s = 1.0 - (curTime - LayerAnimtime) / LayerFadeOuttime;
			if (s > 0 && s <= 1.0f) {
				// do a nice spline curve
				s = 3 * s * s - 2 * s * s * s;
			}
			else if (s > 1.0f) {
				// Shouldn't happen, but maybe curtime is behind animtime?
				s = 1.0;
			}
		}

		return s;
	}
}


public partial class C_BaseAnimatingOverlay : C_BaseAnimating {
	public const int MAX_OVERLAYS = 15;

	public static readonly RecvTable DT_OverlayVars = new([
		RecvPropList<C_AnimationLayer>(FIELD_BAO.OF_LIST(nameof(AnimOverlay), MAX_OVERLAYS), ResizeAnimationLayerCallback, RecvPropDataTable(null, C_AnimationLayer.DT_AnimationLayer))
	]);

	private static void ResizeAnimationLayerCallback(object instance, object list, int length) {
		Warning($"ResizeAnimationLayerCallback not implemented yet\n");
	}

	public static readonly ClientClass CC_OverlayVars = new ClientClass("OverlayVars", null, null, DT_OverlayVars);

	public static readonly RecvTable DT_BaseAnimatingOverlay = new(DT_BaseAnimating, [
		RecvPropDataTable("overlay_vars", DT_OverlayVars)
	]); public static readonly new ClientClass ClientClass = new ClientClass("BaseAnimatingOverlay", null, null, DT_BaseAnimatingOverlay);

	readonly List<C_AnimationLayer> AnimOverlay = [];
}
