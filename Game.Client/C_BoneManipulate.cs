using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;

using FIELD = FIELD<C_BoneManipulate>;
public class C_BoneManipulate : C_BaseEntity
{
	public static readonly RecvTable DT_BoneManipulate = new(DT_BaseEntity, [
		RecvPropArray3(FIELD.OF_ARRAY(nameof(BonePos)), RecvPropVector(null!)),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(BoneAng)), RecvPropVector(null!)),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(BoneScale)), RecvPropVector(null!)),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(BoneJiggle)), RecvPropInt(null!, null!)),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("BoneManipulate", DT_BoneManipulate).WithManualClassID(StaticClassIndices.CBoneManipulate);

	public InlineArrayMaxStudioBones<Vector3> BonePos;
	public InlineArrayMaxStudioBones<Vector3> BoneAng;
	public InlineArrayMaxStudioBones<Vector3> BoneScale;
	public InlineArrayMaxStudioBones<int> BoneJiggle;
}
