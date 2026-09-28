using Game.Shared;

using Source;
using Source.Common;

using System.Numerics;

using FIELD = Source.FIELD<Game.Client.C_BaseFlex>;

namespace Game.Client;

public interface IHasLocalToGlobalFlexSettings;
public partial class C_BaseFlex : C_BaseAnimatingOverlay, IHasLocalToGlobalFlexSettings
{
	readonly Source.Common.Audio.MouthInfo mouth = new();
	public override Source.Common.Audio.MouthInfo? GetMouth() => mouth;

	public static readonly RecvTable DT_BaseFlex = new(DT_BaseAnimatingOverlay, [
		RecvPropArray3  (FIELD.OF_ARRAY(nameof(FlexWeight)), RecvPropFloat(FIELD.OF_ARRAY(nameof(FlexWeight)))),
		RecvPropInt     (FIELD.OF(nameof(BlinkToggle))),
		RecvPropVector  (FIELD.OF(nameof(ViewTarget))),

		RecvPropFloat   ( FIELD.OF_VECTORELEM(nameof(ViewOffset), 0)),
		RecvPropFloat   ( FIELD.OF_VECTORELEM(nameof(ViewOffset), 1)),
		RecvPropFloat   ( FIELD.OF_VECTORELEM(nameof(ViewOffset), 2)),

		RecvPropVector  ( FIELD.OF(nameof(Lean))),
		RecvPropVector  ( FIELD.OF(nameof(Shift))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("BaseFlex", null, null, DT_BaseFlex).WithManualClassID(StaticClassIndices.CBaseFlex);
	public InlineArray96<float> FlexWeight;
	public int BlinkToggle;
	public Vector3 ViewTarget;
	public Vector3 Lean;
	public Vector3 Shift;
}