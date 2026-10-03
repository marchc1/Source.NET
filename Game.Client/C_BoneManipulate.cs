using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;

using FIELD = FIELD<C_BoneManipulate>;
[LinkEntityToClass("manipulate_bone")]
[NetworkName("CBoneManipulate")]
public class C_BoneManipulate : C_BaseEntity
{
	public static readonly RecvTable DT_BoneManipulate = new(DT_BaseEntity, [
		RecvPropArray3(FIELD.OF_ARRAY(nameof(BonePos)), RecvPropVector(null!)),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(BoneAng)), RecvPropVector(null!)),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(BoneScale)), RecvPropVector(null!)),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(BoneJiggle)), RecvPropInt(null!, null!, sizeOfVar: sizeof(int))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_BoneManipulate);

	[NetworkName("m_BonePos")]
	public InlineArrayMaxStudioBones<Vector3> BonePos;
	[NetworkName("m_BoneAng")]
	public InlineArrayMaxStudioBones<Vector3> BoneAng;
	[NetworkName("m_BoneScale")]
	public InlineArrayMaxStudioBones<Vector3> BoneScale;
	[NetworkName("m_BoneJiggle")]
	public InlineArrayMaxStudioBones<int> BoneJiggle;
}
