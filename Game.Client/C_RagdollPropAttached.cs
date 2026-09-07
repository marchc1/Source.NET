using Source.Common;
using Source;
using System.Numerics;

namespace Game.Client;

using FIELD = FIELD<C_RagdollPropAttached>;

public class C_RagdollPropAttached : C_RagdollProp
{
	public int BoneIndexAttached;
	public int RagdollAttachedObjectIndex;
	public Vector3 AttachmentPointBoneSpace;
	public Vector3 AttachmentPointRagdollSpace;

	public static readonly RecvTable DT_Ragdoll_Attached = new(DT_RagdollProp, [
		RecvPropInt(FIELD.OF(nameof(BoneIndexAttached))),
		RecvPropInt(FIELD.OF(nameof(RagdollAttachedObjectIndex))),
		RecvPropVector(FIELD.OF(nameof(AttachmentPointBoneSpace))),
		RecvPropVector(FIELD.OF(nameof(AttachmentPointRagdollSpace))),
	]);
	public static new readonly ClientClass ClientClass = new ClientClass("RagdollPropAttached", DT_Ragdoll_Attached).WithManualClassID(Shared.StaticClassIndices.CRagdollPropAttached);
}
