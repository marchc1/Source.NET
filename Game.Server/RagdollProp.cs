using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<RagdollProp>;
public class RagdollProp : BaseAnimating
{
	public static readonly SendTable DT_RagdollProp = new(DT_BaseAnimating, [
		SendPropVector(FIELD.OF_ARRAYINDEX(nameof(RagPos), 0), 0, PropFlags.Coord),
		SendPropArray(FIELD.OF_ARRAY(nameof(RagPos))),

		SendPropVector(FIELD.OF_ARRAYINDEX(nameof(RagAngles), 0), 13, PropFlags.RoundDown),
		SendPropArray(FIELD.OF_ARRAY(nameof(RagAngles))),

		SendPropEHandle(FIELD.OF(nameof(HUnragdoll))),
		SendPropFloat(FIELD.OF(nameof(BlendWeight)), 8, PropFlags.RoundDown),
		SendPropInt(FIELD.OF(nameof(OverlaySequence)), 11, 0),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("RagdollProp", DT_RagdollProp).WithManualClassID(StaticClassIndices.CRagdollProp);

	public InlineArray32<Vector3> RagPos;
	public InlineArray32<Vector3> RagAngles;
	public readonly EHANDLE HUnragdoll = new();
	public float BlendWeight;
	public int OverlaySequence;
}
