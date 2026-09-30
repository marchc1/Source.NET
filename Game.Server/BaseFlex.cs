using Game.Shared;

using Source;
using Source.Common;

using System.Numerics;

namespace Game.Server;
using FIELD = Source.FIELD<BaseFlex>;

[NetworkName("CBaseFlex")]
public partial class BaseFlex : BaseAnimatingOverlay {
	public static readonly SendTable DT_BaseFlex = new(DT_BaseAnimatingOverlay, [
		SendPropArray3  (BaseFlex.NetworkVarFields.FlexWeight, SendPropFloat(BaseFlex.NetworkVarFields.FlexWeight, 12, PropFlags.RoundDown, 0.0f, 1.0f )),
		SendPropInt     (NetworkVarFields.BlinkToggle, 1, PropFlags.Unsigned ),
		SendPropVector  (NetworkVarFields.ViewTarget, -1, PropFlags.Coord),

		SendPropFloat   ( FIELD.OF_VECTORELEM(nameof(ViewOffset), 0), 0, PropFlags.NoScale ),
		SendPropFloat   ( FIELD.OF_VECTORELEM(nameof(ViewOffset), 1), 0, PropFlags.NoScale ),
		SendPropFloat   ( FIELD.OF_VECTORELEM(nameof(ViewOffset), 2), 0, PropFlags.NoScale ),

		SendPropVector  ( NetworkVarFields.Lean, -1, PropFlags.Coord ),
		SendPropVector  ( NetworkVarFields.Shift, -1, PropFlags.Coord ),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_BaseFlex);

	[NetworkName("m_flexWeight")]
	[NetworkVar] public partial NetworkArray<InlineArray96<float>, float> FlexWeight { get; }
	[NetworkName("m_blinktoggle")]
	[NetworkVar] public partial int BlinkToggle { get; set; }
	[NetworkName("m_viewtarget")]
	[NetworkVar] public partial Vector3 ViewTarget { get; set; }
	[NetworkName("m_vecLean")]
	[NetworkVar] public partial Vector3 Lean { get; set; }
	[NetworkName("m_vecShift")]
	[NetworkVar] public partial Vector3 Shift { get; set; }
}
