using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEProjectedDecal>;
public class C_TEProjectedDecal : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEProjectedDecal = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropVector(FIELD.OF(nameof(Rotation))),
		RecvPropFloat(FIELD.OF(nameof(LDistance))),
		RecvPropInt(FIELD.OF(nameof(Index))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEProjectedDecal", DT_TEProjectedDecal).WithManualClassID(StaticClassIndices.CTEProjectedDecal);

	public Vector3 Origin;
	public Vector3 Rotation;
	public float LDistance;
	public int Index;
}
