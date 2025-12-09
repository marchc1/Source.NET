using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<BoneFollower>;
public class BoneFollower : BaseEntity
{
	public static readonly SendTable DT_BoneFollower = new(DT_BaseEntity, [
		SendPropInt(FIELD.OF(nameof(ModelIndex)), 14, 0),
		SendPropInt(FIELD.OF(nameof(SolidIndex)), 6, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("BoneFollower", DT_BoneFollower).WithManualClassID(StaticClassIndices.CBoneFollower);

	public new int ModelIndex;
	public int SolidIndex;
}
