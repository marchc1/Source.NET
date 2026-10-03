using Game.Shared;

using Source;
using Source.Common;

using System.Numerics;

namespace Game.Server;
using FIELD = Source.FIELD<BaseFlex>;

[LinkEntityToClass("funCBaseFlex")]
[NetworkName("CBaseFlex")]
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
	public static readonly new ServerClass ServerClass = new ServerClass(DT_BaseFlex);

	[NetworkName("m_flexWeight")]
	public InlineArray96<float> FlexWeight;
	[NetworkName("m_blinktoggle")]
	public int BlinkToggle;
	[NetworkName("m_viewtarget")]
	public Vector3 ViewTarget;
	[NetworkName("m_vecLean")]
	public Vector3 Lean;
	[NetworkName("m_vecShift")]
	public Vector3 Shift;
}
