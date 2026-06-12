using Game.Shared;

using Source;
using Source.Common;

using System.Numerics;

namespace Game.Server;
using FIELD = Source.FIELD<BaseFlex>;

public class BaseFlex : BaseAnimatingOverlay {
	public static readonly SendTable DT_BaseFlex = new(DT_BaseAnimatingOverlay, [
		SendPropArray3  (FIELD.OF_ARRAY(nameof(FlexWeight)), SendPropFloat(FIELD.OF_ARRAY(nameof(FlexWeight)), 12, PropFlags.RoundDown, 0.0f, 1.0f )),
		SendPropInt     (FIELD.OF(nameof(BlinkToggle)), 1, PropFlags.Unsigned ),
		SendPropVector  (FIELD.OF(nameof(ViewTarget)), -1, PropFlags.Coord),

		SendPropFloat   ( FIELD.OF_VECTORELEM(nameof(ViewOffset), 0), 0, PropFlags.NoScale ),
		SendPropFloat   ( FIELD.OF_VECTORELEM(nameof(ViewOffset), 1), 0, PropFlags.NoScale ),
		SendPropFloat   ( FIELD.OF_VECTORELEM(nameof(ViewOffset), 2), 0, PropFlags.NoScale ),

		SendPropVector  ( FIELD.OF(nameof(Lean)), -1, PropFlags.Coord ),
		SendPropVector  ( FIELD.OF(nameof(Shift)), -1, PropFlags.Coord ),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("BaseFlex", DT_BaseFlex).WithManualClassID(StaticClassIndices.CBaseFlex);

	public InlineArray96<float> FlexWeight;
	public int BlinkToggle;
	public Vector3 ViewTarget;
	public Vector3 Lean;
	public Vector3 Shift;
}
