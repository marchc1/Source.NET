using Source.Common;
using Source;
using System.Numerics;

namespace Game.Client;

using FIELD = FIELD<C_RagdollPropAttached>;

[NetworkName("CRagdollPropAttached")]
public class C_RagdollPropAttached : C_RagdollProp
{
	[NetworkName("m_boneIndexAttached")]
	public int BoneIndexAttached;
	[NetworkName("m_ragdollAttachedObjectIndex")]
	public int RagdollAttachedObjectIndex;
	[NetworkName("m_attachmentPointBoneSpace")]
	public Vector3 AttachmentPointBoneSpace;
	[NetworkName("m_attachmentPointRagdollSpace")]
	public Vector3 AttachmentPointRagdollSpace;

	public static readonly RecvTable DT_Ragdoll_Attached = new(DT_Ragdoll, [
		RecvPropInt(FIELD.OF(nameof(BoneIndexAttached))),
		RecvPropInt(FIELD.OF(nameof(RagdollAttachedObjectIndex))),
		RecvPropVector(FIELD.OF(nameof(AttachmentPointBoneSpace))),
		RecvPropVector(FIELD.OF(nameof(AttachmentPointRagdollSpace))),
	]);
	public static new readonly ClientClass ClientClass = new ClientClass(DT_Ragdoll_Attached);
}
