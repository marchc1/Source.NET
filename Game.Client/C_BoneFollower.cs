using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_BoneFollower>;
public class C_BoneFollower : C_BaseEntity
{
	public static readonly RecvTable DT_BoneFollower = new(DT_BaseEntity, [
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropInt(FIELD.OF(nameof(SolidIndex))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("BoneFollower", DT_BoneFollower).WithManualClassID(StaticClassIndices.CBoneFollower);

	public new int ModelIndex;
	public int SolidIndex;
}
