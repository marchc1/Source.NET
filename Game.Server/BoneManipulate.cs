using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<BoneManipulate>;
public class BoneManipulate : BaseEntity
{
	public static readonly SendTable DT_BoneManipulate = new(DT_BaseEntity, [
		SendPropVector(FIELD.OF(nameof(BonePos)), 0, PropFlags.ProxyAlwaysYes | PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(BoneAng)), 0, PropFlags.ProxyAlwaysYes | PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(BoneScale)), 0, PropFlags.ProxyAlwaysYes | PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(BoneJiggle)), 4, PropFlags.ProxyAlwaysYes | PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("BoneManipulate", DT_BoneManipulate).WithManualClassID(StaticClassIndices.CBoneManipulate);

	public Vector3 BonePos;
	public Vector3 BoneAng;
	public Vector3 BoneScale;
	public int BoneJiggle;
}
