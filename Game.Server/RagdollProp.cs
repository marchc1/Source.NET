using Source.Common;
using Source;

using Game.Shared;

using System.Numerics;
namespace Game.Server;

using FIELD = FIELD<RagdollProp>;
[NetworkName("CRagdollProp")]
public partial class RagdollProp : BaseAnimating
{
	const int RAGDOLL_MAX_ELEMENTS = 32;
	public static readonly SendTable DT_Ragdoll = new(DT_BaseAnimating, [
		.. Enumerable.Range(0, RAGDOLL_MAX_ELEMENTS).Select(i => SendPropVector(FIELD.OF_ARRAYINDEX(nameof(RagPos), i), -1, PropFlags.Coord)),
		.. Enumerable.Range(0, RAGDOLL_MAX_ELEMENTS).Select(i => SendPropQAngles(FIELD.OF_ARRAYINDEX(nameof(RagAngles), i), 13, PropFlags.RoundDown)),

		SendPropEHandle(RagdollProp.NetworkVarFields.HUnragdoll),
		SendPropFloat(NetworkVarFields.BlendWeight, 8, PropFlags.RoundDown, 0.0f, 1.0f),
		SendPropInt(NetworkVarFields.OverlaySequence, 11, 0),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_Ragdoll);

	[NetworkName("m_ragPos[ {0} ]")]
	public InlineArray32<Vector3> RagPos;
	[NetworkName("m_ragAngles[ {0} ]")]
	public InlineArray32<Vector3> RagAngles;
	[NetworkName("m_hUnragdoll")]
	[NetworkVar] public partial NetworkHandle<BaseEntity> HUnragdoll { get; }
	[NetworkName("m_flBlendWeight")]
	[NetworkVar] public partial float BlendWeight { get; set; }
	[NetworkName("m_nOverlaySequence")]
	[NetworkVar] public partial int OverlaySequence { get; set; }
}
