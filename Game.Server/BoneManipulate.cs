using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<BoneManipulate>;
public class BoneManipulate : BaseEntity
{
	public static readonly SendTable DT_BoneManipulate = new(DT_BaseEntity, [
		SendPropArray3(FIELD.OF_ARRAY(nameof(BonePos)), SendPropVector(null!, 0, PropFlags.ProxyAlwaysYes | PropFlags.NoScale)),
		SendPropArray3(FIELD.OF_ARRAY(nameof(BoneAng)), SendPropVector(null!, 0, PropFlags.ProxyAlwaysYes | PropFlags.NoScale)),
		SendPropArray3(FIELD.OF_ARRAY(nameof(BoneScale)), SendPropVector(null!, 0, PropFlags.ProxyAlwaysYes | PropFlags.NoScale)),
		SendPropArray3(FIELD.OF_ARRAY(nameof(BoneJiggle)), SendPropInt((IFieldAccessor)null!, 4, PropFlags.ProxyAlwaysYes | PropFlags.Unsigned)),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("BoneManipulate", DT_BoneManipulate).WithManualClassID(StaticClassIndices.CBoneManipulate);

	public InlineArrayMaxStudioBones<Vector3> BonePos;
	public InlineArrayMaxStudioBones<Vector3> BoneAng;
	public InlineArrayMaxStudioBones<Vector3> BoneScale;
	public InlineArrayMaxStudioBones<int> BoneJiggle;
}
