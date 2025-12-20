using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_RagdollProp>;
public class C_RagdollProp : C_BaseAnimating
{
	public static readonly RecvTable DT_RagdollProp = new(DT_BaseAnimating, [
		RecvPropVector(FIELD.OF_ARRAYINDEX(nameof(RagPos), 0)),
		RecvPropArray(FIELD.OF_ARRAY(nameof(RagPos))),
		
		RecvPropVector(FIELD.OF_ARRAYINDEX(nameof(RagAngles), 0)),
		RecvPropArray(FIELD.OF_ARRAY(nameof(RagAngles))),

		RecvPropEHandle(FIELD.OF(nameof(HUnragdoll))),
		RecvPropFloat(FIELD.OF(nameof(BlendWeight))),
		RecvPropInt(FIELD.OF(nameof(OverlaySequence))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("RagdollProp", DT_RagdollProp).WithManualClassID(StaticClassIndices.CRagdollProp);

	public InlineArray32<Vector3> RagPos;
	public InlineArray32<Vector3> RagAngles;
	public readonly EHANDLE HUnragdoll = new();
	public float BlendWeight;
	public int OverlaySequence;
}
