using Game.Shared;
using Source.Common;
using Source;
using System.Numerics;

namespace Game.Server;

using FIELD = FIELD<RagdollPropAttached>;

[LinkEntityToClass("prop_ragdoll_attached")]
[NetworkName("CRagdollPropAttached")]
public class RagdollPropAttached : RagdollProp
{
	[NetworkName("m_boneIndexAttached")]
	public int BoneIndexAttached;
	[NetworkName("m_ragdollAttachedObjectIndex")]
	public int RagdollAttachedObjectIndex;
	[NetworkName("m_attachmentPointBoneSpace")]
	public Vector3 AttachmentPointBoneSpace;
	[NetworkName("m_attachmentPointRagdollSpace")]
	public Vector3 AttachmentPointRagdollSpace;

	public static readonly SendTable DT_Ragdoll_Attached = new(DT_Ragdoll, [
		SendPropInt(FIELD.OF(nameof(BoneIndexAttached)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(RagdollAttachedObjectIndex)), 6, PropFlags.Unsigned),
		SendPropVector(FIELD.OF(nameof(AttachmentPointBoneSpace)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(AttachmentPointRagdollSpace)), 0, PropFlags.Coord),
	]);
	public static new readonly ServerClass ServerClass = new ServerClass(DT_Ragdoll_Attached);
}
