using Source.Common;
using Source;
using System.Numerics;

namespace Game.Server;

using FIELD = FIELD<RagdollPropAttached>;

public class RagdollPropAttached : RagdollProp
{
	public int BoneIndexAttached;
	public int RagdollAttachedObjectIndex;
	public Vector3 AttachmentPointBoneSpace;
	public Vector3 AttachmentPointRagdollSpace;

	public static readonly SendTable DT_Ragdoll_Attached = new(DT_RagdollProp, [
		SendPropInt(FIELD.OF(nameof(BoneIndexAttached)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(RagdollAttachedObjectIndex)), 6, PropFlags.Unsigned),
		SendPropVector(FIELD.OF(nameof(AttachmentPointBoneSpace)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(AttachmentPointRagdollSpace)), 0, PropFlags.Coord),
	]);
	public static new readonly ServerClass ServerClass = new ServerClass("RagdollPropAttached", DT_Ragdoll_Attached).WithManualClassID(Shared.StaticClassIndices.CRagdollPropAttached);
}
