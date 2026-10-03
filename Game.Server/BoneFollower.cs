using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<BoneFollower>;
[LinkEntityToClass("phys_bone_follower")]
[NetworkName("CBoneFollower")]
public class BoneFollower : BaseEntity
{
	public static readonly SendTable DT_BoneFollower = new(DT_BaseEntity, [
		SendPropInt(FIELD.OF(nameof(ModelIndex)), 14, 0),
		SendPropInt(FIELD.OF(nameof(SolidIndex)), 6, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_BoneFollower);

	[NetworkName("m_modelIndex")]
	public new int ModelIndex;
	[NetworkName("m_solidIndex")]
	public int SolidIndex;
}
