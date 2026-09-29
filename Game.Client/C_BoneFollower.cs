using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_BoneFollower>;
[NetworkName("CBoneFollower")]
public class C_BoneFollower : C_BaseEntity
{
	public static readonly RecvTable DT_BoneFollower = new(DT_BaseEntity, [
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropInt(FIELD.OF(nameof(SolidIndex))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_BoneFollower);

	[NetworkName("m_modelIndex")]
	public new int ModelIndex;
	[NetworkName("m_solidIndex")]
	public int SolidIndex;
}
