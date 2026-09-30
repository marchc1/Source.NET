using Source.Common;
using Source;
using System.Numerics;

namespace Game.Server;

using FIELD = FIELD<RagdollPropAttached>;

[NetworkName("CRagdollPropAttached")]
public partial class RagdollPropAttached : RagdollProp
{
	[NetworkName("m_boneIndexAttached")]
	[NetworkVar] public partial int BoneIndexAttached { get; set; }
	[NetworkName("m_ragdollAttachedObjectIndex")]
	[NetworkVar] public partial int RagdollAttachedObjectIndex { get; set; }
	[NetworkName("m_attachmentPointBoneSpace")]
	[NetworkVar] public partial Vector3 AttachmentPointBoneSpace { get; set; }
	[NetworkName("m_attachmentPointRagdollSpace")]
	[NetworkVar] public partial Vector3 AttachmentPointRagdollSpace { get; set; }

	public static readonly SendTable DT_Ragdoll_Attached = new(DT_Ragdoll, [
		SendPropInt(NetworkVarFields.BoneIndexAttached, 8, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.RagdollAttachedObjectIndex, 6, PropFlags.Unsigned),
		SendPropVector(NetworkVarFields.AttachmentPointBoneSpace, 0, PropFlags.Coord),
		SendPropVector(NetworkVarFields.AttachmentPointRagdollSpace, 0, PropFlags.Coord),
	]);
	public static new readonly ServerClass ServerClass = new ServerClass(DT_Ragdoll_Attached);
}
