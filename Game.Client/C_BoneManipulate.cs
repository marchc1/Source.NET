using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_BoneManipulate>;
public class C_BoneManipulate : C_BaseEntity
{
	public static readonly RecvTable DT_BoneManipulate = new(DT_BaseEntity, [
		RecvPropVector(FIELD.OF(nameof(BonePos))),
		RecvPropVector(FIELD.OF(nameof(BoneAng))),
		RecvPropVector(FIELD.OF(nameof(BoneScale))),
		RecvPropInt(FIELD.OF(nameof(BoneJiggle))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("BoneManipulate", DT_BoneManipulate).WithManualClassID(StaticClassIndices.CBoneManipulate);

	public Vector3 BonePos;
	public Vector3 BoneAng;
	public Vector3 BoneScale;
	public int BoneJiggle;
}
