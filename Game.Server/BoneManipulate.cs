using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<BoneManipulate>;
[LinkEntityToClass("manipulate_bone")]
[NetworkName("CBoneManipulate")]
public class BoneManipulate : BaseEntity
{
	public static readonly SendTable DT_BoneManipulate = new(DT_BaseEntity, [
		SendPropArray3(FIELD.OF_ARRAY(nameof(BonePos)), SendPropVector(null!, 0, PropFlags.NoScale)),
		SendPropArray3(FIELD.OF_ARRAY(nameof(BoneAng)), SendPropVector(null!, 0, PropFlags.NoScale)),
		SendPropArray3(FIELD.OF_ARRAY(nameof(BoneScale)), SendPropVector(null!, 0, PropFlags.NoScale)),
		SendPropArray3(FIELD.OF_ARRAY(nameof(BoneJiggle)), SendPropInt((IFieldAccessor)null!, 4, PropFlags.Unsigned, sizeOfVar: sizeof(int))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_BoneManipulate);

	[NetworkName("m_BonePos")]
	public InlineArrayMaxStudioBones<Vector3> BonePos;
	[NetworkName("m_BoneAng")]
	public InlineArrayMaxStudioBones<Vector3> BoneAng;
	[NetworkName("m_BoneScale")]
	public InlineArrayMaxStudioBones<Vector3> BoneScale;
	[NetworkName("m_BoneJiggle")]
	public InlineArrayMaxStudioBones<int> BoneJiggle;
}
