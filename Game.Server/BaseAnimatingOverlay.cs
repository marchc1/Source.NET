using Game.Shared;

using Source.Common;

namespace Game.Server;
using FIELD_AL = Source.FIELD<AnimationLayer>;
using FIELD_BAO = Source.FIELD<BaseAnimatingOverlay>;

public class AnimationLayer
{
	public const int ORDER_BITS = 4;
	public const int WEIGHT_BITS = 8;

	public static readonly SendTable DT_AnimationLayer = new([
		SendPropInt(FIELD_AL.OF(nameof(Sequence)), BaseAnimating.ANIMATION_SEQUENCE_BITS, PropFlags.Unsigned),
		SendPropFloat(FIELD_AL.OF(nameof(Cycle)), ANIMATION_CYCLE_BITS, PropFlags.RoundDown, 0.0f, 1.0f),
		SendPropFloat(FIELD_AL.OF(nameof(PrevCycle)), ANIMATION_CYCLE_BITS, PropFlags.RoundDown, 0.0f, 1.0f),
		SendPropFloat(FIELD_AL.OF(nameof(Weight)), WEIGHT_BITS, 0, 0.0f, 1.0f),
		SendPropInt(FIELD_AL.OF(nameof(Order)), ORDER_BITS, PropFlags.Unsigned),
	]); public static readonly ServerClass ServerClass = new ServerClass("AnimationLayer", DT_AnimationLayer);

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
		throw new NotImplementedException();
	}
}

public class BaseAnimatingOverlay : BaseAnimating
{
	public const int MAX_OVERLAYS = 15;

	public static readonly SendTable DT_OverlayVars = new([
		SendPropList(FIELD_BAO.OF(nameof(AnimOverlay)), MAX_OVERLAYS, SendPropDataTable(null, AnimationLayer.DT_AnimationLayer))
	]); public static readonly ServerClass SC_OverlayVars = new ServerClass("OverlayVars", DT_OverlayVars);

	public static readonly SendTable DT_BaseAnimatingOverlay = new(DT_BaseAnimating, [
		SendPropDataTable("overlay_vars", DT_OverlayVars)
	]); public static readonly new ServerClass ServerClass = new ServerClass("BaseAnimatingOverlay", DT_BaseAnimatingOverlay).WithManualClassID(StaticClassIndices.CBaseAnimatingOverlay);

	readonly List<AnimationLayer> AnimOverlay = [];
}
